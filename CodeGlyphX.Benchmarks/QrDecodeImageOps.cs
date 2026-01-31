using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal static class QrDecodeImageOps {
    public static byte[] ResampleBilinear(byte[] src, int srcWidth, int srcHeight, int dstWidth, int dstHeight) {
        if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0) {
            throw new ArgumentOutOfRangeException("Invalid resample dimensions.");
        }

        var dst = new byte[checked(dstWidth * dstHeight * 4)];
        var scaleX = srcWidth / (double)dstWidth;
        var scaleY = srcHeight / (double)dstHeight;

        for (var y = 0; y < dstHeight; y++) {
            var sy = (y + 0.5) * scaleY - 0.5;
            if (sy < 0) sy = 0;
            var y0 = (int)sy;
            var y1 = Math.Min(y0 + 1, srcHeight - 1);
            var wy = sy - y0;
            var row0 = y0 * srcWidth * 4;
            var row1 = y1 * srcWidth * 4;

            for (var x = 0; x < dstWidth; x++) {
                var sx = (x + 0.5) * scaleX - 0.5;
                if (sx < 0) sx = 0;
                var x0 = (int)sx;
                var x1 = Math.Min(x0 + 1, srcWidth - 1);
                var wx = sx - x0;

                var idx00 = row0 + x0 * 4;
                var idx10 = row0 + x1 * 4;
                var idx01 = row1 + x0 * 4;
                var idx11 = row1 + x1 * 4;
                var dstIndex = (y * dstWidth + x) * 4;

                for (var c = 0; c < 4; c++) {
                    var v00 = src[idx00 + c];
                    var v10 = src[idx10 + c];
                    var v01 = src[idx01 + c];
                    var v11 = src[idx11 + c];

                    var v0 = v00 + (v10 - v00) * wx;
                    var v1 = v01 + (v11 - v01) * wx;
                    var value = (int)Math.Round(v0 + (v1 - v0) * wy);
                    if (value < 0) value = 0;
                    else if (value > 255) value = 255;
                    dst[dstIndex + c] = (byte)value;
                }
            }
        }

        return dst;
    }

    public static void FillSolid(byte[] pixels, int width, int height, int stride, byte r, byte g, byte b, byte a) {
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + x * 4;
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = a;
            }
        }
    }

    public static void Blit(byte[] src, int srcW, int srcH, int srcStride, byte[] dest, int destW, int destH, int destStride, int offsetX, int offsetY) {
        for (var y = 0; y < srcH; y++) {
            var dy = offsetY + y;
            if (dy < 0) continue;
            if ((uint)dy >= (uint)destH) break;

            var destStartX = offsetX;
            var srcStartX = 0;
            if (destStartX < 0) {
                srcStartX = -destStartX;
                destStartX = 0;
            }
            if ((uint)destStartX >= (uint)destW || srcStartX >= srcW) continue;

            var copyWidth = Math.Min(srcW - srcStartX, destW - destStartX);
            if (copyWidth <= 0) continue;

            var srcOffset = y * srcStride + srcStartX * 4;
            var destOffset = dy * destStride + destStartX * 4;
            Buffer.BlockCopy(src, srcOffset, dest, destOffset, copyWidth * 4);
        }
    }

    public static byte[] BuildCompositeGrid(string[] payloads, QrEasyOptions renderOptions, int grid, int pad, out int widthPx, out int heightPx, out int stridePx) {
        var tiles = new List<(byte[] pixels, int width, int height, int stride)>(payloads.Length);

        for (var i = 0; i < payloads.Length; i++) {
            var pixels = QrEasy.RenderPixels(payloads[i], out var tileW, out var tileH, out var tileStride, renderOptions);
            tiles.Add((pixels, tileW, tileH, tileStride));
        }

        var tileWidth = tiles.Max(tile => tile.width);
        var tileHeight = tiles.Max(tile => tile.height);
        var cellWidth = tileWidth + pad * 2;
        var cellHeight = tileHeight + pad * 2;
        widthPx = grid * cellWidth;
        heightPx = grid * cellHeight;
        stridePx = widthPx * 4;
        var canvas = new byte[heightPx * stridePx];

        FillSolid(canvas, widthPx, heightPx, stridePx, 255, 255, 255, 255);

        for (var i = 0; i < tiles.Count; i++) {
            var row = i / grid;
            var col = i % grid;
            var x0 = col * cellWidth + pad;
            var y0 = row * cellHeight + pad;
            var tile = tiles[i];
            Blit(tile.pixels, tile.width, tile.height, tile.stride, canvas, widthPx, heightPx, stridePx, x0, y0);
        }

        return canvas;
    }

    public static void ApplyBoxBlur(byte[] pixels, int width, int height, int stride, int radius) {
        if (radius <= 0) return;
        var copy = new byte[pixels.Length];
        Buffer.BlockCopy(pixels, 0, copy, 0, pixels.Length);

        for (var y = 0; y < height; y++) {
            var y0 = Math.Max(0, y - radius);
            var y1 = Math.Min(height - 1, y + radius);
            for (var x = 0; x < width; x++) {
                var x0 = Math.Max(0, x - radius);
                var x1 = Math.Min(width - 1, x + radius);
                var sumB = 0;
                var sumG = 0;
                var sumR = 0;
                var sumA = 0;
                var count = 0;
                for (var yy = y0; yy <= y1; yy++) {
                    var row = yy * stride;
                    for (var xx = x0; xx <= x1; xx++) {
                        var i = row + xx * 4;
                        sumB += copy[i];
                        sumG += copy[i + 1];
                        sumR += copy[i + 2];
                        sumA += copy[i + 3];
                        count++;
                    }
                }

                var di = y * stride + x * 4;
                pixels[di] = (byte)(sumB / count);
                pixels[di + 1] = (byte)(sumG / count);
                pixels[di + 2] = (byte)(sumR / count);
                pixels[di + 3] = (byte)(sumA / count);
            }
        }
    }

    public static void ApplyDeterministicNoise(byte[] pixels, int width, int height, int stride, int amplitude, int seed) {
        if (amplitude <= 0) return;
        var rng = new Random(seed);
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + x * 4;
                var delta = rng.Next(-amplitude, amplitude + 1);
                pixels[i] = ClampToByte(pixels[i] + delta);
                pixels[i + 1] = ClampToByte(pixels[i + 1] + delta);
                pixels[i + 2] = ClampToByte(pixels[i + 2] + delta);
            }
        }
    }

    public static void ApplyMotionBlurHorizontal(byte[] pixels, int width, int height, int stride, int radius) {
        if (radius <= 0 || width <= 1) return;
        var copy = new byte[pixels.Length];
        Buffer.BlockCopy(pixels, 0, copy, 0, pixels.Length);

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var x0 = Math.Max(0, x - radius);
                var x1 = Math.Min(width - 1, x + radius);
                var sumR = 0;
                var sumG = 0;
                var sumB = 0;
                var sumA = 0;
                var count = 0;
                for (var xx = x0; xx <= x1; xx++) {
                    var i = row + xx * 4;
                    sumR += copy[i];
                    sumG += copy[i + 1];
                    sumB += copy[i + 2];
                    sumA += copy[i + 3];
                    count++;
                }
                var di = row + x * 4;
                pixels[di] = (byte)(sumR / count);
                pixels[di + 1] = (byte)(sumG / count);
                pixels[di + 2] = (byte)(sumB / count);
                pixels[di + 3] = (byte)(sumA / count);
            }
        }
    }

    public static void ApplyHorizontalShear(byte[] pixels, int width, int height, int stride, int maxShift, byte bgR, byte bgG, byte bgB, byte bgA) {
        if (maxShift == 0 || width <= 0 || height <= 0) return;
        var dest = new byte[pixels.Length];
        FillSolid(dest, width, height, stride, bgR, bgG, bgB, bgA);

        var span = Math.Max(1, height - 1);
        for (var y = 0; y < height; y++) {
            var t = (y / (double)span - 0.5) * 2.0;
            var shift = (int)Math.Round(t * maxShift);
            var srcRow = y * stride;
            var destRow = y * stride;
            var srcStart = 0;
            var destStart = shift;
            if (destStart < 0) {
                srcStart = -destStart;
                destStart = 0;
            }
            if (destStart >= width || srcStart >= width) continue;
            var copyWidth = Math.Min(width - srcStart, width - destStart);
            if (copyWidth <= 0) continue;
            Buffer.BlockCopy(pixels, srcRow + srcStart * 4, dest, destRow + destStart * 4, copyWidth * 4);
        }

        Buffer.BlockCopy(dest, 0, pixels, 0, pixels.Length);
    }

    public static void ApplyContrast(byte[] pixels, int width, int height, int stride, double factor, int bias = 0) {
        if (width <= 0 || height <= 0) return;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + x * 4;
                pixels[i] = ClampToByte((int)Math.Round((pixels[i] - 128) * factor + 128 + bias));
                pixels[i + 1] = ClampToByte((int)Math.Round((pixels[i + 1] - 128) * factor + 128 + bias));
                pixels[i + 2] = ClampToByte((int)Math.Round((pixels[i + 2] - 128) * factor + 128 + bias));
            }
        }
    }

    public static void ApplySaltPepperNoise(byte[] pixels, int width, int height, int stride, double probability, int seed) {
        if (probability <= 0 || width <= 0 || height <= 0) return;
        if (probability > 1.0) probability = 1.0;
        var rng = new Random(seed);
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if (rng.NextDouble() >= probability) continue;
                var value = rng.Next(2) == 0 ? (byte)0 : (byte)255;
                var i = row + x * 4;
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
            }
        }
    }

    public static byte[] RotateBilinear(byte[] src, int width, int height, int stride, double degrees, byte bgR, byte bgG, byte bgB, byte bgA) {
        var dst = new byte[checked(width * height * 4)];
        FillSolid(dst, width, height, width * 4, bgR, bgG, bgB, bgA);

        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var cx = (width - 1) / 2.0;
        var cy = (height - 1) / 2.0;

        for (var y = 0; y < height; y++) {
            var dy = y - cy;
            for (var x = 0; x < width; x++) {
                var dx = x - cx;
                var srcX = cos * dx + sin * dy + cx;
                var srcY = -sin * dx + cos * dy + cy;

                if (srcX < 0 || srcY < 0 || srcX >= width - 1 || srcY >= height - 1) continue;

                var x0 = (int)srcX;
                var y0 = (int)srcY;
                var x1 = x0 + 1;
                var y1 = y0 + 1;
                var wx = srcX - x0;
                var wy = srcY - y0;

                var idx00 = y0 * stride + x0 * 4;
                var idx10 = y0 * stride + x1 * 4;
                var idx01 = y1 * stride + x0 * 4;
                var idx11 = y1 * stride + x1 * 4;
                var di = (y * width + x) * 4;

                for (var c = 0; c < 4; c++) {
                    var v00 = src[idx00 + c];
                    var v10 = src[idx10 + c];
                    var v01 = src[idx01 + c];
                    var v11 = src[idx11 + c];
                    var v0 = v00 + (v10 - v00) * wx;
                    var v1 = v01 + (v11 - v01) * wx;
                    dst[di + c] = ClampToByte((int)Math.Round(v0 + (v1 - v0) * wy));
                }
            }
        }

        return dst;
    }

    public static byte[] Crop(byte[] src, int width, int height, int stride, int left, int top, int right, int bottom) {
        if (left < 0 || top < 0 || right < 0 || bottom < 0) throw new ArgumentOutOfRangeException();
        var newWidth = Math.Max(1, width - left - right);
        var newHeight = Math.Max(1, height - top - bottom);
        var dst = new byte[checked(newWidth * newHeight * 4)];
        var dstStride = newWidth * 4;

        for (var y = 0; y < newHeight; y++) {
            var srcRow = (top + y) * stride + left * 4;
            var dstRow = y * dstStride;
            Buffer.BlockCopy(src, srcRow, dst, dstRow, newWidth * 4);
        }

        return dst;
    }

    public static byte[] ApplyVerticalKeystone(byte[] src, int width, int height, int stride, double topScale, double bottomScale, byte bgR, byte bgG, byte bgB, byte bgA) {
        if (width <= 0 || height <= 0) return Array.Empty<byte>();
        var dst = new byte[checked(width * height * 4)];
        FillSolid(dst, width, height, width * 4, bgR, bgG, bgB, bgA);

        topScale = Math.Max(0.1, topScale);
        bottomScale = Math.Max(0.1, bottomScale);
        var span = Math.Max(1, height - 1);

        for (var y = 0; y < height; y++) {
            var t = y / (double)span;
            var scale = topScale + (bottomScale - topScale) * t;
            if (scale <= 0) continue;
            var rowWidth = (int)Math.Round(width * scale);
            if (rowWidth < 1) rowWidth = 1;
            if (rowWidth > width) rowWidth = width;
            var offsetX = (width - rowWidth) / 2;
            var srcScale = width / (double)rowWidth;
            var srcRow = y * stride;
            var dstRow = y * width * 4;

            for (var dx = 0; dx < rowWidth; dx++) {
                var srcX = (dx + 0.5) * srcScale - 0.5;
                if (srcX < 0) srcX = 0;
                if (srcX > width - 1) srcX = width - 1;
                var x0 = (int)srcX;
                var x1 = Math.Min(x0 + 1, width - 1);
                var wx = srcX - x0;
                var idx0 = srcRow + x0 * 4;
                var idx1 = srcRow + x1 * 4;
                var di = dstRow + (offsetX + dx) * 4;
                for (var c = 0; c < 4; c++) {
                    var v0 = src[idx0 + c];
                    var v1 = src[idx1 + c];
                    dst[di + c] = ClampToByte((int)Math.Round(v0 + (v1 - v0) * wx));
                }
            }
        }

        return dst;
    }

    public static void ApplyLinearGradientOverlay(byte[] pixels, int width, int height, int stride, byte r, byte g, byte b, byte alphaStart, byte alphaEnd) {
        if (width <= 0 || height <= 0) return;
        var span = Math.Max(1, height - 1);
        for (var y = 0; y < height; y++) {
            var t = y / (double)span;
            var alpha = (int)Math.Round(alphaStart + (alphaEnd - alphaStart) * t);
            if (alpha <= 0) continue;
            if (alpha > 255) alpha = 255;
            var inv = 255 - alpha;
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + x * 4;
                pixels[i] = (byte)((pixels[i] * inv + r * alpha) / 255);
                pixels[i + 1] = (byte)((pixels[i + 1] * inv + g * alpha) / 255);
                pixels[i + 2] = (byte)((pixels[i + 2] * inv + b * alpha) / 255);
            }
        }
    }

    private static byte ClampToByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }
}
