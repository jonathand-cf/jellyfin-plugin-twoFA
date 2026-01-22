using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TwoFA.Models;
using Jellyfin.Plugin.TwoFA.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TwoFA.Controllers;

/// <summary>
/// Backend-only 2FA endpoints for enrollment and verification.
/// </summary>
[ApiController]
[Route("Plugins/TwoFA")]
public sealed class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorUserStore _store;
    private readonly ITotpService _totp;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorController"/> class.
    /// </summary>
    /// <param name="store">The per-user 2FA store.</param>
    /// <param name="totp">The TOTP service.</param>
    /// <param name="userManager">The user manager.</param>
    public TwoFactorController(ITwoFactorUserStore store, ITotpService totp, IUserManager userManager)
    {
        _store = store;
        _totp = totp;
        _userManager = userManager;
    }

    /// <summary>
    /// Generates a TOTP secret for the user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The secret and otpauth URI.</returns>
    [HttpPost("users/{userId:guid}/totp/enroll")]
    public async Task<ActionResult<EnrollResponse>> Enroll(Guid userId, CancellationToken ct)
    {
        var secret = _totp.GenerateSecret();
        var settings = await _store.GetAsync(userId, ct).ConfigureAwait(false);
        settings.TotpSecret = secret;
        settings.IsTotpConfirmed = false;
        settings.IsEnabled = false;
        await _store.SetAsync(userId, settings, ct).ConfigureAwait(false);

        var user = _userManager.GetUserById(userId);
        var uri = _totp.BuildOtpAuthUri("Jellyfin", user?.Username ?? "Jellyfin", secret);

        return new EnrollResponse
        {
            Secret = secret,
            OtpAuthUri = uri
        };
    }

    /// <summary>
    /// Confirms a TOTP code for the user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The confirmation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An action result.</returns>
    [HttpPost("users/{userId:guid}/totp/confirm")]
    public async Task<ActionResult> Confirm(Guid userId, [FromBody] ConfirmRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequest("Missing request body.");
        }

        var settings = await _store.GetAsync(userId, ct).ConfigureAwait(false);
        if (settings.TotpSecret is null || !_totp.ValidateCode(settings.TotpSecret, request.Code))
        {
            return BadRequest("Invalid code.");
        }

        settings.IsEnabled = true;
        settings.IsTotpConfirmed = true;
        await _store.SetAsync(userId, settings, ct).ConfigureAwait(false);
        return Ok();
    }

    /// <summary>
    /// Verifies a TOTP code for the user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The verification request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("users/{userId:guid}/totp/verify")]
    public async Task<ActionResult<VerifyResponse>> Verify(Guid userId, [FromBody] VerifyRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequest("Missing request body.");
        }

        var settings = await _store.GetAsync(userId, ct).ConfigureAwait(false);
        if (!settings.IsEnabled || !settings.IsTotpConfirmed || settings.TotpSecret is null)
        {
            return BadRequest("2FA is not enabled for this user.");
        }

        if (!_totp.ValidateCode(settings.TotpSecret, request.Code))
        {
            return BadRequest("Invalid code.");
        }

        settings.LastVerifiedUtc = DateTimeOffset.UtcNow;
        await _store.SetAsync(userId, settings, ct).ConfigureAwait(false);
        return new VerifyResponse { Success = true };
    }

    /// <summary>
    /// Disables TOTP for the user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An action result.</returns>
    [HttpPost("users/{userId:guid}/totp/disable")]
    public async Task<ActionResult> Disable(Guid userId, CancellationToken ct)
    {
        var settings = await _store.GetAsync(userId, ct).ConfigureAwait(false);
        settings.IsEnabled = false;
        settings.IsTotpConfirmed = false;
        settings.TotpSecret = null;
        await _store.SetAsync(userId, settings, ct).ConfigureAwait(false);
        return Ok();
    }
}
