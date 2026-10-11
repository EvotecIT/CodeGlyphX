using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Read from the surviving stop toward the crop. Right indicators identify
    // logical rows and metadata; absent prefix characters remain erasures.
    private static bool TryReadRightFramedRows(int width, int height, PixelRunCache runCache,
        CancellationToken token, bool correctPrintGain, out Pdf417Decoded decoded, out bool hasRightPattern) {
        decoded = null!;
        hasRightPattern = false;
        for (var axis = 0; axis < 2; axis++) {
            var down = axis == 0 ? height : width;
            for (var reverse = 0; reverse < 2; reverse++) for (var invert = 0; invert < 2; invert++) {
                var orientationHasRight = false;
                var runs = new List<int>();
                foreach (var slope in PixelRowSlopes) {
                    if (slope != 0 && !orientationHasRight) break;
                    var sets = new Dictionary<int, PixelRightRowSet>();
                    for (var y = 0; y < down; y++) {
                        if (!runCache.TryRead(axis, reverse != 0, invert != 0, y, slope, runs, token, out var first)) return false;
                        var firstRun = first ? 0 : 1;
                        var lastStop = runs.Count - PixelStopRuns.Length;
                        if (((lastStop - firstRun) & 1) != 0) lastStop--;
                        var rightPixel = 0;
                        for (var i = 0; i < lastStop; i++) {
                            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                            rightPixel += runs[i];
                        }
                        // Rightmost guards get priority when the finite spatial
                        // candidate limit is reached on a busy scanline.
                        for (var stop = lastStop; stop >= firstRun + 16; stop -= 2) {
                            if (stop != lastStop) rightPixel -= runs[stop] + runs[stop + 1];
                            if (DecodeBudget.ShouldAbort(token)) return false;
                            if (!MatchPixelRuns(runs, stop, PixelStopRuns, 18) ||
                                !TryReadPixelIndicator(runs, stop - 8, correctPrintGain, out var cluster, out var right)) continue;
                            var logicalRow = right / 30 * 3 + cluster;
                            if (logicalRow >= 90) continue;
                            var bin = rightPixel / 32;
                            if (!sets.TryGetValue(bin, out var set)) {
                                const int maximumSpatialCandidates = 32;
                                if (sets.Count == maximumSpatialCandidates) continue;
                                sets[bin] = set = new PixelRightRowSet();
                            }
                            var words = new List<int> { right };
                            for (var at = stop - 16; at >= firstRun && words.Count <= 30; at -= 8) {
                                if (DecodeBudget.ShouldAbort(token)) return false;
                                if (!ReadPixelCodeword(runs, at, cluster, out var word, correctPrintGain: correctPrintGain)) break;
                                words.Add(word);
                            }
                            if (words.Count > 1) set.Observe(logicalRow, words.ToArray(), y);
                        }
                    }
                    foreach (var set in sets.Values) {
                        if (DecodeBudget.ShouldAbort(token)) return false;
                        if (!set.TryAssemble(out var rows, out var columns, out var count, out var ec)) continue;
                        hasRightPattern = orientationHasRight = true;
                        if (TryDecodePixelAssembly(rows, columns, count, ec, true, token, out decoded)) return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool TryReadPixelIndicator(List<int> runs, int start, bool correctPrintGain, out int cluster, out int word) {
        for (var c = 0; c < 3; c++) if (ReadPixelCodeword(runs, start, c, out word, approximate: false)) {
            cluster = c;
            return true;
        }
        for (var c = 0; c < 3; c++) if (ReadPixelCodeword(runs, start, c, out word, correctPrintGain: correctPrintGain)) {
            cluster = c;
            return true;
        }
        cluster = -1;
        word = 0;
        return false;
    }
}
