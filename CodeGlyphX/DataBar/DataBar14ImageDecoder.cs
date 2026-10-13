using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DataBar;

/// <summary>Locates DataBar-14 data rows; payload and separator definitions stay with their module owners.</summary>
internal static class DataBar14ImageDecoder {
    /// <summary>Fits a complete unstacked row using both finders and four independently scaled characters.</summary>
    internal static bool TryNormalizeUnstackedRow(List<int> runs, int start, out int[] widths, out double fit) {
        widths = Array.Empty<int>(); fit = 0;
        if (start < 0 || start + 46 > runs.Count ||
            !FitFinder(runs, start + 10, false, out var left, out var leftPitch) ||
            !FitFinder(runs, start + 31, true, out var right, out var rightPitch)) return false;
        // Edge guards follow their adjacent character's measured pitch. Only
        // the leading light guard can merge with quiet space; enclosed gaps
        // and dark guards still require one module within the existing residual.
        var firstPitch = CharacterPitch(runs, start + 2, 16);
        var lastPitch = CharacterPitch(runs, start + 36, 16);
        if (Math.Abs(runs[start + 1] / firstPitch - 1) > 0.8 ||
            Math.Abs(runs[start + 44] / lastPitch - 1) > 0.8 ||
            Math.Abs(runs[start + 45] / lastPitch - 1) > 0.8) return false;
        var normalized = new int[46];
        normalized[0] = normalized[1] = normalized[44] = normalized[45] = 1;
        Array.Copy(left, 0, normalized, 10, 5);
        Array.Copy(right, 0, normalized, 31, 5);
        if (!FitCharacter(runs, start + 2, 16, leftPitch, normalized, 2) ||
            !FitCharacter(runs, start + 15, 15, leftPitch, normalized, 15) ||
            !FitCharacter(runs, start + 23, 15, rightPitch, normalized, 23) ||
            !FitCharacter(runs, start + 36, 16, rightPitch, normalized, 36)) return false;
        widths = normalized;
        for (var i = 0; i < 46; i++) {
            if (i == 0) continue;
            var pitch = i < 23 ? leftPitch : rightPitch;
            var residual = runs[start + i] / pitch - normalized[i];
            fit += residual * residual;
        }
        return true;
    }

    private sealed class Row {
        internal readonly int[] Widths;
        internal readonly double Start, End;
        internal readonly bool Inverted;
        internal readonly int First;
        internal int Last, Threshold;
        internal Row(int[] widths, double start, double end, bool inverted, int position, int threshold) {
            Widths = widths; Start = start; End = end; Inverted = inverted;
            First = Last = position; Threshold = threshold;
        }
    }

