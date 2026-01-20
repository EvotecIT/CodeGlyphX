using System;
using System.Collections.Generic;
using System.Globalization;
using CodeGlyphX.Code39;

namespace CodeGlyphX.Code32;

/// <summary>
/// Encodes Code 32 (Italian Pharmacode) barcodes.
/// </summary>
public static class Code32Encoder {
    /// <summary>
    /// Encodes a Code 32 barcode from 1-8 digits (left-padded to 8, checksum appended) or 9 digits (checksum validated).
    /// A leading 'A' is allowed and ignored.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("Code 32 content cannot be empty.");

        if (content.Length > 0 && (content[0] == 'A' || content[0] == 'a')) {
            content = content.Substring(1);
        }

        if (content.Length > 9) {
            throw new InvalidOperationException("Code 32 expects up to 9 digits (8 data digits plus checksum).");
        }

        if (content.Length < 8) {
            content = content.PadLeft(8, '0');
        }

        if (!ulong.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out _)) {
            throw new InvalidOperationException("Code 32 expects numeric digits only.");
        }

        var digits = content.ToCharArray();
        var check = Code32Tables.CalcChecksum(digits);
        if (digits.Length == 8) {
            content += (char)('0' + check);
        } else {
            if (digits[8] != (char)('0' + check)) {
                throw new InvalidOperationException("Code 32 checksum is invalid.");
            }
        }

        var value = int.Parse(content, CultureInfo.InvariantCulture);
        var base32 = Code32Tables.ToBase32(value);
        return Code39Encoder.Encode(base32, includeChecksum: false, fullAsciiMode: false);
    }
}

internal static class Code32Tables {
    internal const string Alphabet = "0123456789BCDFGHJKLMNPQRSTUVWXYZ";

    internal static int CalcChecksum(ReadOnlySpan<char> digits) {
        var sum = 0;
        var len = digits.Length >= 8 ? 8 : digits.Length;
        for (var i = 0; i < len; i++) {
            var digit = digits[i] - '0';
            if ((uint)digit > 9) return -1;
            var weight = (i & 1) == 0 ? 1 : 2;
            var value = digit * weight;
            sum += value < 10 ? value : (value / 10 + value % 10);
        }
        return sum % 10;
    }

    internal static string ToBase32(int value) {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var chars = new char[6];
        for (var i = 5; i >= 0; i--) {
            var digit = value % 32;
            chars[i] = Alphabet[digit];
            value /= 32;
        }
        if (value != 0) throw new InvalidOperationException("Code 32 value is too large to encode.");
        return new string(chars);
    }

    internal static bool TryFromBase32(ReadOnlySpan<char> text, out int value) {
        value = 0;
        if (text.Length != 6) return false;
        for (var i = 0; i < text.Length; i++) {
            var ch = char.ToUpperInvariant(text[i]);
            var idx = Alphabet.IndexOf(ch);
            if (idx < 0) return false;
            value = checked(value * 32 + idx);
        }
        return true;
    }
}
