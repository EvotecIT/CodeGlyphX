using System;
using System.Collections.Generic;
using System.Linq;

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
                    var value = v0 + (v1 - v0) * wy;
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
                pixels[i] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = a;
            }
        }
    }

    public static void Blit(byte[] src, int srcW, int srcH, int srcStride, byte[] dest, int destW, int destH, int destStride, int offsetX, int offsetY) {
        for (var y = 0; y < srcH; y++) {
            var dy = offsetY + y;
            if ((uint)dy >= (uint)destH) break;
            Buffer.BlockCopy(src, y * srcStride, dest, dy * destStride + offsetX * 4, srcW * 4);
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

    private static byte ClampToByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }
}

