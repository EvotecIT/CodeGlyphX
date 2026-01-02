#if NET8_0_OR_GREATER
using System;

namespace CodeMatrix.Qr;

internal static class QrPixelDecoder {
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        result = null!;

        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (pixels.Length < (height - 1) * stride + width * 4) return false;

        // Prefer a full finder-pattern based decode (robust to extra background/noise).
        if (TryDecodeAtScale(pixels, width, height, stride, fmt, 1, out result)) return true;

        // Optional downscale passes (often helps with UI-scaled / anti-aliased QR).
        var minDim = Math.Min(width, height);
        if (minDim >= 160 && TryDecodeAtScale(pixels, width, height, stride, fmt, 2, out result)) return true;
        if (minDim >= 800 && TryDecodeAtScale(pixels, width, height, stride, fmt, 3, out result)) return true;

        return false;
    }

    private static bool TryDecodeAtScale(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, out QrDecoded result) {
        result = null!;

        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, out var image)) return false;

        // Try normal polarity first, then inverted.
        if (TryDecodeFromGray(image, invert: false, out result)) return true;
        if (TryDecodeFromGray(image, invert: true, out result)) return true;

        return false;
    }

    private static bool TryDecodeFromGray(QrGrayImage image, bool invert, out QrDecoded result) {
        result = null!;

        // Finder-based sampling.
        if (QrFinderPatternDetector.TryFind(image, invert, out var tl, out var tr, out var bl)) {
            if (TrySampleAndDecode(image, invert, tl, tr, bl, out result)) return true;
        }

        // Fallback: bounding box exact-fit (works for perfectly cropped/generated images).
        if (TryDecodeByBoundingBox(image, invert, out result)) return true;

        return false;
    }

    private static bool TrySampleAndDecode(QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, out QrDecoded result) {
        result = null!;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

        var distX = Distance(tl.X, tl.Y, tr.X, tr.Y);
        var distY = Distance(tl.X, tl.Y, bl.X, bl.Y);
        if (distX <= 0 || distY <= 0) return false;

        var dimH = (int)Math.Round(distX / moduleSize) + 7;
        var dimV = (int)Math.Round(distY / moduleSize) + 7;

        // Try a few nearby dimensions (estimation can be off on UI-scaled QR).
        var baseDim = NearestValidDimension((dimH + dimV) / 2);
        Span<int> candidates = stackalloc int[6];
        var candidatesCount = 0;
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim);
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimH));
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimV));
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 8);

        for (var i = 0; i < candidatesCount; i++) {
            var dimension = candidates[i];
            if (dimension is < 21 or > 177) continue;
            if (TrySampleAndDecodeDimension(image, invert, tl, tr, bl, dimension, out result)) return true;
        }

        return false;
    }

    private static void AddDimensionCandidate(ref Span<int> list, ref int count, int dimension) {
        if (dimension is < 21 or > 177) return;
        if ((dimension & 3) != 1) return;
        for (var i = 0; i < count; i++) {
            if (list[i] == dimension) return;
        }
        list[count++] = dimension;
    }

    private static bool TrySampleAndDecodeDimension(QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, out QrDecoded result) {
        result = null!;

        var modulesBetweenCenters = dimension - 7;
        if (modulesBetweenCenters <= 0) return false;

        // Use affine mapping based on the three finder centers (no perspective correction yet).
        var vxX = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY = (bl.Y - tl.Y) / modulesBetweenCenters;

        var bm = new global::CodeMatrix.BitMatrix(dimension, dimension);

        var clamped = 0;

        for (var my = 0; my < dimension; my++) {
            var dy = my - 3;
            for (var mx = 0; mx < dimension; mx++) {
                var dx = mx - 3;

                var sx = tl.X + vxX * dx + vyX * dy;
                var sy = tl.Y + vxY * dx + vyY * dy;

                var px = (int)Math.Round(sx);
                var py = (int)Math.Round(sy);

                if (px < 0) { px = 0; clamped++; }
                else if (px >= image.Width) { px = image.Width - 1; clamped++; }

                if (py < 0) { py = 0; clamped++; }
                else if (py >= image.Height) { py = image.Height - 1; clamped++; }

                bm[mx, my] = SampleMajority3x3(image, px, py, invert);
            }
        }

        // If we had to clamp too many samples, the region is likely cropped too tight or the estimate is wrong.
        if (clamped > dimension * 2) return false;

        if (global::CodeMatrix.QrDecoder.TryDecode(bm, out result)) return true;

        var inv = bm.Clone();
        Invert(inv);
        if (global::CodeMatrix.QrDecoder.TryDecode(inv, out result)) return true;

        return false;
    }

    private static bool TryDecodeByBoundingBox(QrGrayImage image, bool invert, out QrDecoded result) {
        result = null!;

        var minX = image.Width;
        var minY = image.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                if (!image.IsBlack(x, y, invert)) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return false;

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
        if (maxVersion < 1) return false;

        // Try smaller versions first (more likely for OTP QR), but accept non-integer module sizes.
        for (var version = 1; version <= maxVersion; version++) {
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;

            var bm = new global::CodeMatrix.BitMatrix(modulesCount, modulesCount);
            for (var my = 0; my < modulesCount; my++) {
                var sy = minY + (my + 0.5) * moduleSizeY;
                var py = (int)Math.Round(sy);
                if (py < 0) py = 0;
                else if (py >= image.Height) py = image.Height - 1;

                for (var mx = 0; mx < modulesCount; mx++) {
                    var sx = minX + (mx + 0.5) * moduleSizeX;
                    var px = (int)Math.Round(sx);
                    if (px < 0) px = 0;
                    else if (px >= image.Width) px = image.Width - 1;

                    bm[mx, my] = SampleMajority3x3(image, px, py, invert);
                }
            }

            if (global::CodeMatrix.QrDecoder.TryDecode(bm, out result)) return true;

            var inv = bm.Clone();
            Invert(inv);
            if (global::CodeMatrix.QrDecoder.TryDecode(inv, out result)) return true;
        }

        return false;
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

    private static void Invert(global::CodeMatrix.BitMatrix matrix) {
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                matrix[x, y] = !matrix[x, y];
            }
        }
    }
}
#endif
