using System;
using System.Threading;

namespace CodeGlyphX.Internal;

/// <summary>Selects the first cutoff for each distinct binary mask without changing caller order.</summary>
internal static class ScanlineThresholds {
    /// <summary>
    /// Compacts the supplied gray cutoffs (0..255) in place. Thresholding one
    /// immutable luminance row is monotone: equal dark counts imply equal pixels,
    /// runs and fit errors. Cancellation returns zero usable hypotheses.
    /// </summary>
    internal static int RetainDistinct(ReadOnlySpan<byte> luminance, CancellationToken cancellationToken,
        Span<int> thresholds, int count) {
        Span<int> darkCounts = stackalloc int[256];
        darkCounts.Clear();
        for (var x = 0; x < luminance.Length; x++) {
            if ((x & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return 0;
            darkCounts[luminance[x]]++;
        }
        var below = 0;
        for (var value = 0; value < darkCounts.Length; value++) {
            var atValue = darkCounts[value];
            darkCounts[value] = below;
            below += atValue;
        }
        var distinct = 0;
        for (var i = 0; i < count; i++) {
            var threshold = thresholds[i];
            var duplicate = false;
            for (var previous = 0; previous < distinct; previous++) {
                if (darkCounts[thresholds[previous]] != darkCounts[threshold]) continue;
                duplicate = true;
                break;
            }
            if (!duplicate) thresholds[distinct++] = threshold;
        }
        return distinct;
    }
}
