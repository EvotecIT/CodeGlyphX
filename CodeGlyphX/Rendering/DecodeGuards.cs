using System;

namespace CodeGlyphX.Rendering;

internal static class DecodeGuards {
    public static void EnsurePayloadWithinLimits(int length, string message) {
        if (length < 0) throw new FormatException(message);
        var maxBytes = ImageReader.MaxImageBytes;
        if (maxBytes > 0 && length > maxBytes) throw new FormatException(message);
    }

    public static bool TryEnsurePixelCount(int width, int height, out int pixels) {
        pixels = 0;
        if (width <= 0 || height <= 0) return false;
        var total = (long)width * height;
        if (total > int.MaxValue) return false;
        var maxPixels = ImageReader.MaxPixels;
        if (maxPixels > 0 && total > maxPixels) return false;
        pixels = (int)total;
        return true;
    }

    public static int EnsurePixelCount(int width, int height, string message) {
        if (!TryEnsurePixelCount(width, height, out var pixels)) {
            throw new FormatException(message);
        }
        return pixels;
    }

    public static int EnsureByteCount(long bytes, string message) {
        if (!TryEnsureByteCount(bytes, out var count)) throw new FormatException(message);
        return count;
    }

    public static bool TryEnsureByteCount(long bytes, out int count) {
        count = 0;
        if (bytes <= 0 || bytes > int.MaxValue) return false;
        count = (int)bytes;
        return true;
    }

    public static byte[] AllocatePixelBuffer(int width, int height, string message) {
        var pixels = EnsurePixelCount(width, height, message);
        return new byte[pixels];
    }

    public static byte[] AllocateRgba32(int width, int height, string message) {
        var pixels = EnsurePixelCount(width, height, message);
        var bytes = EnsureByteCount((long)pixels * 4, message);
        return new byte[bytes];
    }
}
