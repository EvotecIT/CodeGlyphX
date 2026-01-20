using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.Pharmacode;

/// <summary>
/// Encodes Pharmacode (two-track) barcodes.
/// </summary>
public static class PharmacodeTwoTrackEncoder {
    internal const int MinValue = 4;
    internal const int MaxValue = 64570080;
    internal const int MinBars = 2;
    internal const int MaxBars = 16;

    /// <summary>
    /// Encodes a Pharmacode (two-track) barcode value into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("Pharmacode (two-track) content cannot be empty.");
        if (!int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) {
            throw new InvalidOperationException("Pharmacode (two-track) expects a numeric value.");
        }
        return Encode(value);
    }

    /// <summary>
    /// Encodes a Pharmacode (two-track) barcode value into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix Encode(int value) {
        if (value < MinValue || value > MaxValue) {
            throw new InvalidOperationException($"Pharmacode (two-track) value must be in range {MinValue}-{MaxValue}.");
        }

        var digits = BuildDigits(value);
        if (digits.Count < MinBars || digits.Count > MaxBars) {
            throw new InvalidOperationException("Pharmacode (two-track) contains an unsupported number of bars.");
        }

        var width = digits.Count * 2 - 1;
        var matrix = new BitMatrix(width, 2);

        var x = 0;
        foreach (var digit in digits) {
            var bottom = (digit & 0b01) != 0;
            var top = (digit & 0b10) != 0;
            SetBar(matrix, x, top, bottom);
            x += 2;
        }

        return matrix;
    }

    private static List<int> BuildDigits(int value) {
        var digits = new List<int>(MaxBars);
        var remaining = value;
        while (remaining > 0) {
            var remainder = remaining % 3;
            int digit;
            if (remainder == 0) {
                digit = 3;
                remaining = (remaining - 3) / 3;
            } else {
                digit = remainder;
                remaining = (remaining - remainder) / 3;
            }
            digits.Add(digit);
        }
        digits.Reverse();
        return digits;
    }

    private static void SetBar(BitMatrix matrix, int x, bool top, bool bottom) {
        var bottomRow = matrix.Height - 1;
        if (bottom) matrix[x, bottomRow] = true;
        if (top) matrix[x, 0] = true;
    }
}
