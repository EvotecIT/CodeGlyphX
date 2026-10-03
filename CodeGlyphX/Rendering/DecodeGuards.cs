using System;

namespace CodeGlyphX.Rendering;

internal static class DecodeGuards {
    public static void EnsurePayloadWithinLimits(int length, string message) {
        if (length < 0) throw new FormatException(message);
        var maxBytes = ImageReader.EffectiveMaxImageBytes;
        if (maxBytes > 0 && length > maxBytes) {
            throw new FormatException(GuardMessages.ForBytes(message, length, maxBytes));
        }
    }

    public static bool TryEnsurePixelCount(int width, int height, out int pixels) {
        return TryEnsurePixelCount(width, height, ImageReader.EffectiveMaxPixels, out pixels);
    }

    public static bool TryEnsurePixelCount(int width, int height, long maxPixels, out int pixels) {
        pixels = 0;
        if (width <= 0 || height <= 0) return false;
        var total = (long)width * height;
        if (total > int.MaxValue) return false;
        if (maxPixels > 0 && total > maxPixels) return false;
        pixels = (int)total;
        return true;
    }

    public static int EnsurePixelCount(int width, int height, string message) {
        if (TryEnsurePixelCount(width, height, out var pixels)) return pixels;
        var total = (long)width * height;
        throw new FormatException(GuardMessages.ForPixels(message, width, height, total, ImageReader.EffectiveMaxPixels));
    }

    public static int EnsureByteCount(long bytes, string message) {
        if (bytes <= 0 || bytes > int.MaxValue) {
            throw new FormatException(GuardMessages.ForBytes(message, bytes, int.MaxValue));
        }
        EnsureDecodedBytes(bytes, message);
        if (!TryEnsureByteCount(bytes, out var count)) {
            throw new FormatException(GuardMessages.ForBytes(message, bytes, int.MaxValue));
        }
        return count;
    }

    public static bool TryEnsureByteCount(long bytes, out int count) {
        count = 0;
        if (bytes <= 0 || bytes > int.MaxValue) return false;
        var maxBytes = ImageReader.EffectiveMaxDecodedBytes;
        if (maxBytes > 0 && bytes > maxBytes) {
            ImageReader.ReportLimitViolation(new ImageDecodeLimitViolation(ImageDecodeLimitKind.MaxDecodedBytes, maxBytes, bytes));
            return false;
        }
        count = (int)bytes;
        return true;
    }

    public static int EnsureInt32Count(long elements, string message) {
        if (elements <= 0 || elements > int.MaxValue) throw new FormatException(message);
        EnsureDecodedBytes(elements * sizeof(int), message);
        return (int)elements;
    }

    public static bool TryEnsureInt32Count(long elements, out int count) {
        count = 0;
        if (elements <= 0 || elements > int.MaxValue || !TryEnsureDecodedBytes(elements * sizeof(int))) return false;
        count = (int)elements;
        return true;
    }

    public static void EnsureDecodedBytes(long bytes, string message) {
        var limit = ImageReader.EffectiveMaxDecodedBytes;
        if (!TryEnsureDecodedBytes(bytes)) {
            throw new FormatException(GuardMessages.ForBytes(message, bytes, limit));
        }
    }

    public static bool TryEnsureDecodedBytes(long bytes) {
        var limit = ImageReader.EffectiveMaxDecodedBytes;
        if (bytes < 0 || (limit > 0 && bytes > limit)) {
            ImageReader.ReportLimitViolation(new ImageDecodeLimitViolation(ImageDecodeLimitKind.MaxDecodedBytes, limit, bytes));
            return false;
        }
        return true;
    }

    public static byte[] AllocatePixelBuffer(int width, int height, string message) {
        var pixels = EnsurePixelCount(width, height, message);
        return new byte[EnsureByteCount(pixels, message)];
    }

    public static byte[] AllocateRgba32(int width, int height, string message) {
        var pixels = EnsurePixelCount(width, height, message);
        var bytes = EnsureByteCount((long)pixels * 4, message);
        return new byte[bytes];
    }
}
