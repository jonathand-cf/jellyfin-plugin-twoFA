namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Login request payload for the 2FA SSO page.
/// </summary>
public sealed class TwoFactorLoginRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-time password.
    /// </summary>
    public string? Otp { get; set; }
}
