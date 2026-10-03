using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>Encodes an existing tightly packed RGBA image without changing its dimensions or pixels.</summary>
public static class PngImageEncoder {
    /// <summary>Encodes four bytes per pixel (red, green, blue, alpha) as a PNG.</summary>
    public static byte[] EncodeRgba32(byte[] pixels, int width, int height) => EncodeRgba32(pixels, width, height, 6, 0);

    /// <summary>Encodes exact RGBA pixels with lossless compression (0..9) and optional physical
    /// resolution (0 omits metadata; otherwise 1..2400 DPI, rounded to integer pixels per meter).</summary>
    public static byte[] EncodeRgba32(byte[] pixels, int width, int height, int compressionLevel, int dpi = 0) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return EncodeRgba32(pixels.AsSpan(), width, height, compressionLevel, dpi);
    }

    internal static byte[] EncodeRgba32(ReadOnlySpan<byte> pixels, int width, int height, int compressionLevel, int dpi = 0) {
        if (compressionLevel < 0 || compressionLevel > 9) throw new ArgumentOutOfRangeException(nameof(compressionLevel));
        if (dpi < 0 || dpi > 2400) throw new ArgumentOutOfRangeException(nameof(dpi));
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue / 4 || (long)width * height * 4 != pixels.Length)
            throw new ArgumentException("Expected a tightly packed RGBA image.", nameof(pixels));
        RenderGuards.EnsureOutputPixels(width, height, "PNG image exceeds output limits.");
        var stride = width * 4;
        var length = RenderGuards.EnsureOutputBytes((long)(stride + 1) * height, "PNG image exceeds output limits.");
        var scanlines = new byte[length];
        for (var y = 0; y < height; y++) pixels.Slice(y * stride, stride).CopyTo(scanlines.AsSpan(y * (stride + 1) + 1, stride));
        return PngWriter.WriteRgba8(width, height, scanlines, length, compressionLevel, dpi);
    }
}
