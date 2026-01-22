using System;

namespace Jellyfin.Plugin.TwoFA.Models;

/// <summary>
/// Per-user 2FA settings.
/// </summary>
public sealed class TwoFactorUserSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether 2FA is enabled for the user.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the base32-encoded TOTP secret.
    /// </summary>
    public string? TotpSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the TOTP secret has been confirmed.
    /// </summary>
    public bool IsTotpConfirmed { get; set; }

    /// <summary>
    /// Gets or sets the last successful verification time.
    /// </summary>
    public DateTimeOffset? LastVerifiedUtc { get; set; }
}
