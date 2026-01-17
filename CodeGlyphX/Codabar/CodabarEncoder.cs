using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Codabar;

/// <summary>
/// Encodes Codabar barcodes.
/// </summary>
public static class CodabarEncoder {
    /// <summary>
    /// Encodes a Codabar barcode.
    /// </summary>
    public static Barcode1D Encode(string content, char start = 'A', char stop = 'B') {
        if (content is null) throw new ArgumentNullException(nameof(content));

        content = content.Trim();
        if (content.Length >= 2 && IsStartStop(content[0]) && IsStartStop(content[content.Length - 1])) {
            start = NormalizeStartStop(content[0]);
            stop = NormalizeStartStop(content[content.Length - 1]);
            content = content.Substring(1, content.Length - 2);
        }

        start = NormalizeStartStop(start);
        stop = NormalizeStartStop(stop);

        var segments = new List<BarSegment>((content.Length + 2) * 16);

        AppendChar(segments, start);
        for (var i = 0; i < content.Length; i++) {
            BarcodeSegments.AppendBit(segments, false);
            AppendChar(segments, NormalizeData(content[i]));
        }
        BarcodeSegments.AppendBit(segments, false);
        AppendChar(segments, stop);

        return new Barcode1D(segments);
    }

    private static void AppendChar(List<BarSegment> segments, char ch) {
        if (!CodabarTables.EncodingTable.TryGetValue(ch, out var pattern)) {
            throw new InvalidOperationException($"Invalid Codabar character: '{ch}'.");
        }

        for (var i = 0; i < pattern.Length; i++) {
            var isBar = (i % 2) == 0;
            var width = pattern[i] == '1' ? 2 : 1;
            for (var m = 0; m < width; m++) {
                BarcodeSegments.AppendBit(segments, isBar);
            }
        }
    }

    private static bool IsStartStop(char ch) {
        ch = char.ToUpperInvariant(ch);
        return CodabarTables.StartStopChars.Contains(ch);
    }

    private static char NormalizeStartStop(char ch) {
        ch = char.ToUpperInvariant(ch);
        if (!CodabarTables.StartStopChars.Contains(ch)) {
            throw new InvalidOperationException($"Invalid Codabar start/stop: '{ch}'.");
        }
        return ch;
    }

    private static char NormalizeData(char ch) {
        ch = char.ToUpperInvariant(ch);
        if (!CodabarTables.EncodingTable.ContainsKey(ch) || CodabarTables.StartStopChars.Contains(ch)) {
            throw new InvalidOperationException($"Invalid Codabar data character: '{ch}'.");
        }
        return ch;
    }
}
