using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Itf;

/// <summary>
/// Encodes ITF-14 (Interleaved 2 of 5) barcodes.
/// </summary>
public static class Itf14Encoder {
    /// <summary>
    /// Encodes an ITF-14 barcode. Accepts 13 digits (checksum calculated) or 14 digits (checksum verified).
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (!RegexCache.DigitsOptional().IsMatch(content)) throw new InvalidOperationException("Can only encode numerical digits (0-9)");

        if (content.Length == 13) {
            content += CalcChecksum(content);
        } else if (content.Length == 14) {
            var expected = CalcChecksum(content.AsSpan(0, 13));
            if (content[13] != expected) throw new InvalidOperationException("Checksum mismatch");
        } else {
            throw new InvalidOperationException("Invalid content length. Should be 13 if the code does not include a checksum, 14 if the code already includes a checksum");
        }

        var segments = new List<BarSegment>(135);

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
