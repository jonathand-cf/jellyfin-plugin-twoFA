namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Verification response payload.
/// </summary>
public sealed class VerifyResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether verification succeeded.
    /// </summary>
    public bool Success { get; set; }
}
