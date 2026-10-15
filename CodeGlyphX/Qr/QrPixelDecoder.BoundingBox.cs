#if NET8_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TryDecodeByBoundingBox(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics,
        int candidateCount = 0,
        int candidateTriplesTried = 0,
        int scanMinX = 0,
        int scanMinY = 0,
        int scanMaxX = -1,
        int scanMaxY = -1) {
        result = null!;
        diagnostics = default;

        var width = image.Width;
        var height = image.Height;
        if (scanMaxX < 0) scanMaxX = width - 1;
        if (scanMaxY < 0) scanMaxY = height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= width) scanMaxX = width - 1;
        if (scanMaxY >= height) scanMaxY = height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        if (!TryFindForegroundBounds(image, invert, budget, scanMinX, scanMinY, scanMaxX, scanMaxY,
                out var minX, out var minY, out var maxX, out var maxY)) return false;
        var checkBudget = budget.CanAbort;
        TrimBoundingBox(image, invert, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return false;

        // Expand a touch to counter anti-aliasing that can shrink the detected black bbox.
        if (minX > 0) minX--;
        if (minY > 0) minY--;
        if (maxX < width - 1) maxX++;
        if (maxY < height - 1) maxY++;

        var boxW = maxX - minX + 1;
        var boxH = maxY - minY + 1;
        if (boxW <= 0 || boxH <= 0) return false;

        var maxModules = Math.Min(boxW, boxH);
        var maxVersion = Math.Min(40, (maxModules - 17) / 4);
        if (budget.Enabled && maxVersion > 10) {
            maxVersion = 10;
        }
        if (maxVersion < 1) return false;
        // Try smaller versions first (more likely for OTP QR), but accept non-integer module sizes.
        var best = default(QrPixelDecodeDiagnostics);
        Span<double> phases = stackalloc double[3];
        for (var version = 1; version <= maxVersion; version++) {
            if (checkBudget && budget.IsExpired) return false;
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;
            var moduleSize = (moduleSizeX + moduleSizeY) * 0.5;
            var useRing = stylized && moduleSize >= 1.25 && !(checkBudget && budget.IsNearDeadline(200));
            var ringDelta = useRing ? QrPixelSampling.GetSampleDeltaRingForModule(moduleSize) : 0.0;

            phases[0] = 0.5;
            var phaseCount = 1;
            if (aggressive && moduleSize >= 2.0 && !(checkBudget && budget.IsNearDeadline(150))) {
                phases[phaseCount++] = 0.42;
                phases[phaseCount++] = 0.58;
            }

            for (var p = 0; p < phaseCount; p++) {
                if (checkBudget && budget.IsExpired) return false;
                var phase = phases[p];
                if (!HasPlausibleBoundingFinders(image, minX, minY, modulesCount, moduleSizeX, moduleSizeY,
                    phase, invert, useRing, ringDelta, budget)) continue;
                var bm = new global::CodeGlyphX.BitMatrix(modulesCount, modulesCount);
                var bmWords = bm.Words;
                var bmWidth = modulesCount;

                for (var my = 0; my < modulesCount; my++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var sy = minY + (my + phase) * moduleSizeY;

                    var rowOffset = my * bmWidth;
                    var wordIndex = rowOffset >> 5;
                    var bitMask = 1u << (rowOffset & 31);
                    for (var mx = 0; mx < modulesCount; mx++) {
                        var sx = minX + (mx + phase) * moduleSizeX;
                        var isBlack = moduleSize >= 2.0
                            ? (useRing
                                ? QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, ringDelta)
                                : QrPixelSampling.SampleModule9Px(image, sx, sy, invert, moduleSize))
                            : (useRing
                                ? QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, ringDelta)
                                : QrPixelSampling.SampleModuleMajority3x3(image, sx, sy, invert));
                        if (isBlack) {
                            bmWords[wordIndex] |= bitMask;
                        }

                        bitMask <<= 1;
                        if (bitMask == 0) {
                            bitMask = 1u;
                            wordIndex++;
                        }
                    }
                }

                if (checkBudget && budget.IsNearDeadline(120)) return false;
                if (TryDecodeWithInversion(bm, accept, budget, out result, out var moduleDiag)) {
                    diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag);
                    return true;
                }
                best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag));

                if (checkBudget && budget.IsNearDeadline(120)) return false;
                if (TryDecodeByRotations(bm, accept, budget, out result, out var moduleDiagRot)) {
                    diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot);
                    return true;
                }
                best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot));
            }
        }

        diagnostics = best;
        return false;
    }

    private static bool TryGetCandidateBounds(
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        int width,
        int height,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) {
            minX = minY = maxX = maxY = 0;
            return false;
        }

        var pad = moduleSize * 8.0;
        var minXF = Math.Min(tl.X, Math.Min(tr.X, bl.X)) - pad;
        var maxXF = Math.Max(tl.X, Math.Max(tr.X, bl.X)) + pad;
        var minYF = Math.Min(tl.Y, Math.Min(tr.Y, bl.Y)) - pad;
        var maxYF = Math.Max(tl.Y, Math.Max(tr.Y, bl.Y)) + pad;

        minX = Math.Clamp(QrMath.RoundToInt(minXF), 0, width - 1);
        maxX = Math.Clamp(QrMath.RoundToInt(maxXF), 0, width - 1);
        minY = Math.Clamp(QrMath.RoundToInt(minYF), 0, height - 1);
        maxY = Math.Clamp(QrMath.RoundToInt(maxYF), 0, height - 1);

        return minX < maxX && minY < maxY;
    }

    private static void TrimBoundingBox(QrGrayImage image, bool invert, ref int minX, ref int minY, ref int maxX, ref int maxY) {
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (width <= 0 || height <= 0) return;

        var rowThreshold = Math.Max(2, width / 40);
        var colThreshold = Math.Max(2, height / 40);

        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, minY) <= rowThreshold) minY++;
        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, maxY) <= rowThreshold) maxY--;
        while (minX <= maxX && CountDarkCol(image, invert, minX, minY, maxY) <= colThreshold) minX++;
        while (minX <= maxX && CountDarkCol(image, invert, maxX, minY, maxY) <= colThreshold) maxX--;
    }

    private static int CountDarkRow(QrGrayImage image, bool invert, int minX, int maxX, int y) {
        var width = maxX - minX + 1;
        if (width <= 0) return 0;

        var gray = image.Gray;
        var start = y * image.Width + minX;
        var end = start + width;
        var thresholdMap = image.ThresholdMap;
        var count = 0;

        if (thresholdMap is null) {
            var threshold = image.Threshold;
            if (Sse2.IsSupported && width >= 16) {
                count = 0;
                var i = start;
                var offset = Vector128.Create((byte)0x80);
                var thresholdVec = Vector128.Create((byte)(threshold ^ 0x80));

                while (i + 16 <= end) {
                    var vec = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(i));
                    var signed = Sse2.Xor(vec, offset).AsSByte();
                    var gt = Sse2.CompareGreaterThan(signed, thresholdVec.AsSByte());
                    var mask = (uint)Sse2.MoveMask(gt.AsByte());
                    count += CodeGlyphX.Internal.BitOps.PopCount(mask);
                    i += 16;
                }

                for (; i < end; i++) {
                    if (gray[i] > threshold) count++;
                }

                return invert ? count : width - count;
            }

            count = 0;
            if (!invert) {
                for (var i = start; i < end; i++) {
                    if (gray[i] <= threshold) count++;
                }
            } else {
                for (var i = start; i < end; i++) {
                    if (gray[i] > threshold) count++;
                }
            }
            return count;
        }

        count = 0;
        if (!invert) {
            for (var i = start; i < end; i++) {
                if (gray[i] <= thresholdMap[i]) count++;
            }
        } else {
            for (var i = start; i < end; i++) {
                if (gray[i] > thresholdMap[i]) count++;
            }
        }
        return count;
    }

    private static int CountDarkCol(QrGrayImage image, bool invert, int x, int minY, int maxY) {
        var width = image.Width;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var count = 0;
        if (thresholdMap is null) {
            if (!invert) {
                for (var y = minY; y <= maxY; y++) {
                    var idx = y * width + x;
                    if (gray[idx] <= threshold) count++;
                }
            } else {
                for (var y = minY; y <= maxY; y++) {
                    var idx = y * width + x;
                    if (gray[idx] > threshold) count++;
                }
            }
            return count;
        }

        if (!invert) {
            for (var y = minY; y <= maxY; y++) {
                var idx = y * width + x;
                if (gray[idx] <= thresholdMap[idx]) count++;
            }
        } else {
            for (var y = minY; y <= maxY; y++) {
                var idx = y * width + x;
                if (gray[idx] > thresholdMap[idx]) count++;
            }
        }
        return count;
    }

}
#endif
