using System;
using System.Linq;
using System.Text.RegularExpressions;
using CodeMatrix.Internal;

namespace CodeMatrix.UpcA;

/// <summary>
/// Encodes UPC-A barcodes.
/// </summary>
public static class UpcAEncoder {
    /// <summary>
    /// Encodes a UPC-A barcode.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!Regex.IsMatch(content, "^[0-9]*$")) throw new InvalidOperationException("Can only encode numerical digits (0-9)");

        if (content.Length == 11) {
            var c = CalcChecksum(content);
            content += c;
        } else if (content.Length == 12) {
            var c = CalcChecksum(content.Substring(0, content.Length - 1));
            if (content[^1] != c) throw new InvalidOperationException("Checksum mismatch");
        }

        if (content.Length != 12) {
            throw new InvalidOperationException("Invalid content length. Should be 11 if the code does not include a checksum, 12 if the code already includes a checksum");
        }

        var segments = new List<BarSegment>(95);
        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        var index = 0;
        foreach (var ch in content) {
            var encoded = UpcATables.EncodingTable[ch];
            var bits = index < 6 ? encoded.Left : encoded.Right;
            if (index == 6) {
                BarcodeSegments.AppendBits(segments, new[] { false, true, false, true, false });
            }
            BarcodeSegments.AppendBits(segments, bits);
            index++;
        }
        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        return new Barcode1D(segments);
    }

    private static char CalcChecksum(string content) {
        var digits = content.Select(c => c - '0').ToArray();
        var sum = 3 * (digits[0] + digits[2] + digits[4] + digits[6] + digits[8] + digits[10]);
        sum += digits[1] + digits[3] + digits[5] + digits[7] + digits[9];
        sum %= 10;
        sum = sum != 0 ? 10 - sum : 0;
        return (char)(sum + '0');
    }
}
