using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    // Decorations stay inside the outer one-module ring: they never invade the
    // light separator, replace the dark ball, or enter the quiet zone.
    private static bool StyledFrameRingVisible(QrEyeFrameStyle frame, double x, double y, int cells) {
        // Finder detectors read the horizontal/vertical center cross. Keep these
        // complete so fine decorative grooves cannot introduce extra scan runs.
        if ((x >= 2 && x <= cells - 2) || (y >= 2 && y <= cells - 2)) return true;
        if (Math.Abs(x - y) < 0.65 || Math.Abs(x + y - cells) < 0.65) return true;
        var edge = Math.Min(Math.Min(x, y), Math.Min(cells - x, cells - y));
        var outerGroove = edge is >= 0.20 and < 0.32;
        var innerGroove = edge is >= 0.68 and < 0.80;
        return frame switch {
            QrEyeFrameStyle.DoubleRing => !outerGroove,
            QrEyeFrameStyle.Target => !outerGroove && !innerGroove,
            QrEyeFrameStyle.InsetRing => !innerGroove,
            QrEyeFrameStyle.Bracket => edge >= 0.36
                || (Math.Min(x, cells - x) < 1.25 && Math.Min(y, cells - y) < 1.25),
            _ => true
        };
    }
}
