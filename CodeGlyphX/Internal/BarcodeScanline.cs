using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;
using System.Buffers;
using System.Threading;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Internal;

/// <summary>
/// A physical scanline's sampled modules and optional alternative pitch, with
/// squared pixel-run residuals for choosing between validated interpretations.
/// </summary>
internal readonly struct BarcodeScanlineCandidate {
    internal bool[] Modules { get; }
    internal bool[] AlternativeModules { get; }
    internal double FitError { get; }
    internal double AlternativeFitError { get; }
    internal int Position { get; }
    internal bool IsVertical { get; }

    internal BarcodeScanlineCandidate(bool[] modules, int position, bool isVertical, bool[]? alternativeModules = null, double fitError = 0, double alternativeFitError = 0) {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        AlternativeModules = alternativeModules ?? Array.Empty<bool>();
        FitError = fitError;
        AlternativeFitError = alternativeFitError;
        Position = position;
        IsVertical = isVertical;
    }
}

internal static class BarcodeScanline {
    public static bool TryGetModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out bool[] modules) {
        return TryGetModules(pixels, width, height, stride, format, CancellationToken.None, out modules);
    }

    public static bool TryGetModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out bool[] modules) {
        modules = Array.Empty<bool>();
#if !NET8_0_OR_GREATER
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
#else
        if (pixels.IsEmpty) return false;
#endif
        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        var bestLen = 0;
        var best = Array.Empty<bool>();

        var y0 = height / 2;
        var y1 = height / 3;
        var y2 = (height * 2) / 3;

        TryPickBest(TryGetModulesFromHorizontal(pixels, width, height, stride, format, y0, cancellationToken, out var m0), m0, ref bestLen, ref best);
        TryPickBest(TryGetModulesFromHorizontal(pixels, width, height, stride, format, y1, cancellationToken, out var m1), m1, ref bestLen, ref best);
        TryPickBest(TryGetModulesFromHorizontal(pixels, width, height, stride, format, y2, cancellationToken, out var m2), m2, ref bestLen, ref best);

        if (bestLen > 0) {
            modules = best;
            return true;
        }

        var x0 = width / 2;
        var x1 = width / 3;
        var x2 = (width * 2) / 3;

        TryPickBest(TryGetModulesFromVertical(pixels, width, height, stride, format, x0, cancellationToken, out var v0), v0, ref bestLen, ref best);
        TryPickBest(TryGetModulesFromVertical(pixels, width, height, stride, format, x1, cancellationToken, out var v1), v1, ref bestLen, ref best);
        TryPickBest(TryGetModulesFromVertical(pixels, width, height, stride, format, x2, cancellationToken, out var v2), v2, ref bestLen, ref best);

        if (bestLen == 0) return false;
        modules = best;
        return true;
    }

    public static bool TryGetModuleCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out BarcodeScanlineCandidate[] candidates) {
        return TryGetModuleCandidates(pixels, width, height, stride, format, CancellationToken.None, out candidates);
    }

    public static bool TryGetModuleCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out BarcodeScanlineCandidate[] candidates) {
        return TryGetLocatedModuleCandidates(pixels, width, height, stride, format, cancellationToken, out candidates);
    }

    internal static bool TryGetLocatedModuleCandidates(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out BarcodeScanlineCandidate[] candidates) {
        candidates = Array.Empty<BarcodeScanlineCandidate>();
#if !NET8_0_OR_GREATER
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
#else
        if (pixels.IsEmpty) return false;
#endif
        if (width <= 0 || height <= 0 || stride < width * 4 || DecodeBudget.ShouldAbort(cancellationToken)) return false;

        var list = new List<BarcodeScanlineCandidate>(8);
        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, height / 2, cancellationToken, list);
        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, height / 3, cancellationToken, list);
        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, (height * 2) / 3, cancellationToken, list);
        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, width / 2, cancellationToken, list);
        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, width / 3, cancellationToken, list);
        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, (width * 2) / 3, cancellationToken, list);

        if (DecodeBudget.ShouldAbort(cancellationToken) || list.Count == 0) return false;
        candidates = list.ToArray();
        return true;
    }

    private static void TryPickBest(bool ok, bool[] candidate, ref int bestLen, ref bool[] best) {
        if (!ok) return;
        if (candidate.Length <= bestLen) return;
        bestLen = candidate.Length;
        best = candidate;
    }

    private static bool TryGetModulesFromHorizontal(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int y, CancellationToken cancellationToken, out bool[] modules) {
        modules = Array.Empty<bool>();
        if ((uint)y >= (uint)height) return false;
        var rented = ArrayPool<byte>.Shared.Rent(width);
        var luminance = rented.AsSpan(0, width);
        var offset = y * stride;
        var min = 255;
        var max = 0;

        try {
            for (var x = 0; x < width; x++) {
                if ((x & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var p = offset + x * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Rgba32) {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                } else {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                }
                var lum = (byte)((r * 54 + g * 183 + b * 19) >> 8);
                luminance[x] = lum;
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }

            return TryDecodeRuns(luminance, min, max, cancellationToken, out modules);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static bool TryGetModulesFromVertical(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, CancellationToken cancellationToken, out bool[] modules) {
        modules = Array.Empty<bool>();
        if ((uint)x >= (uint)width) return false;
        var rented = ArrayPool<byte>.Shared.Rent(height);
        var luminance = rented.AsSpan(0, height);
        var min = 255;
        var max = 0;

        try {
            for (var y = 0; y < height; y++) {
                if ((y & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var p = y * stride + x * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Rgba32) {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                } else {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                }
                var lum = (byte)((r * 54 + g * 183 + b * 19) >> 8);
                luminance[y] = lum;
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }

            return TryDecodeRuns(luminance, min, max, cancellationToken, out modules);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int min, int max, out bool[] modules) {
        return TryDecodeRuns(luminance, min, max, CancellationToken.None, out modules);
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int min, int max, CancellationToken cancellationToken, out bool[] modules) {
        modules = Array.Empty<bool>();
        var range = max - min;
        if (range <= 0) return false;

        var thresholds = range < 8
            ? new[] { (min + max) / 2 }
            : new[] {
                (min + max) / 2,
                min + range / 3,
                min + (range * 2) / 3,
                min + range / 4,
                min + (range * 3) / 4
            };

        for (var i = 0; i < thresholds.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryDecodeRuns(luminance, thresholds[i], cancellationToken, out modules)) return true;
        }

        return false;
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int threshold, out bool[] modules) {
        return TryDecodeRuns(luminance, threshold, CancellationToken.None, out modules);
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int threshold, CancellationToken cancellationToken, out bool[] modules) {
        return TryDecodeRuns(luminance, threshold, cancellationToken, out modules, out _, out _, out _);
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int threshold, CancellationToken cancellationToken, out bool[] modules, out bool[] minimumRunModules, out double fitError, out double minimumRunFitError) {
        modules = Array.Empty<bool>();
        minimumRunModules = Array.Empty<bool>();
        fitError = minimumRunFitError = 0;
        if (luminance.Length == 0) return false;

        var runs = ArrayPool<int>.Shared.Rent(luminance.Length);
        var runBars = ArrayPool<bool>.Shared.Rent(luminance.Length);
        var runCount = 0;
        var current = luminance[0] < threshold;
        var runLen = 1;

        try {
            for (var i = 1; i < luminance.Length; i++) {
                if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var isBar = luminance[i] < threshold;
                if (isBar == current) {
                    runLen++;
                } else {
                    runBars[runCount] = current;
                    runs[runCount++] = runLen;
                    current = isBar;
                    runLen = 1;
                }
            }
            runBars[runCount] = current;
            runs[runCount++] = runLen;

            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var start = 0;
            while (start < runCount && !runBars[start]) start++;
            var end = runCount - 1;
            while (end >= start && !runBars[end]) end--;
            if (start > end) return false;

            var minRun = int.MaxValue;
            for (var i = start; i <= end; i++) {
                if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                if (runs[i] < minRun) minRun = runs[i];
            }
            if (minRun <= 0) return false;
            var modulePitch = EstimateModulePitch(runs, start, end, minRun, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (!TrySampleRuns(runs, runBars, start, end, modulePitch, cancellationToken, out modules, out fitError)) return false;
            // Narrow-run distributions can be ambiguous. Keep the established
            // sampling as a bounded alternative; format/checksum validation still
            // decides whether either candidate represents a barcode.
            // The estimator returns a finite pitch at or above minRun.
            if (modulePitch > minRun) {
                if (!TrySampleRuns(runs, runBars, start, end, minRun, cancellationToken, out minimumRunModules, out minimumRunFitError)) return false;
            } else {
                minimumRunModules = modules;
                minimumRunFitError = fitError;
            }
            return true;
        } finally {
            ArrayPool<int>.Shared.Return(runs);
            ArrayPool<bool>.Shared.Return(runBars);
        }
    }

    private static bool TrySampleRuns(int[] runs, bool[] runBars, int start, int end, double pitch, CancellationToken cancellationToken, out bool[] modules, out double fitError) {
        modules = Array.Empty<bool>();
        fitError = 0;
        var totalModules = 0;
        for (var i = start; i <= end; i++) {
            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var count = Math.Max(1, (int)Math.Round(runs[i] / pitch));
            totalModules += count;
            var residual = runs[i] - count * pitch;
            fitError += residual * residual;
        }
        if (totalModules <= 0) return false;

        modules = new bool[totalModules];
        var offset = 0;
        for (var i = start; i <= end; i++) {
            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var count = Math.Max(1, (int)Math.Round(runs[i] / pitch));
            for (var m = 0; m < count; m++) modules[offset++] = runBars[i];
        }
        return true;
    }

    /// <summary>
    /// Estimates a fractional module pitch from pixel-rounded runs. The shortest
    /// observed run need not be the exact width of one module.
    /// </summary>
    private static double EstimateModulePitch(int[] runs, int start, int end, int minRun, CancellationToken cancellationToken) {
        // At one pixel, a two-pixel run may be either one rounded module or two
        // real modules. Retain the existing sampling instead of guessing.
        if (minRun < 2) return minRun;

        double narrowTotal = 0;
        var narrowCount = 0;
        for (var i = start; i <= end; i++) {
            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return minRun;
            // A fractional narrow run is sampled at two adjacent integer widths.
            // Avoid pulling wider elements into that initial cluster.
            if (runs[i] <= minRun + 1.0) {
                narrowTotal += runs[i];
                narrowCount++;
            }
        }

        var pitch = narrowTotal / narrowCount;
        // Refine using every run so that a biased distribution of rounded narrow
        // runs does not distort wider elements. Work remains bounded and linear.
        for (var pass = 0; pass < 4; pass++) {
            double weightedWidths = 0;
            double squaredModules = 0;
            for (var i = start; i <= end; i++) {
                if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return minRun;
                var count = Math.Max(1, Math.Round(runs[i] / pitch));
                weightedWidths += runs[i] * count;
                squaredModules += count * count;
            }
            var refined = Math.Max(minRun, Math.Min(minRun + 1.0, weightedWidths / squaredModules));
            if (Math.Abs(refined - pitch) < 0.0001) break;
            pitch = refined;
        }
        return pitch;
    }

    private static void TryCollectCandidatesFromHorizontal(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int y, CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates) {
        if ((uint)y >= (uint)height) return;
        var rented = ArrayPool<byte>.Shared.Rent(width);
        var luminance = rented.AsSpan(0, width);
        var offset = y * stride;
        var min = 255;
        var max = 0;

        try {
            for (var x = 0; x < width; x++) {
                if ((x & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return;
                var p = offset + x * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Rgba32) {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                } else {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                }
                var lum = (byte)((r * 54 + g * 183 + b * 19) >> 8);
                luminance[x] = lum;
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }

            TryCollectCandidates(luminance, min, max, cancellationToken, candidates, y, isVertical: false);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void TryCollectCandidatesFromVertical(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates) {
        if ((uint)x >= (uint)width) return;
        var rented = ArrayPool<byte>.Shared.Rent(height);
        var luminance = rented.AsSpan(0, height);
        var min = 255;
        var max = 0;

        try {
            for (var y = 0; y < height; y++) {
                if ((y & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return;
                var p = y * stride + x * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Rgba32) {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                } else {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                }
                var lum = (byte)((r * 54 + g * 183 + b * 19) >> 8);
                luminance[y] = lum;
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }

            TryCollectCandidates(luminance, min, max, cancellationToken, candidates, x, isVertical: true);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void TryCollectCandidates(ReadOnlySpan<byte> luminance, int min, int max, CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        if (max - min < 8) return;
        var range = max - min;
        var thresholds = new[] { (min + max) / 2, min + range / 3, min + (range * 2) / 3 };
        for (var i = 0; i < thresholds.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            if (TryDecodeRuns(luminance, thresholds[i], cancellationToken, out var modules, out var minimumRunModules, out var fitError, out var minimumRunFitError)) {
                var alternative = ModulesEqual(modules, minimumRunModules) ? Array.Empty<bool>() : modules;
                if (alternative.Length == 0) minimumRunFitError = Math.Min(minimumRunFitError, fitError);
                AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(minimumRunModules, position, isVertical, alternative, minimumRunFitError, fitError));
            }
        }
    }
    private static void AddUniqueLocatedCandidate(List<BarcodeScanlineCandidate> candidates, BarcodeScanlineCandidate candidate) {
        for (var i = 0; i < candidates.Count; i++) {
            var existing = candidates[i];
            if (existing.Position != candidate.Position || existing.IsVertical != candidate.IsVertical) continue;
            if (ModulesEqual(existing.Modules, candidate.Modules) && ModulesEqual(existing.AlternativeModules, candidate.AlternativeModules)) return;
        }
        candidates.Add(candidate);
    }

    private static bool ModulesEqual(bool[] left, bool[] right) {
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
}
