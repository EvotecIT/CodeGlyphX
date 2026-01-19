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

        var bm = new global::CodeGlyphX.BitMatrix(dimension, dimension);

        var clamped = 0;

        for (var my = 0; my < dimension; my++) {
            if (budget.IsExpired) return false;
            for (var mx = 0; mx < dimension; mx++) {
                if (budget.IsExpired) return false;
                var mxc = mx + 0.5 + phaseX;
                var myc = my + 0.5 + phaseY;

                transform.Transform(mxc, myc, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) return false;

                if (sx < 0) { sx = 0; clamped++; }
                else if (sx > image.Width - 1) { sx = image.Width - 1; clamped++; }

                if (sy < 0) { sy = 0; clamped++; }
                else if (sy > image.Height - 1) { sy = image.Height - 1; clamped++; }

                // When modules are reasonably large, nearest-neighbor majority sampling is more stable than bilinear
                // (bilinear can blur binary UI edges into mid-gray values around the threshold).
                // Prefer a tighter sampling pattern for typical UI-rendered QRs (3–6 px/module).
                // 5x5 sampling is more sensitive to small transform errors; use it only when modules are large.
                if (centerSampling && moduleSizePx >= 1.25) {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModuleCenter3x3Loose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModuleCenter3x3(image, sx, sy, invert, moduleSizePx);
                } else if (moduleSizePx >= 6.0) {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule25NearestLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule25Nearest(image, sx, sy, invert, moduleSizePx);
                } else if (moduleSizePx >= 1.25) {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule9NearestLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule9Nearest(image, sx, sy, invert, moduleSizePx);
                } else {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule9PxLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule9Px(image, sx, sy, invert, moduleSizePx);
                }
            }
        }

        // If we had to clamp too many samples, the region is likely cropped too tight or the estimate is wrong.
        if (clamped > dimension * 2) return false;

        if (budget.IsNearDeadline(120)) return false;
        Func<bool>? shouldStop = budget.Enabled || budget.IsCancelled ? () => budget.IsExpired : null;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        var inv = bm.Clone();
        Invert(inv);
        if (budget.IsNearDeadline(120)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(inv, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv)) {
            moduleDiagnostics = moduleDiagInv;
            if (accept == null || accept(result)) return true;
            return false;
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
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
        if (scanMaxX < 0) scanMaxX = image.Width - 1;
        if (scanMaxY < 0) scanMaxY = image.Height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= image.Width) scanMaxX = image.Width - 1;
        if (scanMaxY >= image.Height) scanMaxY = image.Height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        var minX = scanMaxX;
        var minY = scanMaxY;
        var maxX = -1;
        var maxY = -1;

        for (var y = scanMinY; y <= scanMaxY; y++) {
            if (budget.IsExpired) return false;
            for (var x = scanMinX; x <= scanMaxX; x++) {
                if (!image.IsBlack(x, y, invert)) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return false;

        TrimBoundingBox(image, invert, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return false;

        // Expand a touch to counter anti-aliasing that can shrink the detected black bbox.
        if (minX > 0) minX--;
        if (minY > 0) minY--;
        if (maxX < image.Width - 1) maxX++;
        if (maxY < image.Height - 1) maxY++;

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
            for (var my = 0; my < modulesCount; my++) {
                if (budget.IsExpired) return false;
                var sy = minY + (my + 0.5) * moduleSizeY;
                var py = QrMath.RoundToInt(sy);
                if (py < 0) py = 0;
                else if (py >= image.Height) py = image.Height - 1;

                for (var mx = 0; mx < modulesCount; mx++) {
                    var sx = minX + (mx + 0.5) * moduleSizeX;
                    var px = QrMath.RoundToInt(sx);
                    if (px < 0) px = 0;
                    else if (px >= image.Width) px = image.Width - 1;

                    bm[mx, my] = SampleMajority3x3(image, px, py, invert);
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
        var count = 0;
        var width = maxX - minX + 1;
        if (width <= 0) return 0;

        if (image.ThresholdMap is null && Sse2.IsSupported && width >= 16) {
            var gray = image.Gray;
            var start = y * image.Width + minX;
            var end = start + width;
            var i = start;
            var offset = Vector128.Create((byte)0x80);
            var threshold = Vector128.Create((byte)(image.Threshold ^ 0x80));

            while (i + 16 <= end) {
                var vec = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(i));
                var signed = Sse2.Xor(vec, offset).AsSByte();
                var gt = Sse2.CompareGreaterThan(signed, threshold.AsSByte());
                var mask = (uint)Sse2.MoveMask(gt.AsByte());
                count += BitOperations.PopCount(mask);
                i += 16;
            }

            for (; i < end; i++) {
                if (gray[i] > image.Threshold) count++;
            }

            return invert ? count : width - count;
        }

        for (var x = minX; x <= maxX; x++) {
            if (image.IsBlack(x, y, invert)) count++;
        }
        return count;
    }

    private static int CountDarkCol(QrGrayImage image, bool invert, int x, int minY, int maxY) {
        var count = 0;
        for (var y = minY; y <= maxY; y++) {
            if (image.IsBlack(x, y, invert)) count++;
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
        var black = 0;

        for (var dy = -1; dy <= 1; dy++) {
            var y = py + dy;
            if (y < 0) y = 0;
            else if (y >= image.Height) y = image.Height - 1;

            for (var dx = -1; dx <= 1; dx++) {
                var x = px + dx;
                if (x < 0) x = 0;
                else if (x >= image.Width) x = image.Width - 1;

                if (image.IsBlack(x, y, invert)) black++;
            }
        }

        return black >= 5;
    }

    private static void Invert(global::CodeGlyphX.BitMatrix matrix) {
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                matrix[x, y] = !matrix[x, y];
            }
        }
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

        var inv = matrix.Clone();
        Invert(inv);
        ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(inv, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv);
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

        var rot90 = Rotate90(matrix);
        if (TryDecodeWithInversion(rot90, accept, budget, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        var rot180 = Rotate180(matrix);
        if (TryDecodeWithInversion(rot180, accept, budget, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        var rot270 = Rotate270(matrix);
        if (TryDecodeWithInversion(rot270, accept, budget, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        diagnostics = best;
        return false;
    }

    private static global::CodeGlyphX.BitMatrix Rotate90(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Height - 1 - y, x] = matrix[x, y];
            }
        }
        return result;
    }

    private static global::CodeGlyphX.BitMatrix Rotate180(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, matrix.Height - 1 - y] = matrix[x, y];
            }
        }
        return result;
    }

    private static global::CodeGlyphX.BitMatrix Rotate270(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[y, matrix.Width - 1 - x] = matrix[x, y];
            }
        }
        return result;
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
