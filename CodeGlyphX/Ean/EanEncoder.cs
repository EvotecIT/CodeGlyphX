using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Ean;

/// <summary>
/// Encodes EAN-8 and EAN-13 barcodes.
/// </summary>
public static class EanEncoder {
    /// <summary>
    /// Encodes an EAN barcode (EAN-8 or EAN-13, depending on input length). Use "+NN" or "+NNNNN" to append add-ons.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        SplitAddOn(content, out content, out var addOn);

        if (!RegexCache.DigitsOptional().IsMatch(content)) throw new InvalidOperationException("Can only encode numerical digits (0-9)");

        content = NormalizeContent(content);

        var baseCode = content.Length == 8 ? EncodeEan8(content) : EncodeEan13(content);
        return AppendAddOn(baseCode, addOn);
    }

    private static void SplitAddOn(string content, out string core, out string? addOn) {
        addOn = null;
        var plusIndex = content.IndexOf('+');
        if (plusIndex < 0) {
            core = content;
            return;
        }
        if (content.IndexOf('+', plusIndex + 1) >= 0) throw new InvalidOperationException("Only one add-on separator '+' is supported.");
        addOn = content.Substring(plusIndex + 1);
        if (addOn.Length == 0) throw new InvalidOperationException("Add-on digits are required after '+'.");
        core = content.Substring(0, plusIndex);
    }

    private static string NormalizeContent(string content) {
        if (content.Length == 7 || content.Length == 12) {
            var c = CalcChecksum(content);
            return content + c;
        }

        if (content.Length == 8 || content.Length == 13) {
            var c = CalcChecksum(content.Substring(0, content.Length - 1));
            if (content[content.Length - 1] != c) throw new InvalidOperationException("Checksum mismatch");
            return content;
        }

        throw new InvalidOperationException("Invalid content length. Should be 7 or 12 if the code does not include a checksum, 8 or 13 if the code already includes a checksum");
    }

    private static Barcode1D AppendAddOn(Barcode1D baseCode, string? addOn) {
        if (string.IsNullOrEmpty(addOn)) return baseCode;
        var segments = new List<BarSegment>(baseCode.Segments);
        EanAddOn.AppendAddOn(segments, addOn!);
        return new Barcode1D(segments);
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
