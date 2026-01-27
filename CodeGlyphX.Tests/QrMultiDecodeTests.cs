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

    private static void Blit(byte[] src, int srcW, int srcH, int srcStride, byte[] dest, int destW, int destH, int destStride, int offsetX, int offsetY) {
        for (var y = 0; y < srcH; y++) {
            var dy = offsetY + y;
            if ((uint)dy >= (uint)destH) break;
            Buffer.BlockCopy(src, y * srcStride, dest, dy * destStride + offsetX * 4, srcW * 4);
        }
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
