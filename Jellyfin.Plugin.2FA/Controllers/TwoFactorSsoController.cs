using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Plugin.TwoFA.Classes;
using Jellyfin.Plugin.TwoFA.Configuration;
using Jellyfin.Plugin.TwoFA.Models;
using Jellyfin.Plugin.TwoFA.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TwoFA.Controllers;

/// <summary>
/// Provides a standalone 2FA login page and authentication endpoint.
/// </summary>
[ApiController]
[Route("sso/2fa")]
public sealed class TwoFactorSsoController : ControllerBase
{
    private static readonly string[] EntryPoints = ["index.html", "login", "login.html"];

    private readonly BrandingOptions _brandingOptions;
    private readonly PluginConfiguration _config;
    private readonly ISessionManager _sessionManager;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorUserStore _userStore;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorSsoController"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager for direct auth.</param>
    /// <param name="userManager">User manager for password auth.</param>
    /// <param name="userStore">User 2FA settings store.</param>
    /// <param name="totpService">TOTP service.</param>
    /// <param name="configurationManager">Configuration manager for branding.</param>
    public TwoFactorSsoController(
        ISessionManager sessionManager,
        IUserManager userManager,
        ITwoFactorUserStore userStore,
        ITotpService totpService,
        IConfigurationManager configurationManager)
    {
        _sessionManager = sessionManager;
        _userManager = userManager;
        _userStore = userStore;
        _totpService = totpService;
        _config = Plugin.Instance?.Configuration ?? throw new ArgumentException("Plugin instance not initialized.");
        _brandingOptions = configurationManager.GetConfiguration<BrandingOptions>("branding");
    }

    /// <summary>
    /// Serves the 2FA login page assets.
    /// </summary>
    /// <param name="fileName">Requested file name.</param>
    /// <returns>Embedded login page resource.</returns>
    [AllowAnonymous]
    [HttpGet("{fileName=login}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Files([FromRoute] string fileName)
    {
        var lowerFilename = fileName.ToLowerInvariant();
        if (EntryPoints.Contains(lowerFilename))
        {
            lowerFilename = "index";
        }

        ExtraPageInfo? view = Constants.LoginFiles.FirstOrDefault(extra => extra.Name == lowerFilename);
        if (view == null)
        {
            return NotFound($"Resource not found: '{lowerFilename}'");
        }

        string mimeType = MimeTypes.GetMimeType(view.EmbeddedResourcePath);
        Stream? rawStream = GetType().Assembly.GetManifestResourceStream(view.EmbeddedResourcePath);
        if (rawStream == null)
        {
            return StatusCode(500, $"Resource failed to load: {view.EmbeddedResourcePath}");
        }

        if (!view.NeedsReplacement)
        {
            return File(rawStream, mimeType);
        }

        using var reader = new StreamReader(rawStream);
        string html = await reader.ReadToEndAsync().ConfigureAwait(false);
        string replaced = html
            .Replace("{{SERVER_URL}}", GetServerBaseUrl(), StringComparison.Ordinal)
            .Replace("/*{{CUSTOM_CSS}}*/", _brandingOptions.CustomCss ?? string.Empty, StringComparison.Ordinal);

        return Content(replaced, mimeType);
    }

    /// <summary>
    /// Authenticates a user using password and optional TOTP.
    /// </summary>
    /// <param name="request">Login request.</param>
    /// <returns>Authentication result.</returns>
    [AllowAnonymous]
    [HttpPost("authenticate")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SsoAuthenticationResult>> Authenticate([FromBody] TwoFactorLoginRequest request)
    {
        if (request == null)
        {
            return BadRequest("Missing request body.");
        }

        string serverUrl = GetServerBaseUrl();
        string remoteEndPoint = Request.HttpContext.GetNormalizedRemoteIP()?.ToString() ?? string.Empty;

        try
        {
            var user = await _userManager.AuthenticateUser(
                request.Username,
                request.Password,
                passwordSha1: string.Empty,
                remoteEndPoint: remoteEndPoint,
                isUserSession: true).ConfigureAwait(false);

            if (user == null)
            {
                return Unauthorized(new SsoAuthenticationResult
                {
                    ServerAddress = serverUrl,
                    ErrorMessage = "Invalid username or password."
                });
            }

            var settings = await _userStore.GetAsync(user.Id, HttpContext.RequestAborted).ConfigureAwait(false);
            bool requiresTotp = _config.EnableTotp && settings.IsEnabled && settings.IsTotpConfirmed;

            if (requiresTotp)
            {
                if (string.IsNullOrWhiteSpace(request.Otp) || settings.TotpSecret is null)
                {
                    return Unauthorized(new SsoAuthenticationResult
                    {
                        ServerAddress = serverUrl,
                        ErrorMessage = "One-time code required."
                    });
                }

                if (!_totpService.ValidateCode(settings.TotpSecret, request.Otp))
                {
                    return Unauthorized(new SsoAuthenticationResult
                    {
                        ServerAddress = serverUrl,
                        ErrorMessage = "Invalid one-time code."
                    });
                }
            }

            var authRequest = new AuthenticationRequest
            {
                App = Constants.PluginName,
                AppVersion = GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0.1",
                DeviceName = GetDeviceName(Request),
                DeviceId = GetDeviceId(Request),
                RemoteEndPoint = remoteEndPoint,
                UserId = user.Id,
                Username = user.Username
            };

            var authResult = await _sessionManager.AuthenticateDirect(authRequest).ConfigureAwait(false);
            if (authResult == null)
            {
                return Unauthorized(new SsoAuthenticationResult
                {
                    ServerAddress = serverUrl,
                    ErrorMessage = "Unable to create session."
                });
            }

            return Ok(new SsoAuthenticationResult
            {
                Ok = true,
                ServerAddress = serverUrl,
                AuthenticatedUser = authResult
            });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new SsoAuthenticationResult
            {
                ServerAddress = serverUrl,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SsoAuthenticationResult
            {
                ServerAddress = serverUrl,
                ErrorMessage = ex.Message
            });
        }
    }

    private static string GetDeviceName(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-DeviceName", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return "Web Browser - 2FA";
    }

    private static string GetDeviceId(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-DeviceId", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }

    private string GetServerBaseUrl()
    {
        string? forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        string? forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();

        string scheme = !string.IsNullOrWhiteSpace(forwardedProto) ? forwardedProto : Request.Scheme;
        string host = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost! : Request.Host.Value ?? string.Empty;
        string pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;

        return $"{scheme}://{host}{pathBase}";
    }
}
