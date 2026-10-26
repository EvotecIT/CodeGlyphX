#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrFinderPatternDetector {
    // A circular three-module ball in a square aperture has a shorter diagonal
    // than a square ball. This check is reached only after both orthogonal
    // 1:1:3:1:1 cross-checks and the diagonal total-length bound have passed.
    private static bool CircularBallDiagonal(int s0, int s1, int s2, int s3, int s4, int orthogonalTotal) {
        var module = orthogonalTotal / 7.0;
        if (module < 1 || s0 == 0 || s1 == 0 || s2 == 0 || s3 == 0 || s4 == 0) return false;
        const double rootHalf = .7071067811865476;
        var ring = module * rootHalf;
        var ball = module * 3 * rootHalf;
        var gap = (module * 5 - ball) / 2;
        return Math.Abs(s0 - ring) <= module * .28 && Math.Abs(s4 - ring) <= module * .28
            && Math.Abs(s1 - gap) <= module * .32 && Math.Abs(s3 - gap) <= module * .32
            && Math.Abs(s2 - ball) <= module * .4
            && Math.Abs(s0 - s4) <= module * .35 && Math.Abs(s1 - s3) <= module * .35;
    }

    // The complementary geometry: a square three-module ball inside a circular
    // aperture. Mirrored leaf rings have this diagonal through their rounded pair.
    private static bool SquareBallDiagonal(int s0, int s1, int s2, int s3, int s4, int orthogonalTotal) {
        var module = orthogonalTotal / 7.0;
        if (module < 1 || s0 == 0 || s1 == 0 || s2 == 0 || s3 == 0 || s4 == 0) return false;
        var ring = module * .7071067811865476;
        var gap = (module * 5 * .7071067811865476 - module * 3) / 2;
        return Math.Abs(s0 - ring) <= module * .25 && Math.Abs(s4 - ring) <= module * .25
            && Math.Abs(s1 - gap) <= module * .22 && Math.Abs(s3 - gap) <= module * .22
            && Math.Abs(s2 - module * 3) <= module * .4
            && Math.Abs(s0 - s4) <= module * .35 && Math.Abs(s1 - s3) <= module * .3;
    }
}
#endif
