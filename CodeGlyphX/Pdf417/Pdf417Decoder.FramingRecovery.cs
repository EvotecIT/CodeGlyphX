using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Three consistent start/left-indicator rows establish one immutable
    // header and sampling anchor before rows with damaged framing are read.
    private readonly struct PixelFramingCandidate {
        internal readonly int Axis, Columns, Count, Ec;
        internal readonly bool Reverse, Invert;
        internal readonly double Slope, IndicatorX, Pitch;

        internal PixelFramingCandidate(int axis, bool reverse, bool invert, double slope,
            double indicatorX, double pitch, int columns, int count, int ec) {
            Axis = axis; Reverse = reverse; Invert = invert; Slope = slope;
            IndicatorX = indicatorX; Pitch = pitch; Columns = columns; Count = count; Ec = ec;
        }

        internal int Left(int row) => 30 * (row / 3) +
            (row % 3 == 0 ? (Count - 1) / 3 : row % 3 == 1 ? Ec * 3 + (Count - 1) % 3 : Columns - 1);

        internal int Right(int row) => 30 * (row / 3) +
            (row % 3 == 0 ? Columns - 1 : row % 3 == 1 ? (Count - 1) / 3 : Ec * 3 + (Count - 1) % 3);
    }

    private static void RetainFramingCandidate(List<PixelFramingCandidate> candidates, PixelRowHints hints,
        List<int> runs, int start, int leftPixel, int axis, bool reverse, bool invert, double slope) {
        if (!hints.TryGetFramingMetadata(out var columns, out var count, out var ec)) return;
        var width = 0;
        for (var i = 0; i < 8; i++) width += runs[start + i];
        var pitch = width / 17.0;
        var indicatorX = leftPixel + width;
        foreach (var candidate in candidates) {
            if (candidate.Axis == axis && candidate.Reverse == reverse && candidate.Invert == invert &&
                candidate.Slope.Equals(slope) && candidate.Columns == columns && candidate.Count == count && candidate.Ec == ec &&
                Math.Abs(candidate.IndicatorX - indicatorX) <= pitch && Math.Abs(candidate.Pitch - pitch) <= pitch * 0.05) return;
        }
        const int maximumCandidates = 16;
        if (candidates.Count < maximumCandidates)
            candidates.Add(new PixelFramingCandidate(axis, reverse, invert, slope, indicatorX, pitch, columns, count, ec));
    }

    private static bool TryReadDamagedFrameRows(int width, int height, PixelRunCache cache,
        List<PixelFramingCandidate> candidates, CancellationToken token, out Pdf417Decoded decoded) {
        decoded = null!;
        foreach (var candidate in candidates) {
            var across = candidate.Axis == 0 ? width : height;
            var down = candidate.Axis == 0 ? height : width;
            var runs = new List<int>();
            var rows = new PixelRowSet(candidate.Columns, false);
            for (var y = 0; y < down; y++) {
                if (!cache.TryRead(candidate.Axis, candidate.Reverse, candidate.Invert, y, candidate.Slope, runs, token, out var first)) return false;
                var x = 0;
                for (var start = 0; start + 8 <= runs.Count; x += runs[start], start++) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    if (x > candidate.IndicatorX + candidate.Pitch) break;
                    if (((start + (first ? 0 : 1)) & 1) != 0 || Math.Abs(x - candidate.IndicatorX) > candidate.Pitch ||
                        !TryReadPixelIndicator(cache.Codewords, runs, start, false, out var cluster, out var left)) continue;
                    var row = left / 30 * 3 + cluster;
                    if (row >= candidate.Count || left != candidate.Left(row)) continue;
                    var words = new int[candidate.Columns + 2];
                    words[0] = left;
                    var at = start + 8;
                    var column = 1;
                    for (; column <= candidate.Columns; column++, at += 8) {
                        if (DecodeBudget.ShouldAbort(token)) return false;
                        if (!cache.Codewords.Read(runs, at, cluster, out words[column])) break;
                    }
                    var right = candidate.Right(row);
                    if (column <= candidate.Columns || !MatchRightIndicatorTail(runs, at, cluster, right, candidate.Pitch)) continue;
                    words[words.Length - 1] = right;
                    var middle = candidate.IndicatorX + candidate.Pitch * candidate.Columns * 17 / 2;
                    if (candidate.Reverse) middle = across - 1 - middle;
                    var physicalY = y + (middle - across / 2.0) * candidate.Slope;
                    rows.Observe(row, left, right, words, middle, physicalY, candidate.Slope,
                        candidate.Pitch * (candidate.Columns * 17 + 69), maximumCandidates: 8);
                }
            }
            if (!rows.Ordered || rows.Rows.Count < 3) continue;
            var assembly = new PixelRowAssembly(candidate.Columns, (candidate.Count - 1) / 3,
                (candidate.Count - 1) % 3, candidate.Ec, false, rows.MeanX, rows.MeanWidth);
            assembly.Observe(rows);
            if (!assembly.TryAssemble(out var combined) || combined.Count < 3 || DecodeBudget.ShouldAbort(token)) continue;
            if (TryDecodePixelAssembly(combined, candidate.Columns, candidate.Count, candidate.Ec, true, token, out decoded)) return true;
        }
        return false;
    }

    // If the stop was removed, the final indicator space can merge with the
    // light image edge. Its seven visible runs must fit the independently
    // established indicator and pitch; the last space must be long enough.
    private static bool MatchRightIndicatorTail(List<int> runs, int start, int cluster, int word, double pitch) {
        if (start + 8 != runs.Count) return false;
        var expected = PixelCodewordRuns[cluster][word];
        var error = 0.0;
        for (var i = 0; i < 7; i++) {
            var residual = runs[start + i] / pitch - expected[i];
            if (Math.Abs(residual) > 0.85) return false;
            error += residual * residual;
        }
        return error <= 1.5 && runs[start + 7] / pitch >= expected[7] - 0.85;
    }
}
