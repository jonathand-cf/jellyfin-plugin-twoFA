using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Jellyfin.Plugin.TwoFA.Services;

/// <summary>
/// TOTP implementation (RFC 6238).
/// </summary>
public sealed class TotpService : ITotpService
{
    /// <inheritdoc />
    public string GenerateSecret(int byteLength = 20)
    {
        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Secret length must be positive.");
        }

        var buffer = new byte[byteLength];
        RandomNumberGenerator.Fill(buffer);
        return Base32.Encode(buffer);
    }

    /// <inheritdoc />
    public string BuildOtpAuthUri(string issuer, string accountName, string secret, int digits = 6, int periodSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException("Issuer is required.", nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("Account name is required.", nameof(accountName));
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("Secret is required.", nameof(secret));
        }

        string label = Uri.EscapeDataString($"{issuer}:{accountName}");
        string issuerParam = Uri.EscapeDataString(issuer);
        return string.Format(
            CultureInfo.InvariantCulture,
            "otpauth://totp/{0}?secret={1}&issuer={2}&digits={3}&period={4}",
            label,
            secret,
            issuerParam,
            digits,
            periodSeconds);
    }

    /// <inheritdoc />
    public bool ValidateCode(
        string secret,
        string code,
        DateTimeOffset? now = null,
        int allowedDriftSteps = 1,
        int digits = 6,
        int periodSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (periodSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(periodSeconds), "Period must be positive.");
        }

        if (!TryNormalizeCode(code, digits, out var normalized))
        {
            return false;
        }

        byte[] secretBytes = Base32.Decode(secret);
        if (secretBytes.Length == 0)
        {
            return false;
        }

        long timestamp = (long)Math.Floor((now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / (double)periodSeconds);
        int modulo = (int)Math.Pow(10, digits);

        for (int drift = -allowedDriftSteps; drift <= allowedDriftSteps; drift++)
        {
            long counter = timestamp + drift;
            int expected = ComputeTotp(secretBytes, counter, modulo);
            if (string.Equals(expected.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0'), normalized, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int ComputeTotp(byte[] secret, long counter, int modulo)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, counter);

#pragma warning disable CA5350
        // RFC 6238 specifies HMAC-SHA1 as the default for TOTP.
        using var hmac = new HMACSHA1(secret);
#pragma warning restore CA5350
        byte[] hash = hmac.ComputeHash(buffer.ToArray());
        int offset = hash[^1] & 0x0f;
        int binaryCode =
            ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);

        return binaryCode % modulo;
    }

    private static bool TryNormalizeCode(string code, int digits, out string normalized)
    {
        var trimmed = code.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        if (trimmed.Length != digits)
        {
            normalized = string.Empty;
            return false;
        }

        for (int i = 0; i < trimmed.Length; i++)
        {
            if (!char.IsDigit(trimmed[i]))
            {
                normalized = string.Empty;
                return false;
            }
        }

        normalized = trimmed;
        return true;
    }
}
