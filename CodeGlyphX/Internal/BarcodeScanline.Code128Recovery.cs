using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private sealed class Code128RowObservation {
        internal readonly BarcodeScanlineCandidate Candidate;
        internal readonly string Key;
        internal Code128RowObservation(BarcodeScanlineCandidate candidate, string key) {
            Candidate = candidate;
            Key = key;
        }
    }

    /// <summary>
    /// Searches bounded physical anchors for complete Code 128 frames. A retry
    /// must pass the module owner's checksum and control rules on three adjacent
    /// physical rows or columns; one isolated threshold fit is insufficient.
    /// </summary>
    private static void CollectCode128RecoveryRows(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        CancellationToken token, List<BarcodeScanlineCandidate> candidates) {
        // This synchronous search never changes the ambient recognition scope.
        var budget = DecodeBudget.Capture();
        if (budget.ShouldAbort(token)) return;
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(width, height));
        try {
            var runs = new List<int>();
            for (var axis = 0; axis < 2; axis++) {
                var vertical = axis != 0;
                var length = vertical ? height : width;
                var across = vertical ? width : height;
                if (length < 46 || across < 3) continue;
                var sampled = new Dictionary<int, List<Code128RowObservation>>();
                var guarded = new HashSet<int>();
                var accepted = new SortedDictionary<int, List<BarcodeScanlineCandidate>>();
                for (var anchor = 0; anchor < (RetailRecoveryFractions.Length + 1) * 2; anchor++) {
                    if (budget.ShouldAbort(token)) return;
                    // The ordinary center row may need one of the recovery
                    // cutoffs. A reflected integer anchor can differ by one pixel.
                    var fraction = anchor < 2 ? 8 : RetailRecoveryFractions[(anchor - 2) / 2];
                    var position = (int)((long)across * fraction / 16) - (anchor & 1);
                    if (position < 0) continue;
                    Sample(pixels, position);
                    if (!guarded.Contains(position)) continue;
                    // A guarded anchor can fall at the edge of the readable
                    // band, or fail its checksum while nearby rows remain clear.
                    // Require three consecutive verified rows within four pixels.
                    var last = Math.Min(across - 1, position + 4);
                    for (var first = Math.Max(0, position - 4); first + 2 <= last; first++) {
                        if (budget.ShouldAbort(token)) return;
                        var a = Sample(pixels, first);
                        var b = Sample(pixels, first + 1);
                        var c = Sample(pixels, first + 2);
                        foreach (var observation in a) {
                            if (!Contains(b, observation.Key) || !Contains(c, observation.Key)) continue;
                            if (!accepted.TryGetValue(first, out var row)) {
                                row = new List<BarcodeScanlineCandidate>();
                                accepted.Add(first, row);
                            }
                            AddUniqueLocatedCandidate(row, observation.Candidate);
                        }
                    }
                }
                // Scanline selection consumes every hypothesis for one physical
                // position together; overlapping refinement windows stay grouped.
                foreach (var row in accepted.Values)
                    foreach (var candidate in row) AddUniqueLocatedCandidate(candidates, candidate);

                List<Code128RowObservation> Sample(PixelSpan input, int position) {
                    if (sampled.TryGetValue(position, out var previous)) return previous;
                    var result = new List<Code128RowObservation>();
                    sampled[position] = result;
                    if (budget.ShouldAbort(token)) return result;
                    var minimum = 255;
                    var maximum = 0;
                    for (var at = 0; at < length; at++) {
                        if ((at & 127) == 0 && budget.ShouldAbort(token)) return result;
                        var p = vertical ? at * stride + position * 4 : position * stride + at * 4;
                        var red = input[p + (format == PixelFormat.Rgba32 ? 0 : 2)];
                        var blue = input[p + (format == PixelFormat.Rgba32 ? 2 : 0)];
                        var value = GetLuminance(red, input[p + 1], blue, input[p + 3]);
                        buffer[at] = value;
                        minimum = Math.Min(minimum, value);
                        maximum = Math.Max(maximum, value);
                    }
                    if (maximum - minimum < 8) return result;
                    Span<int> thresholds = stackalloc int[8];
                    var count = GetDistinctScanlineThresholds(buffer.AsSpan(0, length), minimum, maximum, token, thresholds, includeIntermediate: true);
                    var fits = new List<BarcodeScanlineCandidate>();
                    for (var cutoff = 0; cutoff < count; cutoff++) {
                        if (budget.ShouldAbort(token)) return result;
                        var threshold = thresholds[cutoff];
                        runs.Clear();
                        var firstIsBar = buffer[0] < threshold;
                        var dark = firstIsBar;
                        var run = 1;
                        for (var at = 1; at < length; at++) {
                            if ((at & 127) == 0 && budget.ShouldAbort(token)) return result;
                            var next = buffer[at] < threshold;
                            if (next == dark) run++;
                            else { runs.Add(run); dark = next; run = 1; }
                        }
                        runs.Add(run);
                        if (TryCollectCode128RunCandidates(runs, firstIsBar, token, fits, position, vertical, inspectFraming: true))
                            guarded.Add(position);
                    }
                    foreach (var fit in fits) {
                        if (budget.ShouldAbort(token)) return result;
                        if (BarcodeDecoder.TryDecode(fit.Modules, BarcodeType.Code128, null, token, out var decoded))
                            result.Add(new Code128RowObservation(fit, decoded.DeduplicationKey));
                    }
                    return result;
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static bool Contains(List<Code128RowObservation> observations, string key) {
        foreach (var observation in observations) if (string.Equals(observation.Key, key, StringComparison.Ordinal)) return true;
        return false;
    }
}
