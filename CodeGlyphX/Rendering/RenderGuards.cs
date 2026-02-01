using System;

namespace CodeGlyphX.Rendering;

internal static class RenderGuards {
    public const long DefaultMaxOutputPixels = ImageReader.DefaultMaxPixels;
    public const int DefaultMaxOutputBytes = ImageReader.DefaultMaxImageBytes;

    public static long MaxOutputPixels { get; set; } = DefaultMaxOutputPixels;
    public static int MaxOutputBytes { get; set; } = DefaultMaxOutputBytes;

    public static int EnsureOutputPixels(int width, int height, string message) {
        if (!DecodeGuards.TryEnsurePixelCount(width, height, MaxOutputPixels, out var pixels)) {
            throw new ArgumentException(message);
        }
        return pixels;
    }

    public static int EnsureOutputBytes(long bytes, string message) {
        if (bytes <= 0 || bytes > int.MaxValue) throw new ArgumentException(message);
        var maxBytes = MaxOutputBytes;
        if (maxBytes > 0 && bytes > maxBytes) throw new ArgumentException(message);
        return (int)bytes;
    }
}
