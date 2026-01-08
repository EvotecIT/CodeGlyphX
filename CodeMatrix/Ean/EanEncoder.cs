using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeMatrix.Internal;

namespace CodeMatrix.Ean;

/// <summary>
/// Encodes EAN-8 and EAN-13 barcodes.
/// </summary>
public static class EanEncoder {
    /// <summary>
    /// Encodes an EAN barcode (EAN-8 or EAN-13, depending on input length).
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!Regex.IsMatch(content, "^[0-9]*$")) throw new InvalidOperationException("Can only encode numerical digits (0-9)");

        var checksum = 0;
        if (content.Length == 7 || content.Length == 12) {
            var c = CalcChecksum(content);
            content += c;
            checksum = c - '0';
        } else if (content.Length == 8 || content.Length == 13) {
            var c = CalcChecksum(content.Substring(0, content.Length - 1));
            if (content[^1] != c) throw new InvalidOperationException("Checksum mismatch");
            checksum = c - '0';
        }

        if (content.Length == 8) {
            return EncodeEan8(content);
        }
        if (content.Length == 13) {
            return EncodeEan13(content);
        }

        throw new InvalidOperationException("Invalid content length. Should be 7 or 12 if the code does not include a checksum, 8 or 13 if the code already includes a checksum");
    }

    private static Barcode1D EncodeEan8(string content) {
        var segments = new List<BarSegment>(67);
        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        var index = 0;
        foreach (var ch in content) {
            var encoded = EanTables.EncodingTable[ch];
            var bits = index < 4 ? encoded.LeftOdd : encoded.Right;
            if (index == 4) {
                BarcodeSegments.AppendBits(segments, new[] { false, true, false, true, false });
            }
            BarcodeSegments.AppendBits(segments, bits);
            index++;
        }
        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        return new Barcode1D(segments);
    }

    private static Barcode1D EncodeEan13(string content) {
        var segments = new List<BarSegment>(95);
        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        bool[]? parity = null;
        var index = 0;
        foreach (var ch in content) {
            var encoded = EanTables.EncodingTable[ch];
            if (parity is null) {
                parity = encoded.Checksum;
                index++;
                continue;
            }

            var bits = index >= 7
                ? encoded.Right
                : (parity[index - 1] ? encoded.LeftEven : encoded.LeftOdd);

            if (index == 7) {
                BarcodeSegments.AppendBits(segments, new[] { false, true, false, true, false });
            }

            BarcodeSegments.AppendBits(segments, bits);
            index++;
        }

        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        return new Barcode1D(segments);
    }

    private static char CalcChecksum(string content) {
        var triple = content.Length == 7;
        var sum = 0;
        for (var i = 0; i < content.Length; i++) {
            var val = content[i] - '0';
            if (triple) val *= 3;
            triple = !triple;
            sum += val;
        }
        return (char)((10 - sum % 10) % 10 + '0');
    }
}
