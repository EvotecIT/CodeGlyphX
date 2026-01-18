using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

internal static class ImageScaler {
    public static byte[] ResizeToFitNearest(ReadOnlySpan<byte> rgba, int srcWidth, int srcHeight, int srcStride, int dstWidth, int dstHeight, Rgba32 background, bool preserveAspectRatio) {
        if (srcWidth <= 0) throw new ArgumentOutOfRangeException(nameof(srcWidth));
        if (srcHeight <= 0) throw new ArgumentOutOfRangeException(nameof(srcHeight));
        if (dstWidth <= 0) throw new ArgumentOutOfRangeException(nameof(dstWidth));
        if (dstHeight <= 0) throw new ArgumentOutOfRangeException(nameof(dstHeight));
        if (srcStride < srcWidth * 4) throw new ArgumentOutOfRangeException(nameof(srcStride));
        if (rgba.Length < (srcHeight - 1) * srcStride + srcWidth * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var dest = new byte[dstWidth * dstHeight * 4];
        Fill(dest, dstWidth, dstHeight, background);

        var targetWidth = dstWidth;
        var targetHeight = dstHeight;
        if (preserveAspectRatio) {
            var scale = Math.Min(dstWidth / (double)srcWidth, dstHeight / (double)srcHeight);
            targetWidth = Math.Max(1, (int)Math.Round(srcWidth * scale));
            targetHeight = Math.Max(1, (int)Math.Round(srcHeight * scale));
        }

        var offsetX = (dstWidth - targetWidth) / 2;
        var offsetY = (dstHeight - targetHeight) / 2;
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;

        for (var y = 0; y < targetHeight; y++) {
            var srcY = (int)((y + 0.5) * srcHeight / targetHeight);
            if (srcY >= srcHeight) srcY = srcHeight - 1;
            var srcRow = srcY * srcStride;
            var dstRow = (y + offsetY) * dstWidth * 4;
            for (var x = 0; x < targetWidth; x++) {
                var srcX = (int)((x + 0.5) * srcWidth / targetWidth);
                if (srcX >= srcWidth) srcX = srcWidth - 1;
                var srcIndex = srcRow + srcX * 4;
                var dstIndex = dstRow + (x + offsetX) * 4;
                dest[dstIndex + 0] = rgba[srcIndex + 0];
                dest[dstIndex + 1] = rgba[srcIndex + 1];
                dest[dstIndex + 2] = rgba[srcIndex + 2];
                dest[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        return dest;
    }

    private static void Fill(byte[] dest, int width, int height, Rgba32 background) {
        for (var y = 0; y < height; y++) {
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                dest[p + 0] = background.R;
                dest[p + 1] = background.G;
                dest[p + 2] = background.B;
                dest[p + 3] = background.A;
            }
        }
    }
}
