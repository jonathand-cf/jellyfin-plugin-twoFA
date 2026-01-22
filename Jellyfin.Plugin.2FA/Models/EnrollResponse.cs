namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Enrollment response payload.
/// </summary>
public sealed class EnrollResponse
{
    /// <summary>
    /// Gets or sets the base32-encoded secret.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the otpauth URI.
    /// </summary>
    public string OtpAuthUri { get; set; } = string.Empty;
}
