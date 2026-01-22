using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Represents the result of a 2FA login attempt.
/// </summary>
public sealed class SsoAuthenticationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether login was successful.
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin server address.
    /// </summary>
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an error message for failed logins.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the authentication result on success.
    /// </summary>
    public AuthenticationResult? AuthenticatedUser { get; set; }
}
