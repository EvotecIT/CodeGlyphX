using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Helpers for building simple logo pixels.
/// </summary>
public static class LogoBuilder {
    /// <summary>
    /// Creates a circular RGBA logo with an optional accent center.
    /// </summary>
    /// <param name="size">Square size in pixels.</param>
    /// <param name="color">Outer circle color.</param>
    /// <param name="accent">Inner circle color.</param>
    /// <param name="width">Logo width in pixels.</param>
    /// <param name="height">Logo height in pixels.</param>
    /// <returns>RGBA pixel buffer.</returns>
    public static byte[] CreateCircleRgba(int size, Rgba32 color, Rgba32 accent, out int width, out int height) {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        width = size;
        height = size;
        var rgba = new byte[size * size * 4];
        var cx = (size - 1) / 2.0;
        var radius = size * 0.45;
        var accentRadius = size * 0.25;
        var r2 = radius * radius;
        var a2 = accentRadius * accentRadius;

        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                var dx = x - cx;
                var dy = y - cx;
                var dist2 = dx * dx + dy * dy;
                if (dist2 > r2) continue;

                var c = dist2 <= a2 ? accent : color;
                var p = (y * size + x) * 4;
                rgba[p + 0] = c.R;
                rgba[p + 1] = c.G;
                rgba[p + 2] = c.B;
                rgba[p + 3] = c.A;
            }
        }

        return rgba;
    }

    /// <summary>
    /// Creates a circular PNG logo with an optional accent center.
    /// </summary>
    /// <param name="size">Square size in pixels.</param>
    /// <param name="color">Outer circle color.</param>
    /// <param name="accent">Inner circle color.</param>
    /// <param name="width">Logo width in pixels.</param>
    /// <param name="height">Logo height in pixels.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] CreateCirclePng(int size, Rgba32 color, Rgba32 accent, out int width, out int height) {
        var rgba = CreateCircleRgba(size, color, accent, out width, out height);
        return EncodePng(rgba, width, height);
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height) {
        var stride = width * 4;
        var scanlines = new byte[height * (stride + 1)];
        for (var y = 0; y < height; y++) {
            var rowStart = y * (stride + 1);
            scanlines[rowStart] = 0;
            Buffer.BlockCopy(rgba, y * stride, scanlines, rowStart + 1, stride);
        }
        return PngWriter.WriteRgba8(width, height, scanlines);
    }
}
