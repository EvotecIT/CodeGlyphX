#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    // Exact two-tone rasters need one sample per module. Neighborhood voting
    // destroys data at one pixel per module; blind version searches also waste
    // the allowance when integer finder borders already determine the grid.
    private static bool TryDecodeExactGrid(int scale, QrGrayImage image, DecodeBudget budget,
        Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;
        if (image.ThresholdMap != null || image.Min == image.Max || budget.IsExpired) return false;

        // Capture both polarities in one pass. Ordinary photographs exit at the
        // first intermediate level. No thresholds or approximate pitches are tried.
        Span<int> bounds = stackalloc int[8] {
            image.Width, image.Height, -1, -1, image.Width, image.Height, -1, -1
        };
        for (var y = 0; y < image.Height; y++) {
            if (budget.IsExpired) return false;
            var row = y * image.Width;
            for (var x = 0; x < image.Width; x++) {
                if ((x & 255) == 0 && budget.IsExpired) return false;
                var value = image.Gray[row + x];
                if (value != image.Min && value != image.Max) return false;
                var offset = value == image.Min ? 0 : 4;
                if (x < bounds[offset]) bounds[offset] = x;
                if (y < bounds[offset + 1]) bounds[offset + 1] = y;
                if (x > bounds[offset + 2]) bounds[offset + 2] = x;
                if (y > bounds[offset + 3]) bounds[offset + 3] = y;
            }
        }

        Span<int> triedPitches = stackalloc int[8];
        for (var polarity = 0; polarity < 2; polarity++) {
            var offset = polarity * 4;
            var left = bounds[offset];
            var top = bounds[offset + 1];
            var right = bounds[offset + 2];
            var bottom = bounds[offset + 3];
            var boxWidth = right - left + 1;
            var boxHeight = bottom - top + 1;
            if (boxWidth < 21 || boxHeight < 21) continue;
            var darkLevel = polarity == 0 ? image.Min : image.Max;
            var tried = 0;
            for (var corner = 0; corner < 4; corner++) {
                if (budget.IsExpired) return false;
                var x = (corner & 1) == 0 ? left : right;
                var y = (corner & 2) == 0 ? top : bottom;
                var dx = (corner & 1) == 0 ? 1 : -1;
                var dy = (corner & 2) == 0 ? 1 : -1;
                var runX = ExactGridBorderRun(image, x, y, dx, 0, boxWidth, darkLevel, budget);
                var runY = ExactGridBorderRun(image, x, y, 0, dy, boxHeight, darkLevel, budget);
                if (runX <= 0 || runY <= 0 || runX % 7 != 0 || runY % 7 != 0) continue;
                var pitchX = runX / 7;
                var pitchY = runY / 7;
                if (boxWidth % pitchX != 0 || boxHeight % pitchY != 0) continue;
                var dimension = boxWidth / pitchX;
                if (dimension != boxHeight / pitchY || dimension < 21 || dimension > 177 || (dimension - 17) % 4 != 0) continue;
                var duplicate = false;
                for (var i = 0; i < tried; i += 2) {
                    if (triedPitches[i] == pitchX && triedPitches[i + 1] == pitchY) duplicate = true;
                }
                if (duplicate) continue;
                triedPitches[tried++] = pitchX;
                triedPitches[tried++] = pitchY;
                if (!HasExactGridFinders(image, left, top, dimension, pitchX, pitchY, darkLevel, budget)) continue;
                if (!TrySampleExactGrid(image, left, top, dimension, pitchX, pitchY, darkLevel, budget, out var matrix)) continue;

                if (TryDecodeWithInversion(matrix, accept, budget, out result, out var moduleDiagnostics)
                    || TryDecodeByRotations(matrix, accept, budget, out result, out moduleDiagnostics)) {
                    diagnostics = new QrPixelDecodeDiagnostics(scale, image.Threshold, polarity != 0,
                        candidateCount: 3, candidateTriplesTried: 1, dimension, moduleDiagnostics);
                    return true;
                }
            }
        }
        return false;
    }

    private static int ExactGridBorderRun(QrGrayImage image, int x, int y, int dx, int dy, int length,
        byte darkLevel, DecodeBudget budget) {
        var run = 0;
        for (; run < length; run++, x += dx, y += dy) {
            if ((run & 255) == 0 && budget.IsExpired) return 0;
            if (image.Gray[y * image.Width + x] != darkLevel) break;
        }
        return run;
    }

    private static bool HasExactGridFinders(QrGrayImage image, int left, int top, int dimension,
        int pitchX, int pitchY, byte darkLevel, DecodeBudget budget) {
        var finders = 0;
        // A quarter turn moves the missing finder to any corner. Require three
        // recognizable patterns before allocating or decoding a full grid.
        for (var corner = 0; corner < 4; corner++) {
            var originX = (corner & 1) == 0 ? 0 : dimension - 7;
            var originY = (corner & 2) == 0 ? 0 : dimension - 7;
            var errors = 0;
            for (var y = 0; y < 7 && errors <= 3; y++) {
                if (budget.IsExpired) return false;
                var row = (top + (originY + y) * pitchY + pitchY / 2) * image.Width;
                for (var x = 0; x < 7; x++) {
                    var dark = image.Gray[row + left + (originX + x) * pitchX + pitchX / 2] == darkLevel;
                    var expected = Math.Max(Math.Abs(x - 3), Math.Abs(y - 3)) != 2;
                    if (dark != expected) errors++;
                }
            }
            if (errors <= 3) finders++;
        }
        return finders >= 3;
    }

    private static bool TrySampleExactGrid(QrGrayImage image, int left, int top, int dimension,
        int pitchX, int pitchY, byte darkLevel, DecodeBudget budget, out BitMatrix matrix) {
        matrix = new BitMatrix(dimension, dimension);
        for (var my = 0; my < dimension; my++) {
            for (var mx = 0; mx < dimension; mx++) {
                if ((mx & 31) == 0 && budget.IsExpired) return false;
                var x = left + mx * pitchX;
                var y = top + my * pitchY;
                var level = image.Gray[y * image.Width + x];
                // A valid pitch partitions the entire symbol into uniform pixel
                // blocks. This rejects a corner fit that aliases a finer grid.
                for (var py = 0; py < pitchY; py++) {
                    if (budget.IsExpired) return false;
                    var row = (y + py) * image.Width + x;
                    for (var px = 0; px < pitchX; px++) {
                        if ((px & 255) == 0 && budget.IsExpired) return false;
                        if (image.Gray[row + px] != level) return false;
                    }
                }
                matrix[mx, my] = level == darkLevel;
            }
        }
        return true;
    }
}
#endif
