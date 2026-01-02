using System;
using System.Collections.Generic;
using System.Text;

namespace CodeMatrix;

/// <summary>
/// Base32 helpers for <c>otpauth://</c> secrets.
/// </summary>
public static class OtpAuthSecret {
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Decodes a Base32-encoded secret into raw bytes.
    /// </summary>
    /// <remarks>
    /// Normalization rules:
    /// <list type="bullet">
    /// <item><description>Ignores ASCII whitespace, <c>-</c> separators and <c>=</c> padding.</description></item>
    /// <item><description>Accepts upper and lower case.</description></item>
    /// </list>
    /// </remarks>
    public static byte[] FromBase32(string base32) {
        if (base32 is null) throw new ArgumentNullException(nameof(base32));

        // Normalization rules:
        // - Trim and ignore ASCII whitespace and '-' separators
        // - Accept lower/upper case
        // - Accept optional '=' padding (ignored)
        var cleaned = new StringBuilder(base32.Length);
        for (var i = 0; i < base32.Length; i++) {
            var c = base32[i];
            if (c is '=' or '-' or ' ' or '\t' or '\r' or '\n') continue;
            cleaned.Append(char.ToUpperInvariant(c));
        }

        if (cleaned.Length == 0) throw new FormatException("Base32 string is empty.");

        var result = new List<byte>(cleaned.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;

        for (var i = 0; i < cleaned.Length; i++) {
            var c = cleaned[i];
            var val = DecodeBase32Char(c);
            if (val < 0) throw new FormatException($"Invalid Base32 character: '{c}'.");

            buffer = (buffer << 5) | val;
            bits += 5;

            while (bits >= 8) {
                bits -= 8;
                result.Add((byte)((buffer >> bits) & 0xFF));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Encodes bytes as Base32 without padding (uppercase).
    /// </summary>
    public static string ToBase32(byte[] bytes) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        return ToBase32Core(bytes, 0, bytes.Length);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes bytes as Base32 without padding (uppercase).
    /// </summary>
    public static string ToBase32(ReadOnlySpan<byte> bytes) => ToBase32Core(bytes);
#endif

    private static string ToBase32Core(byte[] bytes, int offset, int count) {
        if (count == 0) return string.Empty;

        var sb = new StringBuilder((count * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;

        for (var i = 0; i < count; i++) {
            buffer = (buffer << 8) | bytes[offset + i];
            bits += 8;
            while (bits >= 5) {
                bits -= 5;
                var idx = (buffer >> bits) & 31;
                sb.Append(Alphabet[idx]);
                buffer &= (1 << bits) - 1;
            }
        }

        if (bits > 0) {
            var idx = (buffer << (5 - bits)) & 31;
            sb.Append(Alphabet[idx]);
        }

        // No '=' padding (otpauth-compatible).
        return sb.ToString();
    }

#if NET8_0_OR_GREATER
    private static string ToBase32Core(ReadOnlySpan<byte> bytes) {
        if (bytes.Length == 0) return string.Empty;

        var sb = new StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;

        for (var i = 0; i < bytes.Length; i++) {
            buffer = (buffer << 8) | bytes[i];
            bits += 8;
            while (bits >= 5) {
                bits -= 5;
                var idx = (buffer >> bits) & 31;
                sb.Append(Alphabet[idx]);
                buffer &= (1 << bits) - 1;
            }
        }

        if (bits > 0) {
            var idx = (buffer << (5 - bits)) & 31;
            sb.Append(Alphabet[idx]);
        }

        return sb.ToString();
    }
#endif

    private static int DecodeBase32Char(char c) {
        if (c is >= 'A' and <= 'Z') return c - 'A';
        if (c is >= '2' and <= '7') return 26 + (c - '2');
        return -1;
    }
}
