using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ico;

internal static class IcoPngBuilder {
    public static byte[] FromRgba(byte[] rgba, int width, int height, int stride) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var scanlineStride = width * 4;
        var scanlines = new byte[height * (scanlineStride + 1)];
        for (var y = 0; y < height; y++) {
            var dst = y * (scanlineStride + 1);
            scanlines[dst] = 0; // filter: none
            Buffer.BlockCopy(rgba, y * stride, scanlines, dst + 1, scanlineStride);
        }
        return PngWriter.WriteRgba8(width, height, scanlines);
    }

    public static byte[] FromRgbaForIco(byte[] rgba, int width, int height, int stride, Rgba32 background) {
        if (width > 256 || height > 256) {
            var size = 256;
            var scaled = ImageScaler.ResizeToFitNearest(rgba, width, height, stride, size, size, background, true);
            return FromRgba(scaled, size, size, size * 4);
        }
        return FromRgba(rgba, width, height, stride);
    }
}
