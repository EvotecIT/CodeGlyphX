#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
namespace CodeGlyphX.Qr;

internal static class QrMath {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(double value) {
        // Avoid banker's rounding (Math.Round default) - QR sampling works better with half-up.
        return value >= 0 ? (int)(value + 0.5) : (int)(value - 0.5);
    }
}
#endif
