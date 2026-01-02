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

        // Optional downscale pass for very large captures.
        if (Math.Min(width, height) >= 400 && TryDecodeAtScale(pixels, width, height, stride, fmt, 2, out result)) return true;

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
        var dimension = NearestValidDimension((dimH + dimV) / 2);
        if (dimension is < 21 or > 177) return false;

        var modulesBetweenCenters = dimension - 7;
        if (modulesBetweenCenters <= 0) return false;

        // Use affine mapping based on the three finder centers (no perspective correction yet).
        var vxX = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY = (bl.Y - tl.Y) / modulesBetweenCenters;

        var bm = new global::CodeMatrix.BitMatrix(dimension, dimension);

        for (var my = 0; my < dimension; my++) {
            var dy = my - 3;
            for (var mx = 0; mx < dimension; mx++) {
                var dx = mx - 3;

                var sx = tl.X + vxX * dx + vyX * dy;
                var sy = tl.Y + vxY * dx + vyY * dy;

                var px = (int)Math.Round(sx);
                var py = (int)Math.Round(sy);
                if ((uint)px >= (uint)image.Width || (uint)py >= (uint)image.Height) return false;

                bm[mx, my] = image.IsBlack(px, py, invert);
            }
        }

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
        var boxW = maxX - minX + 1;
        var boxH = maxY - minY + 1;
        if (boxW <= 0 || boxH <= 0) return false;

        for (var version = 1; version <= 40; version++) {
            var modulesCount = version * 4 + 17;
            if (boxW % modulesCount != 0 || boxH % modulesCount != 0) continue;
            var moduleSize = boxW / modulesCount;
            if (moduleSize <= 0) continue;
            if (boxH / modulesCount != moduleSize) continue;

            var bm = new global::CodeMatrix.BitMatrix(modulesCount, modulesCount);
            for (var my = 0; my < modulesCount; my++) {
                var py = minY + my * moduleSize + moduleSize / 2;
                for (var mx = 0; mx < modulesCount; mx++) {
                    var px = minX + mx * moduleSize + moduleSize / 2;
                    bm[mx, my] = image.IsBlack(px, py, invert);
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

    private static void Invert(global::CodeMatrix.BitMatrix matrix) {
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                matrix[x, y] = !matrix[x, y];
            }
        }
    }
}
#endif
