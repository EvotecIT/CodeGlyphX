using System;
using System.Threading;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

internal static class ImageScaler {
    public static byte[] ResizeToFitNearest(ReadOnlySpan<byte> rgba, int srcWidth, int srcHeight, int srcStride, int dstWidth, int dstHeight, Rgba32 background, bool preserveAspectRatio, CancellationToken cancellationToken = default) {
        if (srcWidth <= 0) throw new ArgumentOutOfRangeException(nameof(srcWidth));
        if (srcHeight <= 0) throw new ArgumentOutOfRangeException(nameof(srcHeight));
        if (dstWidth <= 0) throw new ArgumentOutOfRangeException(nameof(dstWidth));
        if (dstHeight <= 0) throw new ArgumentOutOfRangeException(nameof(dstHeight));
        if (srcStride < srcWidth * 4) throw new ArgumentOutOfRangeException(nameof(srcStride));
        if (rgba.Length < (srcHeight - 1) * srcStride + srcWidth * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        cancellationToken.ThrowIfCancellationRequested();
        var dest = new byte[dstWidth * dstHeight * 4];
        Fill(dest, dstWidth, dstHeight, background, cancellationToken);

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
            cancellationToken.ThrowIfCancellationRequested();
            var srcY = (int)((y + 0.5) * srcHeight / targetHeight);
            if (srcY >= srcHeight) srcY = srcHeight - 1;
            var srcRow = srcY * srcStride;
            var dstRow = (y + offsetY) * dstWidth * 4;
            for (var x = 0; x < targetWidth; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
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

    public static byte[] ResizeToFitBox(ReadOnlySpan<byte> rgba, int srcWidth, int srcHeight, int srcStride, int dstWidth, int dstHeight, Rgba32 background, bool preserveAspectRatio, CancellationToken cancellationToken = default) {
        if (srcWidth <= 0) throw new ArgumentOutOfRangeException(nameof(srcWidth));
        if (srcHeight <= 0) throw new ArgumentOutOfRangeException(nameof(srcHeight));
        if (dstWidth <= 0) throw new ArgumentOutOfRangeException(nameof(dstWidth));
        if (dstHeight <= 0) throw new ArgumentOutOfRangeException(nameof(dstHeight));
        if (srcStride < srcWidth * 4) throw new ArgumentOutOfRangeException(nameof(srcStride));
        if (rgba.Length < (srcHeight - 1) * srcStride + srcWidth * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        cancellationToken.ThrowIfCancellationRequested();
        var dest = new byte[dstWidth * dstHeight * 4];
        Fill(dest, dstWidth, dstHeight, background, cancellationToken);

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

        var scaleX = srcWidth / (double)targetWidth;
        var scaleY = srcHeight / (double)targetHeight;

        for (var y = 0; y < targetHeight; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var srcY0 = (int)Math.Floor(y * scaleY);
            var srcY1 = (int)Math.Floor((y + 1) * scaleY);
            if (srcY1 <= srcY0) srcY1 = srcY0 + 1;
            if (srcY1 > srcHeight) srcY1 = srcHeight;

            var dstRow = (y + offsetY) * dstWidth * 4;
            for (var x = 0; x < targetWidth; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var srcX0 = (int)Math.Floor(x * scaleX);
                var srcX1 = (int)Math.Floor((x + 1) * scaleX);
                if (srcX1 <= srcX0) srcX1 = srcX0 + 1;
                if (srcX1 > srcWidth) srcX1 = srcWidth;

                long sumR = 0;
                long sumG = 0;
                long sumB = 0;
                long sumA = 0;
                var count = 0;

                for (var sy = srcY0; sy < srcY1; sy++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var srcRow = sy * srcStride + srcX0 * 4;
                    for (var sx = srcX0; sx < srcX1; sx++) {
                        if ((sx & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                        sumR += rgba[srcRow + 0];
                        sumG += rgba[srcRow + 1];
                        sumB += rgba[srcRow + 2];
                        sumA += rgba[srcRow + 3];
                        srcRow += 4;
                        count++;
                    }
                }

                if (count <= 0) continue;
                var dstIndex = dstRow + (x + offsetX) * 4;
                dest[dstIndex + 0] = (byte)((sumR + (count / 2)) / count);
                dest[dstIndex + 1] = (byte)((sumG + (count / 2)) / count);
                dest[dstIndex + 2] = (byte)((sumB + (count / 2)) / count);
                dest[dstIndex + 3] = (byte)((sumA + (count / 2)) / count);
            }
        }

        return dest;
    }

    private static void Fill(byte[] dest, int width, int height, Rgba32 background, CancellationToken cancellationToken) {
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var p = row + x * 4;
                dest[p + 0] = background.R;
                dest[p + 1] = background.G;
                dest[p + 2] = background.B;
                dest[p + 3] = background.A;
            }
        }
    }
}
