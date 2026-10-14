using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Ean;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>Threshold-local evidence survives cross-threshold candidate deduplication.</summary>
    private readonly struct RetailRowEvidence {
        internal bool HasGuards { get; }
        internal bool HasNormalizedBase { get; }
        internal RetailRowEvidence(bool hasGuards, bool hasNormalizedBase) {
            HasGuards = hasGuards;
            HasNormalizedBase = hasNormalizedBase;
        }
    }

    private static readonly int[][] RetailDigitRuns = BuildRetailDigitRuns();

    // Photographs contain text, borders, add-ons and varying bar pitch. Locate guarded
    // retail symbols inside a scanline and estimate pitch independently for each digit.
    // The existing decoder remains responsible for parity and checksum validation.
    private static RetailRowEvidence TryCollectRetailRunCandidates(ReadOnlySpan<byte> luminance, int threshold,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical, bool localThreshold = false,
        bool retailOnly = false, bool includeParityRecovery = false, bool fullLengthRetailOnly = false) {
        if (luminance.Length < 51) return default;
        // This synchronous row search does not change the ambient budget. Read
        // its deadline once while retaining every clock and cancellation check.
        var budget = DecodeBudget.Capture();
        var hasRetailGuards = false;
        var hasNormalizedBase = false;
        var rentedRuns = ArrayPool<int>.Shared.Rent(luminance.Length);
        try {
            var runCount = 0;
            var firstIsBar = luminance[0] < threshold;
            var current = firstIsBar;
            var length = 1;
            for (var i = 1; i < luminance.Length; i++) {
                if ((i & 255) == 0 && budget.ShouldAbort(cancellationToken)) return default;
                var bar = luminance[i] < threshold;
                if (bar == current) length++;
                else { rentedRuns[runCount++] = length; current = bar; length = 1; }
            }
            rentedRuns[runCount++] = length;
            var runs = rentedRuns.AsSpan(0, runCount);
            if (!localThreshold && !retailOnly) {
                // Existing non-retail localizers consume an owned list. Only that
                // ordinary path needs a copy; retail and adaptive hypotheses use
                // the bounded pooled runs directly, including rejected fits.
                var otherRuns = new List<int>(runCount);
                for (var i = 0; i < runCount; i++) {
                    if ((i & 255) == 0 && budget.ShouldAbort(cancellationToken)) return default;
                    otherRuns.Add(runs[i]);
                }
                TryCollectCode128RunCandidates(otherRuns, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectCode39RunCandidates(otherRuns, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectCode93RunCandidates(otherRuns, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectCodabarRunCandidates(runs, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectItfRunCandidates(runs, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectExpandedRunCandidates(otherRuns, firstIsBar, cancellationToken, candidates, position, isVertical);
                TryCollectDataBar14RunCandidates(otherRuns, firstIsBar, cancellationToken, candidates, position, isVertical);
            }
            for (var start = 0; start + 33 <= runs.Length; start++) {
                // A candidate has at most twelve digits and a fixed number of
                // parity hypotheses. Poll the clock every 32 candidates, as the
                // pixel loops already do in bounded blocks. Keep cancellation
                // at every candidate and check the clock again before returning.
                if (cancellationToken.IsCancellationRequested || ((start & 31) == 0 && budget.IsExpired)) return default;
                // Also locate inverted symbols; the normal transform path resolves polarity.
                var pitch = (runs[start] + runs[start + 1] + runs[start + 2]) / 3.0;
                if (!RetailGuardMatches(runs, start, 3, pitch) || (start > 0 && runs[start - 1] < pitch * 3)) continue;
                Collect(runs, 6, 6, false);
                // Additional fine cutoffs need the full-length symbol's center
                // guard and twelve encoded digits. Shorter shapes can fit
                // perforated textures even when a neighboring row agrees.
                if (!fullLengthRetailOnly) {
                    Collect(runs, 4, 4, false);
                    Collect(runs, 6, 0, true);
                }
                if (!includeParityRecovery) continue;

                // Constrain ambiguous digits only as a fallback. UPC-A and EAN-8 use
                // odd-family run widths on both halves; reversed rows use even widths.
                // EAN-13 permits mixed left parity, with a fixed right-hand family.
                // The complete guards, residual bounds and decoder checks still apply.
                Collect(runs, 6, 6, false, 0, 0);
                Collect(runs, 6, 6, false, 1, 1);
                Collect(runs, 6, 6, false, -1, 0);
                Collect(runs, 6, 6, false, 1, -1);
                if (!fullLengthRetailOnly) {
                    Collect(runs, 4, 4, false, 0, 0);
                    Collect(runs, 4, 4, false, 1, 1);
                }

                void Collect(ReadOnlySpan<int> sampledRuns, int leftDigits, int rightDigits, bool upce, int leftParity = -1, int rightParity = -1) {
                    var count = 3 + leftDigits * 4 + (upce ? 6 : 5 + rightDigits * 4 + 3);
                    var end = start + count;
                    if (end > sampledRuns.Length) return;
                    var lastGuard = upce ? 6 : 3;
                    var endPitch = 0.0;
                    for (var i = end - lastGuard; i < end; i++) endPitch += sampledRuns[i];
                    endPitch /= lastGuard;
                    if (!RetailGuardMatches(sampledRuns, end - lastGuard, lastGuard, endPitch)) return;
                    var hasQuietZone = end == sampledRuns.Length || sampledRuns[end] >= endPitch * 3;
                    // The encoder separates an add-on by one module, before the base's
                    // standalone quiet zone. Keep that evidence for full-row validation,
                    // while local base-only candidates retain their quiet-zone check.
                    if (!hasQuietZone && sampledRuns[end] < endPitch) return;
                    if (!upce) {
                        var centerAt = start + 3 + leftDigits * 4;
                        var centerPitch = 0.0;
                        for (var i = centerAt; i < centerAt + 5; i++) centerPitch += sampledRuns[i];
                        if (!RetailGuardMatches(sampledRuns, centerAt, 5, centerPitch / 5)) return;
                    }
                    // Guard geometry can survive a damaged digit on this exact row.
                    // Recovery may inspect its immediate neighbors before giving up.
                    hasRetailGuards = true;
                    // Rejected hypotheses must not allocate a list and its growing
                    // buffers for every digit. The largest retail shape has 59 runs.
                    Span<int> widths = stackalloc int[59];
                    var widthCount = 3;
                    widths.Slice(0, 3).Fill(1);
                    var at = start + 3;
                    var fit = 0.0;
                    for (var digit = 0; digit < leftDigits; digit++, at += 4) {
                        if (!TryNormalizeRetailDigit(sampledRuns, at, leftParity, out var pattern, out var error)) return;
                        for (var i = 0; i < pattern.Length; i++) widths[widthCount++] = pattern[i];
                        fit += error;
                    }
                    if (!upce) {
                        widths.Slice(widthCount, 5).Fill(1); widthCount += 5; at += 5;
                        for (var digit = 0; digit < rightDigits; digit++, at += 4) {
                            if (!TryNormalizeRetailDigit(sampledRuns, at, rightParity, out var pattern, out var error)) return;
                            for (var i = 0; i < pattern.Length; i++) widths[widthCount++] = pattern[i];
                            fit += error;
                        }
                    }
                    widths.Slice(widthCount, lastGuard).Fill(1); widthCount += lastGuard;
                    hasNormalizedBase = true;
                    if (!hasQuietZone) return;
                    var moduleCount = upce ? 51 : leftDigits == 6 ? 95 : 67;
                    var modules = new bool[moduleCount];
                    var offset = 0;
                    var bar = firstIsBar == ((start & 1) == 0);
                    foreach (var width in widths.Slice(0, widthCount)) {
                        for (var i = 0; i < width; i++) modules[offset++] = bar;
                        bar = !bar;
                    }
                    AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(modules, position, isVertical, fitError: fit,
                        isLocalThreshold: localThreshold, isRetailParityFallback: leftParity >= 0 || rightParity >= 0));
                }
            }
            return budget.ShouldAbort(cancellationToken) ? default : new RetailRowEvidence(hasRetailGuards, hasNormalizedBase);
        } finally {
            ArrayPool<int>.Shared.Return(rentedRuns);
        }
    }

    private static bool RetailGuardMatches(ReadOnlySpan<int> runs, int start, int count, double pitch) {
        for (var i = 0; i < count; i++) if (Math.Abs(runs[start + i] - pitch) > pitch * 0.5 + 0.5) return false;
        return true;
    }

    private static bool TryNormalizeRetailDigit(ReadOnlySpan<int> runs, int start, int parity, out int[] pattern, out double fit) {
        pattern = Array.Empty<int>(); fit = double.PositiveInfinity;
        var total = runs[start] + runs[start + 1] + runs[start + 2] + runs[start + 3];
        var pitch = total / 7.0;
        // Reversing a row changes floating-point addition order. Keep the same
        // digit for equal constrained fits instead of letting rounding pick a tie.
        var tieTolerance = parity < 0 ? 0 : pitch * pitch * 1e-12;
        // Keep the four sampled widths and residual limit outside the pattern
        // loop. Early rejection and the order of squared-error additions stay
        // the same, including constrained-family ties.
        var first = runs[start];
        var second = runs[start + 1];
        var third = runs[start + 2];
        var fourth = runs[start + 3];
        var limit = pitch * 0.7 + 0.5;
        // BuildRetailDigitRuns interleaves odd and even patterns for every digit.
        // A fixed family narrows the search without increasing any fit tolerance.
        for (var index = Math.Max(0, parity); index < RetailDigitRuns.Length; index += parity < 0 ? 1 : 2) {
            var candidate = RetailDigitRuns[index];
            var a = Math.Abs(first - candidate[0] * pitch);
            if (a > limit) continue;
            var b = Math.Abs(second - candidate[1] * pitch);
            if (b > limit) continue;
            var c = Math.Abs(third - candidate[2] * pitch);
            if (c > limit) continue;
            var d = Math.Abs(fourth - candidate[3] * pitch);
            if (d > limit) continue;
            var error = a * a;
            error += b * b;
            error += c * c;
            error += d * d;
            if (error + tieTolerance < fit) { pattern = candidate; fit = error; }
        }
        return pattern.Length != 0 && fit / (pitch * pitch) < 1.0;
    }

    private static int[][] BuildRetailDigitRuns() {
        var patterns = new List<int[]>();
        foreach (var digit in EanTables.EncodingTable.Values) {
            Add(digit.LeftOdd); Add(digit.LeftEven);
        }
        return patterns.ToArray();
        void Add(bool[] modules) {
            var runs = new List<int>(); var current = modules[0]; var length = 1;
            for (var i = 1; i < modules.Length; i++) {
                if (modules[i] == current) length++;
                else { runs.Add(length); current = modules[i]; length = 1; }
            }
            runs.Add(length); patterns.Add(runs.ToArray());
        }
    }
}
