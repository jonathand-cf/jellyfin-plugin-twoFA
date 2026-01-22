namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Verification request payload.
/// </summary>
public sealed class VerifyRequest
{
    /// <summary>
    /// Gets or sets the TOTP code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
