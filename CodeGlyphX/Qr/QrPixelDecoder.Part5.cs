#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TrySampleWithCorners(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        if (budget.IsExpired) {
            result = null!;
            moduleDiagnostics = default;
            return false;
        }

        if (TrySampleWithCornersInternal(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                scratch,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                loose: false,
                centerSampling: false,
                out result,
                out moduleDiagnostics)) {
            return true;
        }

        var strictDiag = moduleDiagnostics;
        var looseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
        if (aggressive && ShouldTryLooseSampling(moduleDiagnostics, moduleSizePx) &&
            TrySampleWithCornersInternal(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                scratch,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                loose: true,
                centerSampling: false,
                out result,
                out looseDiag)) {
            moduleDiagnostics = looseDiag;
            return true;
        }

        var best = Better(strictDiag, looseDiag);
        if (aggressive && ShouldTryCenterSampling(best, moduleSizePx)) {
            if (TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
                    cornerTlX,
                    cornerTlY,
                    cornerTrX,
                    cornerTrY,
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    loose: false,
                    centerSampling: true,
                    out result,
                    out var centerDiag)) {
                moduleDiagnostics = centerDiag;
                return true;
            }

            var centerLooseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
            if (ShouldTryLooseSampling(centerDiag, moduleSizePx) &&
                TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
                    cornerTlX,
                    cornerTlY,
                    cornerTrX,
                    cornerTrY,
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    loose: true,
                    centerSampling: true,
                    out result,
                    out centerLooseDiag)) {
                moduleDiagnostics = centerLooseDiag;
                return true;
            }

            best = Better(best, centerDiag);
            best = Better(best, centerLooseDiag);
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithCornersInternal(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool loose,
        bool centerSampling,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        // Build a perspective transform from module-corner space (0..dimension) to image-space and sample using it.
        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            dimension, 0,
            dimension, dimension,
            0, dimension,
            cornerTlX, cornerTlY,
            cornerTrX, cornerTrY,
            cornerBrX, cornerBrY,
            cornerBlX, cornerBlY);

        var bm = scratch;
        bm.Clear();
        var clampedLimit = dimension * 2;
        var mode = (centerSampling && moduleSizePx >= 1.25)
            ? 0
            : moduleSizePx >= 6.0
                ? 1
                : moduleSizePx >= 1.25
                    ? 2
                    : 3;
        var delta = mode switch {
            0 => QrPixelSampling.GetSampleDeltaCenterForModule(moduleSizePx),
            1 => QrPixelSampling.GetSampleDelta5x5ForModule(moduleSizePx),
            _ => QrPixelSampling.GetSampleDeltaForModule(moduleSizePx)
        };

        var sampledOk = mode switch {
            0 => loose
                ? SampleModules<Center3x3LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<Center3x3Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
            1 => loose
                ? SampleModules<Nearest25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<Nearest25Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
            2 => loose
                ? SampleModules<Nearest9LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<Nearest9Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
            _ => loose
                ? SampleModules<NinePxLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<NinePxSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
        };

        if (!sampledOk) return false;

        if (budget.IsNearDeadline(120)) return false;
        Func<bool>? shouldStop = budget.Enabled || budget.IsCancelled ? () => budget.IsExpired : null;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            moduleDiagnostics = moduleDiag;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        bm.Invert();
        try {
            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out moduleDiagInv)) {
                moduleDiagnostics = moduleDiagInv;
                if (accept == null || accept(result)) return true;
                return false;
            }
        } finally {
            bm.Invert();
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
    }

    private interface IModuleSampler {
        static abstract bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta);
    }

    private readonly struct Center3x3Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3WithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Center3x3LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3LooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxLooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxLooseWithDelta(image, sx, sy, invert, delta);
    }

    private static bool SampleModules<TSampler>(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        global::CodeGlyphX.BitMatrix bm,
        DecodeBudget budget,
        int clampedLimit,
        double delta,
        out int clamped)
        where TSampler : struct, IModuleSampler {
        clamped = 0;

        var bmWords = bm.Words;
        var bmWidth = dimension;
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var checkBudget = budget.Enabled || budget.IsCancelled;
        var budgetCounter = 0;
        var xStart = 0.5 + phaseX;

        for (var my = 0; my < dimension; my++) {
            if (checkBudget && budget.IsExpired) return false;
            var myc = my + 0.5 + phaseY;
            transform.GetRowParameters(
                xStart,
                myc,
                out var numX,
                out var numY,
                out var denom,
                out var stepNumX,
                out var stepNumY,
                out var stepDenom);

            if (double.IsNaN(numX) || double.IsNaN(numY) || double.IsNaN(denom) ||
                double.IsNaN(stepNumX) || double.IsNaN(stepNumY) || double.IsNaN(stepDenom) ||
                double.IsInfinity(numX) || double.IsInfinity(numY) || double.IsInfinity(denom) ||
                double.IsInfinity(stepNumX) || double.IsInfinity(stepNumY) || double.IsInfinity(stepDenom)) {
                return false;
            }

            var denomEnd = denom + stepDenom * (dimension - 1);
            if (double.IsNaN(denomEnd) || double.IsInfinity(denomEnd)) return false;
            if (Math.Abs(denom) < 1e-12 || Math.Abs(denomEnd) < 1e-12) return false;
            if (denom * denomEnd < 0) return false;

            var rowOffset = my * bmWidth;
            if (Math.Abs(stepDenom) < 1e-12) {
                var inv = 1.0 / denom;
                var sx = numX * inv;
                var sy = numY * inv;
                var sxStep = stepNumX * inv;
                var syStep = stepNumY * inv;

                for (var mx = 0; mx < dimension; mx++) {
                    if (checkBudget && ((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                    var sampleX = sx;
                    var sampleY = sy;
                    if (sampleX < 0) { sampleX = 0; clamped++; }
                    else if (sampleX > maxX) { sampleX = maxX; clamped++; }

                    if (sampleY < 0) { sampleY = 0; clamped++; }
                    else if (sampleY > maxY) { sampleY = maxY; clamped++; }

                    if (TSampler.Sample(image, sampleX, sampleY, invert, delta)) {
                        var bitIndex = rowOffset + mx;
                        bmWords[bitIndex >> 5] |= 1u << (bitIndex & 31);
                    }

                    sx += sxStep;
                    sy += syStep;
                }
            } else {
                for (var mx = 0; mx < dimension; mx++) {
                    if (checkBudget && ((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                    var inv = 1.0 / denom;
                    var sx = numX * inv;
                    var sy = numY * inv;

                    if (sx < 0) { sx = 0; clamped++; }
                    else if (sx > maxX) { sx = maxX; clamped++; }

                    if (sy < 0) { sy = 0; clamped++; }
                    else if (sy > maxY) { sy = maxY; clamped++; }

                    if (TSampler.Sample(image, sx, sy, invert, delta)) {
                        var bitIndex = rowOffset + mx;
                        bmWords[bitIndex >> 5] |= 1u << (bitIndex & 31);
                    }

                    numX += stepNumX;
                    numY += stepNumY;
                    denom += stepDenom;
                }
            }

            if (clamped > clampedLimit) return false;
        }

        return clamped <= clampedLimit;
    }

    private static bool ShouldTryLooseSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.0) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool ShouldTryCenterSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.5) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool TryDecodeByBoundingBox(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
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

        if (budget.IsExpired) return false;
        var width = image.Width;
        var height = image.Height;
        if (scanMaxX < 0) scanMaxX = width - 1;
        if (scanMaxY < 0) scanMaxY = height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= width) scanMaxX = width - 1;
        if (scanMaxY >= height) scanMaxY = height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        var minX = scanMaxX;
        var minY = scanMaxY;
        var maxX = -1;
        var maxY = -1;

        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var imageThreshold = image.Threshold;
        var checkBudget = budget.Enabled || budget.IsCancelled;
        var budgetCounter = 0;

        if (thresholdMap is null) {
            if (!invert) {
                for (var y = scanMinY; y <= scanMaxY; y++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var row = y * width;
                    for (var x = scanMinX; x <= scanMaxX; x++) {
                        if (checkBudget && ((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                        if (gray[row + x] > imageThreshold) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            } else {
                for (var y = scanMinY; y <= scanMaxY; y++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var row = y * width;
                    for (var x = scanMinX; x <= scanMaxX; x++) {
                        if (checkBudget && ((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                        if (gray[row + x] <= imageThreshold) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var y = scanMinY; y <= scanMaxY; y++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var row = y * width;
                    for (var x = scanMinX; x <= scanMaxX; x++) {
                        if (checkBudget && ((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                        var idx = row + x;
                        if (gray[idx] > thresholdMap[idx]) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            } else {
                for (var y = scanMinY; y <= scanMaxY; y++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var row = y * width;
                    for (var x = scanMinX; x <= scanMaxX; x++) {
                        if (checkBudget && ((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                        var idx = row + x;
                        if (gray[idx] <= thresholdMap[idx]) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }
        }

        if (maxX < 0) return false;

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
        for (var version = 1; version <= maxVersion; version++) {
            if (budget.IsExpired) return false;
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;

            var bm = new global::CodeGlyphX.BitMatrix(modulesCount, modulesCount);
            var bmWords = bm.Words;
            var bmWidth = modulesCount;
            for (var my = 0; my < modulesCount; my++) {
                if (budget.IsExpired) return false;
                var sy = minY + (my + 0.5) * moduleSizeY;
                var py = QrMath.RoundToInt(sy);
                if (py < 0) py = 0;
                else if (py >= height) py = height - 1;

                for (var mx = 0; mx < modulesCount; mx++) {
                    var sx = minX + (mx + 0.5) * moduleSizeX;
                    var px = QrMath.RoundToInt(sx);
                    if (px < 0) px = 0;
                    else if (px >= width) px = width - 1;

                    if (SampleMajority3x3(image, px, py, invert)) {
                        var bitIndex = my * bmWidth + mx;
                        bmWords[bitIndex >> 5] |= 1u << (bitIndex & 31);
                    }
                }
            }

            if (budget.IsNearDeadline(120)) return false;
            if (TryDecodeWithInversion(bm, accept, budget, out result, out var moduleDiag)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag);
                return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag));

            if (budget.IsNearDeadline(120)) return false;
            if (TryDecodeByRotations(bm, accept, budget, out result, out var moduleDiagRot)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot);
                return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot));
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
                    count += BitOperations.PopCount(mask);
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

    private static int NearestValidDimension(int dimension) {
        // Valid sizes are 17 + 4*version => dimension mod 4 == 1.
        var best = -1;
        var bestDiff = int.MaxValue;

        for (var delta = -2; delta <= 2; delta++) {
            var d = dimension + delta;
            if (d < 21 || d > 177) continue;
            if ((d & 3) != 1) continue;

            var diff = Math.Abs(delta);
            if (diff < bestDiff) {
                bestDiff = diff;
                best = d;
            }
        }

        return best;
    }

    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool SampleMajority3x3(QrGrayImage image, int px, int py, bool invert) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var black = 0;
        if (thresholdMap is null) {
            if (!invert) {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        if (gray[row + x] <= threshold) black++;
                    }
                }
            } else {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        if (gray[row + x] > threshold) black++;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        var idx = row + x;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                    }
                }
            } else {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        var idx = row + x;
                        if (gray[idx] > thresholdMap[idx]) black++;
                    }
                }
            }
        }

        return black >= 5;
    }

    private static bool TryDecodeWithInversion(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        Func<bool>? shouldStop = budget.Enabled || budget.IsCancelled ? () => budget.IsExpired : null;
        var ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag);
        var best = moduleDiag;
        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiag;
            return true;
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            diagnostics = best;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        matrix.Invert();
        try {
            ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out moduleDiagInv);
        } finally {
            matrix.Invert();
        }
        best = Better(best, moduleDiagInv);

        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiagInv;
            return true;
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeByRotations(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        var rotated = new global::CodeGlyphX.BitMatrix(matrix.Width, matrix.Height);
        RotateInto90(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        RotateInto180(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        RotateInto270(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        diagnostics = best;
        return false;
    }

    private static void RotateInto90(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = height - 1 - y;
                var ry = x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto180(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = width - 1 - x;
                var ry = height - 1 - y;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto270(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = y;
                var ry = width - 1 - x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static QrPixelDecodeDiagnostics Better(QrPixelDecodeDiagnostics a, QrPixelDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        // Pick the attempt that got "furthest" (format ok > RS > payload), then the one with lower format distance.
        var sa = Score(a.ModuleDiagnostics);
        var sb = Score(b.ModuleDiagnostics);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.ModuleDiagnostics.FormatBestDistance;
        var db = b.ModuleDiagnostics.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

}
#endif
