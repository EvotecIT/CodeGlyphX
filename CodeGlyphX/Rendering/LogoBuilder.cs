using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Helpers for building simple logo pixels.
/// </summary>
public static class LogoBuilder {
    /// <summary>Creates the square black-and-white recognition cross used by Swiss QR bills.</summary>
    /// <remarks>The 83-unit design includes its white border. Scale the complete image to 7 mm when the QR area is 46 mm wide.</remarks>
    public static byte[] CreateSwissCrossPng() {
        const int size = 166;
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++) {
                var unitX = x / 2;
                var unitY = y / 2;
                var square = unitX >= 6 && unitX < 77 && unitY >= 6 && unitY < 77;
                var cross = (unitX >= 34 && unitX < 49 && unitY >= 18 && unitY < 65)
                    || (unitX >= 18 && unitX < 65 && unitY >= 34 && unitY < 49);
                var offset = (y * size + x) * 4;
                var color = (byte)(square && !cross ? 0 : 255);
                rgba[offset] = color; rgba[offset + 1] = color; rgba[offset + 2] = color; rgba[offset + 3] = 255;
            }
        return PngImageEncoder.EncodeRgba32(rgba, size, size);
    }
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
