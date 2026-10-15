#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    // Blind bounding-box fits can propose hundreds of large grids in ordinary
    // scenes. Check their required finder geometry before sampling all data.
    // Two recognizable finders allow the third to be damaged or occluded.
    private static bool HasPlausibleBoundingFinders(QrGrayImage image, int left, int top, int dimension,
        double pitchX, double pitchY, double phase, bool invert, bool useRing, double ringDelta, DecodeBudget budget) {
        var plausible = 0;
        var pitch = (pitchX + pitchY) * 0.5;
        for (var finder = 0; finder < 3; finder++) {
            var originX = finder == 1 ? dimension - 7 : 0;
            var originY = finder == 2 ? dimension - 7 : 0;
            var errors = 0;
            for (var y = 0; y < 7 && errors <= 12; y++) {
                if (budget.IsExpired) return false;
                for (var x = 0; x < 7; x++) {
                    var sx = left + (originX + x + phase) * pitchX;
                    var sy = top + (originY + y + phase) * pitchY;
                    var dark = useRing ? QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, ringDelta)
                        : pitch >= 2 ? QrPixelSampling.SampleModule9Px(image, sx, sy, invert, pitch)
                        : QrPixelSampling.SampleModuleMajority3x3(image, sx, sy, invert);
                    var expected = Math.Max(Math.Abs(x - 3), Math.Abs(y - 3)) != 2;
                    if (dark != expected) errors++;
                }
            }
            if (errors <= 12) plausible++;
        }
        return plausible >= 2;
    }
}
#endif