    /// <summary>Finds one stacked symbol with bounded row observations and pairing work.</summary>
    internal static bool TryDecode(byte[] rgba, int width, int height, bool allowStacked, bool allowOmni,
        CancellationToken token, out string content, out bool omni) {
        content = string.Empty; omni = false;
        if ((!allowStacked && !allowOmni) || width < 1 || height < 1 || DecodeBudget.ShouldAbort(token)) return false;
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(width, height));
        try {
            for (var vertical = 0; vertical < 2; vertical++) {
                var length = vertical == 0 ? width : height;
                var across = vertical == 0 ? height : width;
                if (length < 50) continue;
                var step = Math.Max(1, (across + 255) / 256);
                var rows = new[] { new List<Row>(), new List<Row>(), new List<Row>(), new List<Row>() };
                for (var position = 0; position < across; position += step) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    var min = 255; var max = 0;
                    for (var i = 0; i < length; i++) {
                        if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                        var value = Luminance(rgba, width, vertical != 0, i, position);
                        buffer[i] = (byte)value;
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }
                    if (max - min < 8) continue;
                    for (var cutoff = 1; cutoff <= 3; cutoff++) {
                        var threshold = min + (max - min) * cutoff / 4;
                        Collect(buffer.AsSpan(0, length), threshold, position, step, rows, token);
                    }
                }
                if (Assemble(rgba, width, length, vertical != 0, rows, allowStacked, allowOmni, token, out content, out omni)) return true;
            }
            return false;
        } finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private static void Collect(ReadOnlySpan<byte> line, int threshold, int position, int step, List<Row>[] rows, CancellationToken token) {
        var runs = new List<int> { 0 };
        var budget = DecodeBudget.Capture();
        var first = line[0] < threshold;
        var current = first; var count = 1;
        for (var i = 1; i < line.Length; i++) {
            if ((i & 255) == 0 && budget.ShouldAbort(token)) return;
            var dark = line[i] < threshold;
            if (dark == current) count++;
            else { runs.Add(count); current = dark; count = 1; }
        }
        runs.Add(count); runs.Add(0);
        if (runs.Count < 25) return;
        // The extra empty edge runs retain guards that merge with the quiet zone.
        var firstRunDark = !first;
        for (var reverse = 0; reverse < 2; reverse++) {
            if (reverse != 0) {
                firstRunDark = firstRunDark == ((runs.Count & 1) != 0);
                runs.Reverse();
            }
            var offsets = new int[runs.Count + 1];
            for (var i = 0; i < runs.Count; i++) offsets[i + 1] = offsets[i] + runs[i];
            for (var start = 0; start + 25 <= runs.Count; start++) {
                if ((start & 31) == 0 && budget.ShouldAbort(token)) return;
                for (var bottom = 0; bottom < 2; bottom++) {
                    if (!FitFinder(runs, start + 10, bottom != 0, out var finder, out var pitch)) continue;
                    var firstPitch = CharacterPitch(runs, start + 2, bottom == 0 ? 16 : 15);
                    var lastPitch = CharacterPitch(runs, start + 15, bottom == 0 ? 15 : 16);
                    var firstDark = bottom == 0 ? 1 : 0;
                    var lastDark = bottom == 0 ? 23 : 24;
                    if (Math.Abs(runs[start + firstDark] / firstPitch - 1) > 0.8 ||
                        Math.Abs(runs[start + lastDark] / lastPitch - 1) > 0.8) continue;
                    // A bottom row starts and ends dark. Its two light guard
                    // gaps are enclosed and cannot absorb outer quiet space.
                    if (bottom != 0 && (Math.Abs(runs[start + 1] / firstPitch - 1) > 0.8 ||
                        Math.Abs(runs[start + 23] / lastPitch - 1) > 0.8)) continue;
                    // Reject malformed guards before allocating and fitting the
                    // character widths; guard pitch still uses the same raw runs.
                    var widths = new int[25];
                    widths[0] = widths[1] = widths[23] = widths[24] = 1;
                    Array.Copy(finder, 0, widths, 10, 5);
                    if (!FitCharacter(runs, start + 2, bottom == 0 ? 16 : 15, pitch, widths, 2) ||
                        !FitCharacter(runs, start + 15, bottom == 0 ? 15 : 16, pitch, widths, 15)) continue;
                    var rowStart = offsets[start + 2] - pitch * 2;
                    var rowEnd = offsets[start + 23] + pitch * 2;
                    if (Math.Abs((rowEnd - rowStart) / pitch - 50) > 3) continue;
                    var inverted = (firstRunDark == ((start & 1) == 0)) != (bottom != 0);
                    var list = rows[reverse * 2 + bottom];
                    var matched = false;
                    foreach (var prior in list) {
                        if (prior.Inverted != inverted || position > prior.Last + step * 2 ||
                            Math.Abs(prior.Start - rowStart) > pitch * 1.5 || Math.Abs(prior.End - rowEnd) > pitch * 1.5 ||
                            !EqualWidths(prior.Widths, widths)) continue;
                        prior.Last = position; prior.Threshold = threshold; matched = true; break;
                    }
                    if (!matched && list.Count < 64) list.Add(new Row(widths, rowStart, rowEnd, inverted, position, threshold));
                }
            }
        }
    }

    private static bool FitFinder(List<int> runs, int start, bool reverse, out int[] finder, out double pitch) {
        finder = Array.Empty<int>();
        var sum = 0;
        for (var i = 0; i < 5; i++) sum += runs[start + i];
        pitch = sum / 15.0;
        if (pitch < 0.8) return false;
        var best = -1; var error = double.PositiveInfinity;
        for (var pattern = 0; pattern < 9; pattern++) {
            var fit = 0.0;
            for (var i = 0; i < 5; i++) {
                var residual = runs[start + i] / pitch - DataBar14Tables.FINDER_PATTERN[pattern * 5 + (reverse ? 4 - i : i)];
                if (Math.Abs(residual) > 0.8) { fit = double.PositiveInfinity; break; }
                fit += residual * residual;
            }
            if (fit < error) { best = pattern; error = fit; }
        }
        if (best < 0 || error > 1.2) return false;
        finder = new int[5];
        for (var i = 0; i < 5; i++) finder[i] = DataBar14Tables.FINDER_PATTERN[best * 5 + (reverse ? 4 - i : i)];
        return true;
    }

    private static double CharacterPitch(List<int> runs, int start, int modules) {
        var total = 0;
        for (var i = 0; i < 8; i++) total += runs[start + i];
        return total / (double)modules;
    }

    private static bool FitCharacter(List<int> runs, int start, int modules, double finderPitch, int[] widths, int offset) {
        var pitch = CharacterPitch(runs, start, modules);
        if (pitch < finderPitch * 0.7 || pitch > finderPitch * 1.3) return false;
        var sum = 0;
        for (var i = 0; i < 8; i++) { widths[offset + i] = Math.Max(1, (int)Math.Round(runs[start + i] / pitch)); sum += widths[offset + i]; }
        while (sum != modules) {
            var adjust = sum < modules ? 1 : -1;
            var best = -1; var error = double.PositiveInfinity;
            for (var i = 0; i < 8; i++) {
                var value = widths[offset + i];
                if (value + adjust < 1 || value + adjust > 8) continue;
                var residual = value - runs[start + i] / pitch;
                var change = (residual + adjust) * (residual + adjust) - residual * residual;
                if (change < error) { error = change; best = i; }
            }
            if (best < 0) return false;
            widths[offset + best] += adjust; sum += adjust;
        }
        for (var i = 0; i < 8; i++) if (Math.Abs(widths[offset + i] - runs[start + i] / pitch) > 0.8) return false;
        return true;
    }

    private static bool EqualWidths(int[] left, int[] right) {
        for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    private static bool Assemble(byte[] rgba, int width, int length, bool vertical, List<Row>[] rows,
        bool allowStacked, bool allowOmni, CancellationToken token, out string content, out bool omni) {
        content = string.Empty; omni = false;
        var remaining = 4096;
        for (var reverse = 0; reverse < 2; reverse++) {
            foreach (var top in rows[reverse * 2]) foreach (var bottom in rows[reverse * 2 + 1]) {
                if (--remaining < 0 || DecodeBudget.ShouldAbort(token)) return false;
                var pitch = (top.End - top.Start + bottom.End - bottom.Start) / 100;
                if (top.Inverted != bottom.Inverted || Math.Abs(top.Start - bottom.Start) > pitch * 2 ||
                    Math.Abs(top.End - bottom.End) > pitch * 2 ||
                    (top.First <= bottom.Last && bottom.First <= top.Last) ||
                    Math.Abs((top.First + top.Last - bottom.First - bottom.Last) / 2.0) > pitch * 90) continue;
                if (!DataBar14Decoder.TryDecodeStackedRows(top.Widths, bottom.Widths, out var decoded)) continue;
                var stackedError = allowStacked ? SeparatorError(DataBar14Encoder.EncodeStacked(decoded), top, bottom) : double.PositiveInfinity;
                var omniError = allowOmni ? SeparatorError(DataBar14Encoder.EncodeStackedOmnidirectional(decoded), top, bottom) : double.PositiveInfinity;
                if (Math.Min(stackedError, omniError) > 0.16) continue;
                content = decoded; omni = omniError < stackedError; return true;

                double SeparatorError(BitMatrix expected, Row first, Row second) {
                    var firstCenter = (first.First + first.Last) / 2;
                    var secondCenter = (second.First + second.Last) / 2;
                    var direction = Math.Sign(secondCenter - firstCenter);
                    if (direction == 0) return double.PositiveInfinity;
                    var firstEdge = FindEdge(first, 0, firstCenter, secondCenter, direction);
                    var secondEdge = FindEdge(second, expected.Height - 1, secondCenter, firstCenter, -direction);
                    if (DecodeBudget.ShouldAbort(token)) return double.PositiveInfinity;
                    var gap = Math.Abs(secondEdge - firstEdge) - 1;
                    var separatorRows = expected.Height - 2;
                    if (gap < 1 || gap / pitch < separatorRows * 0.45 || gap / pitch > separatorRows * 2.0 + 1) return double.PositiveInfinity;
                    var error = SampleSeparator(0, requireInsideGap: false);
                    if (error <= 0.16) return error;
                    // A photographed separator may cross several pixel rows.
                    // Keep one coherent slope for every separator band, bounded
                    // to two modules across the sampled width. Fallback samples
                    // stay strictly between the verified data-row edges.
                    for (var skew = -2; skew <= 2; skew++) {
                        if (skew == 0) continue;
                        if (DecodeBudget.ShouldAbort(token)) return double.PositiveInfinity;
                        error = Math.Min(error, SampleSeparator(skew * pitch, requireInsideGap: true));
                    }
                    return error;

                    double SampleSeparator(double skew, bool requireInsideGap) {
                        var mismatches = 0;
                        for (var row = 1; row <= separatorRows; row++) {
                            var errors = 0;
                            for (var x = 4; x < 46; x++) {
                                var offset = (gap + 1) * (row - 0.5) / separatorRows + skew * (x - 24.5) / 42;
                                var across = firstEdge + direction * (int)Math.Round(offset);
                                if (requireInsideGap && (across <= Math.Min(firstEdge, secondEdge) || across >= Math.Max(firstEdge, secondEdge)))
                                    return double.PositiveInfinity;
                                if (Read(first, x, across) != expected[x, row]) errors++;
                            }
                            if (errors > 9) return double.PositiveInfinity;
                            mismatches += errors;
                        }
                        return mismatches / (42.0 * separatorRows);
                    }

                    int FindEdge(Row observation, int row, int from, int until, int increment) {
                        var edge = from;
                        for (var at = from + increment; at != until; at += increment) {
                            if (DecodeBudget.ShouldAbort(token)) break;
                            var errors = 0;
                            for (var x = 4; x < 46; x++) if (Read(observation, x, at) != expected[x, row]) errors++;
                            if (errors > 6) break;
                            edge = at;
                        }
                        return edge;
                    }
                    bool Read(Row observation, int module, int across) {
                        var along = (int)Math.Floor(observation.Start + (module + 0.5) * (observation.End - observation.Start) / 50);
                        along = Math.Max(0, Math.Min(length - 1, along));
                        if (reverse != 0) along = length - 1 - along;
                        var threshold = (first.Threshold + second.Threshold) / 2;
                        return (Luminance(rgba, width, vertical, along, across) < threshold) != first.Inverted;
                    }
                }
            }
        }
        return false;
    }

    private static int Luminance(byte[] rgba, int width, bool vertical, int along, int across) {
        var at = (vertical ? along * width + across : across * width + along) * 4;
        return BarcodeScanline.GetLuminance(rgba[at], rgba[at + 1], rgba[at + 2], rgba[at + 3]);
    }
}
