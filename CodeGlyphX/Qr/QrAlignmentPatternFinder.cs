#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static class QrAlignmentPatternFinder {
    public static bool TryFind(
        QrGrayImage image,
        bool invert,
        double predictedX,
        double predictedY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        double moduleSize,
        out double centerX,
        out double centerY) {
        centerX = 0;
        centerY = 0;

        var r = (int)Math.Round(moduleSize * 4.0);
        if (r < 6) r = 6;
        if (r > 48) r = 48;

        var x0 = (int)Math.Round(predictedX) - r;
        var y0 = (int)Math.Round(predictedY) - r;
        var x1 = (int)Math.Round(predictedX) + r;
        var y1 = (int)Math.Round(predictedY) + r;

        if (x1 < 0 || y1 < 0 || x0 >= image.Width || y0 >= image.Height) return false;

        x0 = Math.Max(0, x0);
        y0 = Math.Max(0, y0);
        x1 = Math.Min(image.Width - 1, x1);
        y1 = Math.Min(image.Height - 1, y1);

        var bestScore = -1;
        var bestX = 0.0;
        var bestY = 0.0;

        for (var y = y0; y <= y1; y++) {
            for (var x = x0; x <= x1; x++) {
                var score = ScoreAt(image, invert, x, y, vxX, vxY, vyX, vyY);
                if (score > bestScore) {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                    if (bestScore == 25) break;
                }
            }

            if (bestScore == 25) break;
        }

        // Accept a near-perfect match; allow a few errors for anti-aliasing.
        if (bestScore < 22) return false;

        centerX = bestX;
        centerY = bestY;
        return true;
    }

    private static int ScoreAt(
        QrGrayImage image,
        bool invert,
        double centerX,
        double centerY,
        double vxX,
        double vxY,
        double vyX,
        double vyY) {
        var score = 0;

        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                var expectedBlack = Math.Abs(dx) == 2 || Math.Abs(dy) == 2 || (dx == 0 && dy == 0);

                var sx = centerX + vxX * dx + vyX * dy;
                var sy = centerY + vxY * dx + vyY * dy;

                var isBlack = QrPixelSampling.IsBlackBilinear(image, sx, sy, invert);
                if (isBlack == expectedBlack) score++;
            }
        }

        return score;
    }
}
#endif

