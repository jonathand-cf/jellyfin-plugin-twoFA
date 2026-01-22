using System;
using System.Text;

namespace Jellyfin.Plugin.TwoFA.Services;

/// <summary>
/// Minimal Base32 encoder/decoder for TOTP secrets.
/// </summary>
public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Encodes the provided bytes using base32 without padding.
    /// </summary>
    /// <param name="data">The data to encode.</param>
    /// <returns>Base32 encoded data.</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var output = new StringBuilder((int)Math.Ceiling(data.Length / 5d) * 8);
        int bitBuffer = data[0];
        int next = 1;
        int bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    bitBuffer <<= 8;
                    bitBuffer |= data[next++] & 0xff;
                    bitsLeft += 8;
                }
                else
                {
                    int padding = 5 - bitsLeft;
                    bitBuffer <<= padding;
                    bitsLeft += padding;
                }
            }

            int index = (bitBuffer >> (bitsLeft - 5)) & 0x1f;
            bitsLeft -= 5;
            output.Append(Alphabet[index]);
        }

        return output.ToString();
    }

    /// <summary>
    /// Decodes a base32 string.
    /// </summary>
    /// <param name="input">The base32 data.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] Decode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<byte>();
        }

        string normalized = input.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        int estimatedLength = normalized.Length * 5 / 8;
        var output = new byte[estimatedLength];
        int outputIndex = 0;
        int bitBuffer = 0;
        int bitsLeft = 0;

        foreach (char c in normalized)
        {
            int value = Alphabet.IndexOf(c.ToString(), StringComparison.Ordinal);
            if (value < 0)
            {
                throw new FormatException($"Invalid Base32 character '{c}'.");
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output[outputIndex++] = (byte)((bitBuffer >> (bitsLeft - 8)) & 0xff);
                bitsLeft -= 8;
            }
        }

        if (outputIndex == output.Length)
        {
            return output;
        }

        Array.Resize(ref output, outputIndex);
        return output;
    }
}
