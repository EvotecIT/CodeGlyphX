using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrMultiDecodeTests {
    [Fact]
    public void DecodeAll_FindsTwoQrCodes() {
        var left = QrEasy.RenderPixels("LEFT-QR", out var w1, out var h1, out var s1);
        var right = QrEasy.RenderPixels("RIGHT-QR", out var w2, out var h2, out var s2);

        var pad = 12;
        var width = w1 + w2 + pad * 3;
        var height = Math.Max(h1, h2) + pad * 2;
        var stride = width * 4;
        var canvas = new byte[height * stride];

        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        Blit(left, w1, h1, s1, canvas, width, height, stride, pad, pad);
        Blit(right, w2, h2, s2, canvas, width, height, stride, pad * 2 + w1, pad);

        Assert.True(QrDecoder.TryDecodeAll(canvas, width, height, stride, PixelFormat.Rgba32, out var results));
        Assert.Contains(results, r => r.Text == "LEFT-QR");
        Assert.Contains(results, r => r.Text == "RIGHT-QR");
    }

    [Fact]
    public void DecodeAll_FindsEightQrCodes_FromCompositeImage() {
        var payloads = Enumerable.Range(1, 8).Select(i => $"QR-{i}").ToArray();
        var renderOptions = new QrEasyOptions {
            ModuleSize = 20,
            QuietZone = 4,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H
        };
        var grid = 4;
        var pad = 40;
        var canvas = BuildCompositeCanvas(payloads, renderOptions, grid, pad, out var widthPx, out var heightPx, out var stridePx);

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            BudgetMilliseconds = 8000,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = grid
        };

        var png = EncodePng(canvas, widthPx, heightPx, stridePx);
        Assert.True(QrImageDecoder.TryDecodeAllImage(png, options, out var decoded), "Failed to decode composite PNG bytes.");

        var texts = decoded.Select(result => result.Text).ToHashSet(StringComparer.Ordinal);
        var missing = payloads.Where(payload => !texts.Contains(payload, StringComparer.Ordinal)).ToArray();
        Assert.True(missing.Length == 0, $"Missing payloads: {string.Join(", ", missing)}. Decoded: {string.Join(", ", texts)}");
        Assert.Equal(payloads.Length, texts.Count);
    }

    [Fact]
    public void DecodeAll_FindsEightQrCodes_FromCompositeJpeg() {
        var payloads = Enumerable.Range(1, 8).Select(i => $"QR-{i}").ToArray();
        var renderOptions = new QrEasyOptions {
            ModuleSize = 16,
            QuietZone = 4,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H
        };
        var grid = 4;
        var pad = 32;
        var canvas = BuildCompositeCanvas(payloads, renderOptions, grid, pad, out var widthPx, out var heightPx, out var stridePx);
        var jpeg = JpegWriter.WriteRgba(widthPx, heightPx, canvas, stridePx, quality: 90);

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2800,
            BudgetMilliseconds = 8000,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = grid
        };

        Assert.True(QrImageDecoder.TryDecodeAllImage(jpeg, options, out var decoded), "Failed to decode composite JPEG bytes.");

        var texts = decoded.Select(result => result.Text).ToHashSet(StringComparer.Ordinal);
        var missing = payloads.Where(payload => !texts.Contains(payload, StringComparer.Ordinal)).ToArray();
        Assert.True(missing.Length == 0, $"Missing payloads: {string.Join(", ", missing)}. Decoded: {string.Join(", ", texts)}");
        Assert.Equal(payloads.Length, texts.Count);
    }

    [Fact]
    public void DecodeAll_FindsEightQrCodes_FromScreenshotLikeJpeg() {
        var payloads = Enumerable.Range(1, 8).Select(i => $"SHOT-{i}").ToArray();
        var renderOptions = new QrEasyOptions {
            ModuleSize = 14,
            QuietZone = 4,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H
        };

        var grid = 4;
        var pad = 28;
        var canvas = BuildCompositeCanvas(payloads, renderOptions, grid, pad, out var widthPx, out var heightPx, out var stridePx);

        // Simulate UI capture/resampling + light blur/noise before JPEG compression.
        canvas = ResizeNearest(canvas, widthPx, heightPx, stridePx, (int)(widthPx * 0.68), (int)(heightPx * 0.68), out widthPx, out heightPx, out stridePx);
        canvas = ResizeNearest(canvas, widthPx, heightPx, stridePx, widthPx + 180, heightPx + 120, out widthPx, out heightPx, out stridePx);
        ApplyBoxBlur(canvas, widthPx, heightPx, stridePx, radius: 1);
        ApplyDeterministicNoise(canvas, widthPx, heightPx, stridePx, amplitude: 8, seed: 4242);

        var jpeg = JpegWriter.WriteRgba(widthPx, heightPx, canvas, stridePx, quality: 82);
        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3600,
            BudgetMilliseconds = 18000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 4
        };

        Assert.True(QrImageDecoder.TryDecodeAllImage(jpeg, options, out var decoded), "Failed to decode screenshot-like JPEG bytes.");

        var texts = decoded.Select(result => result.Text).ToHashSet(StringComparer.Ordinal);
        var missing = payloads.Where(payload => !texts.Contains(payload, StringComparer.Ordinal)).ToArray();
        Assert.True(missing.Length == 0, $"Missing payloads: {string.Join(", ", missing)}. Decoded: {string.Join(", ", texts)}");
        Assert.Equal(payloads.Length, texts.Count);
    }

    private static void Blit(byte[] src, int srcW, int srcH, int srcStride, byte[] dest, int destW, int destH, int destStride, int offsetX, int offsetY) {
        for (var y = 0; y < srcH; y++) {
            var dy = offsetY + y;
            if ((uint)dy >= (uint)destH) break;
            Buffer.BlockCopy(src, y * srcStride, dest, dy * destStride + offsetX * 4, srcW * 4);
        }
    }

    private static byte[] ResizeNearest(byte[] src, int srcW, int srcH, int srcStride, int dstW, int dstH, out int outW, out int outH, out int outStride) {
        outW = Math.Max(1, dstW);
        outH = Math.Max(1, dstH);
        outStride = outW * 4;
        var dst = new byte[outH * outStride];

        for (var y = 0; y < outH; y++) {
            var sy = (int)((long)y * srcH / outH);
            var srcRow = sy * srcStride;
            var dstRow = y * outStride;
            for (var x = 0; x < outW; x++) {
                var sx = (int)((long)x * srcW / outW);
                var si = srcRow + sx * 4;
                var di = dstRow + x * 4;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
        }

        return dst;
    }

    private static void ApplyBoxBlur(byte[] pixels, int width, int height, int stride, int radius) {
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

    private static void ApplyDeterministicNoise(byte[] pixels, int width, int height, int stride, int amplitude, int seed) {
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

    private static byte[] BuildCompositeCanvas(
        string[] payloads,
        QrEasyOptions renderOptions,
        int grid,
        int pad,
        out int widthPx,
        out int heightPx,
        out int stridePx) {
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

        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        for (var i = 0; i < tiles.Count; i++) {
            var row = i / grid;
            var col = i % grid;
            var offsetX = col * cellWidth + pad;
            var offsetY = row * cellHeight + pad;
            var (pixels, w, h, s) = tiles[i];
            Blit(pixels, w, h, s, canvas, widthPx, heightPx, stridePx, offsetX, offsetY);
        }

        return canvas;
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height, int stride) {
        var rowLength = stride + 1;
        var scanlines = new byte[height * rowLength];
        for (var y = 0; y < height; y++) {
            var rowStart = y * rowLength;
            scanlines[rowStart] = 0;
            Buffer.BlockCopy(rgba, y * stride, scanlines, rowStart + 1, stride);
        }
        return PngWriter.WriteRgba8(width, height, scanlines, scanlines.Length);
    }
}
