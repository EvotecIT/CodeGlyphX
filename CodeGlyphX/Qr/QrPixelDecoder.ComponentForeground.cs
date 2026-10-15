#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Writes zero or one for every logical pixel before component traversal.
    /// Vector lanes keep unsigned threshold equality and polarity; scalar tails
    /// leave unused pooled storage untouched. Budget checks remain at each row.
    /// </summary>
    private static bool TryFillComponentForeground(QrGrayImage image, bool invert, DecodeBudget budget, byte[] foreground) {
        for (var y = 0; y < image.Height; y++) {
            if (budget.IsExpired) return false;
            image.WriteBinaryRow(y, invert, foreground.AsSpan(y * image.Width, image.Width));
        }
        return true;
    }
}
#endif
