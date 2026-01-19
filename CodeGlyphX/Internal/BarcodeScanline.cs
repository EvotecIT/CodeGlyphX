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
        if (cancellationToken.IsCancellationRequested) return false;

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

    public static bool TryGetModuleCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out bool[][] candidates) {
        return TryGetModuleCandidates(pixels, width, height, stride, format, CancellationToken.None, out candidates);
    }

    public static bool TryGetModuleCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out bool[][] candidates) {
        candidates = Array.Empty<bool[]>();
#if !NET8_0_OR_GREATER
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
#else
        if (pixels.IsEmpty) return false;
#endif
        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (cancellationToken.IsCancellationRequested) return false;

        var list = new List<bool[]>(8);

        var y0 = height / 2;
        var y1 = height / 3;
        var y2 = (height * 2) / 3;

        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, y0, cancellationToken, list);
        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, y1, cancellationToken, list);
        TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, y2, cancellationToken, list);

        var x0 = width / 2;
        var x1 = width / 3;
        var x2 = (width * 2) / 3;

        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, x0, cancellationToken, list);
        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, x1, cancellationToken, list);
        TryCollectCandidatesFromVertical(pixels, width, height, stride, format, x2, cancellationToken, list);

        if (cancellationToken.IsCancellationRequested) return false;
        if (list.Count == 0) return false;
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
                if ((x & 127) == 0 && cancellationToken.IsCancellationRequested) return false;
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
                if ((y & 127) == 0 && cancellationToken.IsCancellationRequested) return false;
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
        if (max - min < 8) return false;
        var threshold = (min + max) / 2;
        return TryDecodeRuns(luminance, threshold, cancellationToken, out modules);
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int threshold, out bool[] modules) {
        return TryDecodeRuns(luminance, threshold, CancellationToken.None, out modules);
    }

    private static bool TryDecodeRuns(ReadOnlySpan<byte> luminance, int threshold, CancellationToken cancellationToken, out bool[] modules) {
        modules = Array.Empty<bool>();
        if (luminance.Length == 0) return false;

        var runs = ArrayPool<int>.Shared.Rent(luminance.Length);
        var runBars = ArrayPool<bool>.Shared.Rent(luminance.Length);
        var runCount = 0;
        var current = luminance[0] < threshold;
        var runLen = 1;

        try {
            for (var i = 1; i < luminance.Length; i++) {
                if ((i & 255) == 0 && cancellationToken.IsCancellationRequested) return false;
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

            if (cancellationToken.IsCancellationRequested) return false;
            var start = 0;
            while (start < runCount && !runBars[start]) start++;
            var end = runCount - 1;
            while (end >= start && !runBars[end]) end--;
            if (start > end) return false;

            var minRun = int.MaxValue;
            for (var i = start; i <= end; i++) {
                if ((i & 255) == 0 && cancellationToken.IsCancellationRequested) return false;
                if (runs[i] < minRun) minRun = runs[i];
            }
            if (minRun <= 0) return false;

            var totalModules = 0;
            for (var i = start; i <= end; i++) {
                if ((i & 255) == 0 && cancellationToken.IsCancellationRequested) return false;
                var modulesCount = (int)Math.Round(runs[i] / (double)minRun);
                if (modulesCount < 1) modulesCount = 1;
                totalModules += modulesCount;
            }
            if (totalModules <= 0) return false;

            modules = new bool[totalModules];
            var offset = 0;
            for (var i = start; i <= end; i++) {
                if ((i & 255) == 0 && cancellationToken.IsCancellationRequested) return false;
                var modulesCount = (int)Math.Round(runs[i] / (double)minRun);
                if (modulesCount < 1) modulesCount = 1;
                var isBar = runBars[i];
                for (var m = 0; m < modulesCount; m++) {
                    modules[offset++] = isBar;
                }
            }

            return true;
        } finally {
            ArrayPool<int>.Shared.Return(runs);
            ArrayPool<bool>.Shared.Return(runBars);
        }
    }

    private static void TryCollectCandidatesFromHorizontal(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int y, CancellationToken cancellationToken, List<bool[]> candidates) {
        if ((uint)y >= (uint)height) return;
        var rented = ArrayPool<byte>.Shared.Rent(width);
        var luminance = rented.AsSpan(0, width);
        var offset = y * stride;
        var min = 255;
        var max = 0;

        try {
            for (var x = 0; x < width; x++) {
                if ((x & 127) == 0 && cancellationToken.IsCancellationRequested) return;
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

            TryCollectCandidates(luminance, min, max, cancellationToken, candidates);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void TryCollectCandidatesFromVertical(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, CancellationToken cancellationToken, List<bool[]> candidates) {
        if ((uint)x >= (uint)width) return;
        var rented = ArrayPool<byte>.Shared.Rent(height);
        var luminance = rented.AsSpan(0, height);
        var min = 255;
        var max = 0;

        try {
            for (var y = 0; y < height; y++) {
                if ((y & 127) == 0 && cancellationToken.IsCancellationRequested) return;
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

            TryCollectCandidates(luminance, min, max, cancellationToken, candidates);
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void TryCollectCandidates(ReadOnlySpan<byte> luminance, int min, int max, List<bool[]> candidates) {
        TryCollectCandidates(luminance, min, max, CancellationToken.None, candidates);
    }

    private static void TryCollectCandidates(ReadOnlySpan<byte> luminance, int min, int max, CancellationToken cancellationToken, List<bool[]> candidates) {
        if (max - min < 8) return;
        var range = max - min;
        var thresholds = new[] { (min + max) / 2, min + range / 3, min + (range * 2) / 3 };
        for (var i = 0; i < thresholds.Length; i++) {
            if (cancellationToken.IsCancellationRequested) return;
            if (TryDecodeRuns(luminance, thresholds[i], cancellationToken, out var modules)) {
                AddUniqueCandidate(candidates, modules);
            }
        }
    }

    private static void AddUniqueCandidate(List<bool[]> candidates, bool[] modules) {
        if (modules.Length == 0) return;
        for (var i = 0; i < candidates.Count; i++) {
            var existing = candidates[i];
            if (existing.Length != modules.Length) continue;
            var equal = true;
            for (var j = 0; j < modules.Length; j++) {
                if (existing[j] != modules[j]) { equal = false; break; }
            }
            if (equal) return;
        }
        candidates.Add(modules);
    }
}
