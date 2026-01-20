using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Itf;

/// <summary>
/// Encodes Interleaved 2 of 5 (ITF) barcodes.
/// </summary>
public static class ItfEncoder {
    /// <summary>
    /// Encodes an ITF barcode. The input length must be even.
    /// If <paramref name="includeChecksum"/> is true, odd-length inputs get a checksum appended and even-length inputs are validated.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = false) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!RegexCache.DigitsOptional().IsMatch(content)) throw new InvalidOperationException("Can only encode numerical digits (0-9)");
        if (content.Length == 0) throw new InvalidOperationException("Content cannot be empty.");

        if (includeChecksum) {
            if ((content.Length & 1) == 1) {
                content += CalcChecksum(content);
            } else {
                var expected = CalcChecksum(content.AsSpan(0, content.Length - 1));
                if (content[content.Length - 1] != expected) throw new InvalidOperationException("Checksum mismatch");
            }
        }

        if ((content.Length & 1) != 0) {
            throw new InvalidOperationException("Invalid content length. Interleaved 2 of 5 requires an even number of digits.");
        }

        var segments = new List<BarSegment>(content.Length * 9 + 9);

        // Start pattern: narrow bar/space/bar/space.
        AppendSegment(segments, true, 1);
        AppendSegment(segments, false, 1);
        AppendSegment(segments, true, 1);
        AppendSegment(segments, false, 1);

        for (var i = 0; i < content.Length; i += 2) {
            var left = content[i] - '0';
            var right = content[i + 1] - '0';
            var leftPattern = Itf14Tables.DigitPatterns[left];
            var rightPattern = Itf14Tables.DigitPatterns[right];

            for (var p = 0; p < 5; p++) {
                AppendSegment(segments, true, leftPattern[p]);
                AppendSegment(segments, false, rightPattern[p]);
            }
        }

        // Stop pattern: wide bar, narrow space, narrow bar.
        AppendSegment(segments, true, 3);
        AppendSegment(segments, false, 1);
        AppendSegment(segments, true, 1);

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
