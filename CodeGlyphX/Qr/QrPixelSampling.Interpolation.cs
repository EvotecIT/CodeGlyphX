#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SampleLumaBilinear(QrGrayImage image, double x, double y) =>
        InterpolateBilinear(image.Gray, image.Width, GetBilinearCoordinates(image, x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SampleLumaBilinearUnchecked(QrGrayImage image, double x, double y) =>
        InterpolateBilinear(image.Gray, image.Width, GetUncheckedBilinearCoordinates(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SampleLumaThresholdBilinear(QrGrayImage image, double x, double y, out byte lum, out byte threshold) {
        var coordinates = GetBilinearCoordinates(image, x, y);
        lum = InterpolateBilinear(image.Gray, image.Width, coordinates);
        threshold = InterpolateBilinear(image.ThresholdMap!, image.Width, coordinates);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SampleLumaThresholdBilinearUnchecked(QrGrayImage image, double x, double y, out byte lum, out byte threshold) {
        var coordinates = GetUncheckedBilinearCoordinates(x, y);
        lum = InterpolateBilinear(image.Gray, image.Width, coordinates);
        threshold = InterpolateBilinear(image.ThresholdMap!, image.Width, coordinates);
    }

    // Both intensity planes use the same coordinates and operation order. Keep
    // exact clamping, interpolation and byte rounding in one sampler owner.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte InterpolateBilinear(byte[] plane, int width, BilinearCoordinates coordinates) {
        var l00 = plane[coordinates.Y0 * width + coordinates.X0];
        var l10 = plane[coordinates.Y0 * width + coordinates.X1];
        var l01 = plane[coordinates.Y1 * width + coordinates.X0];
        var l11 = plane[coordinates.Y1 * width + coordinates.X1];
        var l0 = l00 + (l10 - l00) * coordinates.Fx;
        var l1 = l01 + (l11 - l01) * coordinates.Fx;
        var lum = l0 + (l1 - l0) * coordinates.Fy;
        var rounded = (int)(lum + 0.5);
        if (rounded < 0) rounded = 0;
        else if (rounded > 255) rounded = 255;
        return (byte)rounded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BilinearCoordinates GetBilinearCoordinates(QrGrayImage image, double x, double y) {
        var maxX = image.Width - 1;
        var maxY = image.Height - 1;
        int x0;
        int y0;
        int x1;
        int y1;
        double fx;
        double fy;
        if (x >= 0 && x < maxX && y >= 0 && y < maxY) {
            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;
            fx = x - x0;
            fy = y - y0;
        } else {
            if (x < 0) x = 0;
            else if (x > maxX) x = maxX;
            if (y < 0) y = 0;
            else if (y > maxY) y = maxY;
            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;
            if (x1 > maxX) x1 = maxX;
            if (y1 > maxY) y1 = maxY;
            fx = x - x0;
            fy = y - y0;
        }
        return new BilinearCoordinates(x0, y0, x1, y1, fx, fy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BilinearCoordinates GetUncheckedBilinearCoordinates(double x, double y) {
        var x0 = (int)x;
        var y0 = (int)y;
        return new BilinearCoordinates(x0, y0, x0 + 1, y0 + 1, x - x0, y - y0);
    }

    private readonly struct BilinearCoordinates {
        internal readonly int X0;
        internal readonly int Y0;
        internal readonly int X1;
        internal readonly int Y1;
        internal readonly double Fx;
        internal readonly double Fy;

        internal BilinearCoordinates(int x0, int y0, int x1, int y1, double fx, double fy) {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
            Fx = fx;
            Fy = fy;
        }
    }
}
#endif
