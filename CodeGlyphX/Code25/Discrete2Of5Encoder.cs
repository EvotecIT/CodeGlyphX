using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;
using CodeGlyphX.Itf;

namespace CodeGlyphX.Code25;

internal static class Discrete2Of5Encoder {
    public static Barcode1D Encode(string content, bool includeChecksum, int[] startBars, int[] stopBars) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!RegexCache.DigitsOptional().IsMatch(content)) throw new InvalidOperationException("Can only encode numerical digits (0-9)");
        if (content.Length == 0) throw new InvalidOperationException("Content cannot be empty.");

        if (includeChecksum) {
            content += CalcChecksum(content.AsSpan());
        }

        var segments = new List<BarSegment>(content.Length * 14 + 32);

        AppendBarsPattern(segments, startBars, appendTrailingSpace: true);

        for (var i = 0; i < content.Length; i++) {
            var digit = content[i] - '0';
            var pattern = Itf14Tables.DigitPatterns[digit];
            AppendBarsPattern(segments, pattern, appendTrailingSpace: true);
        }

        AppendBarsPattern(segments, stopBars, appendTrailingSpace: false);

        return new Barcode1D(segments);
    }

    private static char CalcChecksum(ReadOnlySpan<char> content) {
        var sum = 0;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            var weight = ((content.Length - 1 - i) & 1) == 0 ? 3 : 1;
            sum += digit * weight;
        }
        var check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    private static void AppendBarsPattern(List<BarSegment> segments, int[] bars, bool appendTrailingSpace) {
        for (var i = 0; i < bars.Length; i++) {
            AppendSegment(segments, isBar: true, bars[i]);
            if (appendTrailingSpace || i + 1 < bars.Length) {
                AppendSegment(segments, isBar: false, 1);
            }
        }
    }

    private static void AppendSegment(List<BarSegment> segments, bool isBar, int modules) {
        if (modules <= 0) return;
        if (segments.Count > 0) {
            var last = segments[segments.Count - 1];
            if (last.IsBar == isBar) {
                segments[segments.Count - 1] = new BarSegment(isBar, checked(last.Modules + modules));
                return;
            }
        }
        segments.Add(new BarSegment(isBar, modules));
    }
}
