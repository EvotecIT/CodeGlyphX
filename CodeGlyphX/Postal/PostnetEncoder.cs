using System;
using System.Globalization;

namespace CodeGlyphX.Postal;

/// <summary>
/// Encodes POSTNET barcodes.
/// </summary>
public static class PostnetEncoder {
    /// <summary>
    /// Encodes a POSTNET barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix Encode(string content) {
        return PostalEncoder.Encode(content, invert: false);
    }
}

internal static class PostalEncoder {
    internal static BitMatrix Encode(string content, bool invert) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("POSTNET/PLANET content cannot be empty.");

        if (!ulong.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out _)) {
            throw new InvalidOperationException("POSTNET/PLANET expects numeric digits only.");
        }

        if (content.Length > 12) throw new InvalidOperationException("POSTNET/PLANET supports up to 12 digits including checksum.");

        string payload;
        if (PostalTables.IsChecksumLength(content.Length)) {
            var check = content[content.Length - 1] - '0';
            var expected = PostalTables.CalcChecksum(content.AsSpan(0, content.Length - 1));
            if (expected < 0 || check != expected) {
                throw new InvalidOperationException("POSTNET/PLANET checksum is invalid.");
            }
            payload = content;
        } else {
            if (content.Length > 11) throw new InvalidOperationException("POSTNET/PLANET data must be 11 digits or fewer.");
            var expected = PostalTables.CalcChecksum(content);
            if (expected < 0) throw new InvalidOperationException("POSTNET/PLANET expects numeric digits only.");
            payload = content + (char)('0' + expected);
        }

        var digits = payload.ToCharArray();
        var barCount = 2 + digits.Length * 5;
        var width = barCount * 2 - 1;
        var matrix = new BitMatrix(width, 2);

        var x = 0;
        SetBar(matrix, x, tall: true);
        x += 2;

        for (var d = 0; d < digits.Length; d++) {
            var digit = digits[d] - '0';
            var pattern = PostalTables.PostnetPatterns[digit];
            for (var i = 0; i < 5; i++) {
                var tall = invert ? !pattern[i] : pattern[i];
                SetBar(matrix, x, tall);
                x += 2;
            }
        }

        SetBar(matrix, width - 1, tall: true);
        return matrix;
    }

    private static void SetBar(BitMatrix matrix, int x, bool tall) {
        var bottom = matrix.Height - 1;
        matrix[x, bottom] = true;
        if (tall) matrix[x, 0] = true;
    }
}
