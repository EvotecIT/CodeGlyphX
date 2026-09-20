using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>Encodes an existing tightly packed RGBA image without changing its dimensions or pixels.</summary>
public static class PngImageEncoder {
    /// <summary>Encodes four bytes per pixel (red, green, blue, alpha) as a PNG.</summary>
    public static byte[] EncodeRgba32(byte[] pixels, int width, int height) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue / 4 || (long)width * height * 4 != pixels.Length)
            throw new ArgumentException("Expected a tightly packed RGBA image.", nameof(pixels));
        RenderGuards.EnsureOutputPixels(width, height, "PNG image exceeds output limits.");
        var stride = width * 4;
        var length = RenderGuards.EnsureOutputBytes((long)(stride + 1) * height, "PNG image exceeds output limits.");
        var scanlines = new byte[length];
        for (var y = 0; y < height; y++) Buffer.BlockCopy(pixels, y * stride, scanlines, y * (stride + 1) + 1, stride);
        return PngWriter.WriteRgba8(width, height, scanlines, length, 6);
    }
}
