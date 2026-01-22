namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Confirmation request payload.
/// </summary>
public sealed class ConfirmRequest
{
    /// <summary>
    /// Gets or sets the TOTP code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
