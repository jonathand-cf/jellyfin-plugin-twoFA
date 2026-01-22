using System;

namespace Jellyfin.Plugin.TwoFA.Services;

/// <summary>
/// Abstraction for TOTP generation and validation.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a new base32-encoded TOTP secret.
    /// </summary>
    /// <param name="byteLength">Number of random bytes to use.</param>
    /// <returns>Base32-encoded secret.</returns>
    string GenerateSecret(int byteLength = 20);

    /// <summary>
    /// Builds an otpauth URI for QR enrollment.
    /// </summary>
    /// <param name="issuer">The issuer name.</param>
    /// <param name="accountName">The account name.</param>
    /// <param name="secret">The base32 secret.</param>
    /// <param name="digits">Digits for the TOTP.</param>
    /// <param name="periodSeconds">Period for the TOTP.</param>
    /// <returns>The otpauth URI.</returns>
    string BuildOtpAuthUri(string issuer, string accountName, string secret, int digits = 6, int periodSeconds = 30);

    /// <summary>
    /// Validates a user-supplied TOTP code.
    /// </summary>
    /// <param name="secret">The base32 secret.</param>
    /// <param name="code">The code provided by the user.</param>
    /// <param name="now">Optional time override.</param>
    /// <param name="allowedDriftSteps">Allowed time drift steps.</param>
    /// <param name="digits">Digits for the TOTP.</param>
    /// <param name="periodSeconds">Period for the TOTP.</param>
    /// <returns><c>true</c> if the code is valid.</returns>
    bool ValidateCode(
        string secret,
        string code,
        DateTimeOffset? now = null,
        int allowedDriftSteps = 1,
        int digits = 6,
        int periodSeconds = 30);
}
