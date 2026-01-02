#if NET8_0_OR_GREATER
namespace CodeMatrix.Qr;

internal static class QrMath {
    public static int RoundToInt(double value) {
        // Avoid banker's rounding (Math.Round default) - QR sampling works better with half-up.
        return value >= 0 ? (int)(value + 0.5) : (int)(value - 0.5);
    }
}
#endif

