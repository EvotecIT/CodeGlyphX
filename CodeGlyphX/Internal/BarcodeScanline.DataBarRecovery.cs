using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private sealed class DataBarRowObservation {
        internal readonly BarcodeScanlineCandidate Candidate;
        internal readonly string Content;
        internal readonly double Start, End, Pitch;
        internal readonly bool Reverse, Inverted;
        internal DataBarRowObservation(BarcodeScanlineCandidate candidate, string content,
            double start, double end, double pitch, bool reverse, bool inverted) {
            Candidate = candidate; Content = content; Start = start; End = end;
            Pitch = pitch; Reverse = reverse; Inverted = inverted;
        }
    }

    /// <summary>
    /// Searches bounded anchors and their guarded neighbors for complete DataBar
    /// rows. A recovered symbol needs the same payload and aligned bounds in
    /// three adjacent physical rows; conflicting payloads at those bounds reject.
    /// </summary>
    private static void CollectDataBarRecoveryRows(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        CancellationToken token, List<BarcodeScanlineCandidate> candidates) {
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(width, height));
        int[]? offsets = null;
        try {
            var runs = new List<int>();
            for (var axis = 0; axis < 2; axis++) {
                var vertical = axis != 0;
                var length = vertical ? height : width;
                var across = vertical ? width : height;
                if (length < 96 || across < 3) continue;
                var rows = new Dictionary<int, List<DataBarRowObservation>>();
                var sampled = new Dictionary<int, bool>();
                foreach (var fraction in RetailRecoveryFractions) {
                    if (DecodeBudget.ShouldAbort(token)) return;
                    var position = (int)((long)across * fraction / 16);
                    if (!Sample(pixels, position)) continue;
                    // Four neighbors retain a three-row confirmation window
                    // when a rotation reflects an integer anchor by one pixel.
                    for (var distance = 1; distance <= 4; distance++) {
                        if (position >= distance) Sample(pixels, position - distance);
                        if (position + distance < across) Sample(pixels, position + distance);
                    }
                }
                CollectCorroboratedDataBarRows(rows, token, candidates);

                bool Sample(PixelSpan input, int position) {
                    if (sampled.TryGetValue(position, out var existing)) return existing;
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    var min = 255; var max = 0;
                    for (var at = 0; at < length; at++) {
                        if ((at & 127) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                        var pixel = vertical ? at * stride + position * 4 : position * stride + at * 4;
                        var red = input[pixel + (format == PixelFormat.Rgba32 ? 0 : 2)];
                        var blue = input[pixel + (format == PixelFormat.Rgba32 ? 2 : 0)];
                        var value = GetLuminance(red, input[pixel + 1], blue, input[pixel + 3]);
                        buffer[at] = value; min = Math.Min(min, value); max = Math.Max(max, value);
                    }
                    var observations = new List<DataBarRowObservation>();
                    rows[position] = observations;
                    var guarded = false;
                    Span<int> thresholds = stackalloc int[15];
                    var thresholdCount = 0;
                    if (max - min >= 8) {
                        for (var cutoff = 1; cutoff < 16; cutoff++) thresholds[thresholdCount++] = min + (max - min) * cutoff / 16;
                        thresholdCount = ScanlineThresholds.RetainDistinct(buffer.AsSpan(0, length), token, thresholds, thresholdCount);
                    }
                    for (var cutoff = 0; cutoff < thresholdCount; cutoff++) {
                        if (DecodeBudget.ShouldAbort(token)) return false;
                        var threshold = thresholds[cutoff];
                        var firstDark = buffer[0] < threshold;
                        var current = firstDark; var count = 1;
                        // Sampling is sequential; returned observations own their
                        // modules and never retain these temporary work buffers.
                        runs.Clear();
                        for (var at = 1; at < length; at++) {
                            if ((at & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                            var dark = buffer[at] < threshold;
                            if (dark == current) count++;
                            else { runs.Add(count); current = dark; count = 1; }
                        }
                        runs.Add(count);
                        if (runs.Count < 46) continue;
                        if (offsets is null || offsets.Length < runs.Count + 1) {
                            var replacement = ArrayPool<int>.Shared.Rent(runs.Count + 1);
                            if (offsets is not null) ArrayPool<int>.Shared.Return(offsets);
                            offsets = replacement;
                        }
                        for (var reverse = 0; reverse < 2; reverse++) {
                            if (reverse != 0) { runs.Reverse(); firstDark = firstDark == ((runs.Count & 1) != 0); }
                            offsets[0] = 0;
                            for (var i = 0; i < runs.Count; i++) offsets[i + 1] = offsets[i] + runs[i];
                            for (var start = 0; start + 46 <= runs.Count; start++) {
                                if (DecodeBudget.ShouldAbort(token)) return false;
                                var fitted = DataBar14ImageDecoder.TryNormalizeUnstackedRow(runs, start,
                                    out var widths, out var fit, out var hasFinders, repairCharacter: true);
                                guarded |= hasFinders;
                                if (!fitted || observations.Count >= 32) continue;
                                var modules = new bool[96]; var module = 0; var dark = false;
                                foreach (var run in widths) { for (var i = 0; i < run; i++) modules[module++] = dark; dark = !dark; }
                                if (!DataBar14Decoder.TryDecodeOmnidirectional(modules, out var content)) continue;
                                var pitch = 0.0; for (var i = 0; i < 8; i++) pitch += runs[start + 2 + i]; pitch /= 16;
                                var first = offsets[start + 2] - pitch * 2;
                                double last = offsets[start + 46];
                                if (reverse != 0) { var oldFirst = first; first = length - last; last = length - oldFirst; }
                                var inverted = firstDark == ((start & 1) == 0);
                                if (inverted) for (var i = 0; i < modules.Length; i++) modules[i] = !modules[i];
                                var candidate = new BarcodeScanlineCandidate(modules, position, vertical, fitError: fit);
                                var next = new DataBarRowObservation(candidate, content, first, last, pitch, reverse != 0, inverted);
                                var duplicate = false;
                                foreach (var prior in observations) {
                                    if (prior.Content == content && prior.Reverse == next.Reverse && prior.Inverted == inverted && Aligned(prior, next)) { duplicate = true; break; }
                                }
                                if (!duplicate) observations.Add(next);
                            }
                        }
                    }
                    sampled[position] = guarded;
                    return guarded;
                }
            }
        } finally {
            if (offsets is not null) ArrayPool<int>.Shared.Return(offsets);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Keep selection shared by every pixel entrypoint and both scan axes.
    private static void CollectCorroboratedDataBarRows(Dictionary<int, List<DataBarRowObservation>> rows,
        CancellationToken token, List<BarcodeScanlineCandidate> candidates) {
        foreach (var row in rows) foreach (var observation in row.Value) {
            if (DecodeBudget.ShouldAbort(token)) return;
            if (!HasSupport(row.Key, observation)) continue;
            var conflict = false;
            // Three-row windows overlap when their centers are at most two
            // physical rows apart, even if only one center contains both fits.
            for (var offset = -2; offset <= 2 && !conflict; offset++) {
                var center = (long)row.Key + offset;
                if (center < 0 || center > int.MaxValue || !rows.TryGetValue((int)center, out var alternatives)) continue;
                foreach (var other in alternatives) {
                    if (other.Content != observation.Content && Aligned(observation, other) && HasSupport((int)center, other)) {
                        conflict = true; break;
                    }
                }
            }
            if (!conflict) AddUniqueLocatedCandidate(candidates, observation.Candidate);
        }
        bool HasSupport(int position, DataBarRowObservation observation) =>
            HasMatchingRow((long)position - 1, observation) && HasMatchingRow((long)position + 1, observation);

        bool HasMatchingRow(long position, DataBarRowObservation observation) {
            if (position < 0 || position > int.MaxValue || !rows.TryGetValue((int)position, out var adjacent)) return false;
            foreach (var other in adjacent)
                if (other.Content == observation.Content && other.Reverse == observation.Reverse &&
                    other.Inverted == observation.Inverted && Aligned(observation, other)) return true;
            return false;
        }
    }

    private static bool Aligned(DataBarRowObservation left, DataBarRowObservation right) =>
        Math.Abs(left.Start - right.Start) <= Math.Max(left.Pitch, right.Pitch) * 1.5 &&
        Math.Abs(left.End - right.End) <= Math.Max(left.Pitch, right.Pitch) * 1.5;
}
