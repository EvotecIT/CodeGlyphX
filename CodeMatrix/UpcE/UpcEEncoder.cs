using System;
using System.Linq;
using System.Text.RegularExpressions;
using CodeMatrix.Internal;

namespace CodeMatrix.UpcE;

/// <summary>
/// Encodes UPC-E barcodes.
/// </summary>
public static class UpcEEncoder {
    /// <summary>
    /// Encodes a UPC-E barcode.
    /// </summary>
    public static Barcode1D Encode(string content, UpcENumberSystem numberSystem = UpcENumberSystem.Zero) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!Regex.IsMatch(content, "^[0-9]*$")) throw new InvalidOperationException("Can only encode numerical digits (0-9)");
        if (numberSystem != UpcENumberSystem.Zero && numberSystem != UpcENumberSystem.One) {
            throw new InvalidOperationException("Only number systems 0 and 1 are supported by UPC E");
        }
        if (content.Length != 6) throw new InvalidOperationException("Invalid content length. Should be 6");

        var segments = new List<BarSegment>(51);
        var upcA = GetUpcAFromUpcE(content, numberSystem);
        var checkDigit = upcA[^1];
        var parityPatterns = UpcETables.ParityPatternTable[checkDigit];
        var parity = numberSystem == UpcENumberSystem.Zero ? parityPatterns.NumberSystemZero : parityPatterns.NumberSystemOne;

        BarcodeSegments.AppendBits(segments, new[] { true, false, true });
        for (var i = 0; i < content.Length; i++) {
            var encoded = UpcETables.EncodingTable[content[i]];
            BarcodeSegments.AppendBits(segments, parity[i] == UpcETables.Parity.Even ? encoded.Even : encoded.Odd);
        }
        BarcodeSegments.AppendBits(segments, new[] { false, true, false, true, false, true });
        return new Barcode1D(segments);
    }

    private static string GetUpcAFromUpcE(string content, UpcENumberSystem numberSystem) {
        var text = (numberSystem == UpcENumberSystem.Zero ? "0" : "1");
        switch (content[^1]) {
            case '0':
            case '1':
            case '2':
                text += $"{content.Substring(0, 2)}{content[^1]}0000{content.Substring(2, 3)}";
                break;
            case '3':
                text = text + content.Substring(0, 3) + "00000" + content.Substring(3, 2);
                break;
            case '4':
                text = text + content.Substring(0, 4) + "00000" + content.Substring(4, 1);
                break;
            default:
                text += $"{content.Substring(0, 5)}0000{content[^1]}";
                break;
        }
        return text + CalculateUpcAChecksum(text);
    }

    private static char CalculateUpcAChecksum(string content) {
        var digits = content.Select(c => c - '0').ToArray();
        var sum = 3 * (digits[0] + digits[2] + digits[4] + digits[6] + digits[8] + digits[10]);
        sum += digits[1] + digits[3] + digits[5] + digits[7] + digits[9];
        sum %= 10;
        sum = sum != 0 ? 10 - sum : 0;
        return (char)(sum + '0');
    }
}
