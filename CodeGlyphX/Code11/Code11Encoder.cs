using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Code11;

/// <summary>
/// Encodes Code 11 barcodes.
/// </summary>
public static class Code11Encoder {
    /// <summary>
    /// Encodes a Code 11 barcode.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = true) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Length == 0) throw new InvalidOperationException("Code 11 content cannot be empty.");

        for (var i = 0; i < content.Length; i++) {
            if (!Code11Tables.ValueTable.ContainsKey(content[i])) {
                throw new InvalidOperationException($"Invalid Code 11 character: '{content[i]}'.");
            }
        }

        var data = new StringBuilder(content);
        if (includeChecksum) {
            var c = CalcChecksum(data.ToString(), 10);
            data.Append(c);
            if (content.Length >= 10) {
                var k = CalcChecksum(data.ToString(), 9);
                data.Append(k);
            }
        }

        var segments = new List<BarSegment>(data.Length * 12 + 16);
        AppendPattern(segments, Code11Tables.StartStopPattern);
        for (var i = 0; i < data.Length; i++) {
            BarcodeSegments.AppendBit(segments, false);
            AppendPattern(segments, Code11Tables.EncodingTable[data[i]]);
        }
        BarcodeSegments.AppendBit(segments, false);
        AppendPattern(segments, Code11Tables.StartStopPattern);

        return new Barcode1D(segments);
    }

    private static void AppendPattern(List<BarSegment> segments, string pattern) {
        for (var i = 0; i < pattern.Length; i++) {
            var isBar = (i % 2) == 0;
            var width = pattern[i] == '1' ? 2 : 1;
            for (var m = 0; m < width; m++) {
                BarcodeSegments.AppendBit(segments, isBar);
            }
        }
    }

    private static char CalcChecksum(string content, int maxWeight) {
        var sum = 0;
        var weight = 1;
        for (var i = content.Length - 1; i >= 0; i--) {
            var ch = content[i];
            if (!Code11Tables.ValueTable.TryGetValue(ch, out var value)) return '-';
            sum += value * weight;
            weight++;
            if (weight > maxWeight) weight = 1;
        }
        var check = sum % 11;
        return check == 10 ? '-' : (char)('0' + check);
    }
}
