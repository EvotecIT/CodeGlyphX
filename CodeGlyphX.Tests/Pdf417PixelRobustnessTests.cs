using System;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417PixelRobustnessTests {
    [Fact]
    public void Pdf417_Decode_WithNoisyBorder() {
        var matrix = Pdf417Encoder.Encode("NoiseTest");
        var pixels = MatrixPngRenderer.RenderPixels(
            matrix,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var pad = 3;
        var newWidth = width + pad * 2;
        var newHeight = height + pad * 2;
        var newStride = newWidth * 4;
        var expanded = new byte[newHeight * newStride];
        for (var i = 0; i < expanded.Length; i += 4) {
            expanded[i] = 255;
            expanded[i + 1] = 255;
            expanded[i + 2] = 255;
            expanded[i + 3] = 255;
        }

        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(pixels, y * stride, expanded, (y + pad) * newStride + pad * 4, stride);
        }

        // Inject dark pixels to expand the bounding box.
        SetPixel(expanded, newStride, 0, 0);
        SetPixel(expanded, newStride, newWidth - 1, newHeight - 1);

        Assert.True(Pdf417Decoder.TryDecode(expanded, newWidth, newHeight, newStride, PixelFormat.Rgba32, out var text));
        Assert.Equal("NoiseTest", text);
    }

    [Fact]
    public void Pdf417_Decode_LowContrast() {
        var matrix = Pdf417Encoder.Encode("LowContrast");
        var pixels = MatrixPngRenderer.RenderPixels(
            matrix,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        // Shift both dark/light near the high end to break the fixed 128 threshold.
        ApplyLowContrast(pixels, low: 150, high: 170);

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("LowContrast", text);
    }

    [Fact]
    public void Pdf417_Decode_WithSkew() {
        var matrix = Pdf417Encoder.Encode("SkewTest", new Pdf417EncodeOptions { ErrorCorrectionLevel = 4 });
        var pixels = MatrixPngRenderer.RenderPixels(
            matrix,
            new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var pad = 16;
        var shear = width * 0.03;
        var dstWidth = width + pad * 2 + (int)Math.Ceiling(Math.Abs(shear));
        var dstHeight = height + pad * 2;
        var dstStride = dstWidth * 4;
        var warped = new byte[dstHeight * dstStride];
        for (var i = 0; i < warped.Length; i += 4) {
            warped[i] = 255;
            warped[i + 1] = 255;
            warped[i + 2] = 255;
            warped[i + 3] = 255;
        }

        var offsetX = (dstWidth - width) / 2;
        var offsetY = pad;
        var mid = height / 2.0;

        for (var y = 0; y < height; y++) {
            var shift = (int)Math.Round((y - mid) / height * shear);
            var dstRow = (y + offsetY) * dstStride;
            var srcRow = y * stride;
            for (var x = 0; x < width; x++) {
                var dstX = x + offsetX + shift;
                if ((uint)dstX >= (uint)dstWidth) continue;
                var src = srcRow + x * 4;
                var dst = dstRow + dstX * 4;
                warped[dst] = pixels[src];
                warped[dst + 1] = pixels[src + 1];
                warped[dst + 2] = pixels[src + 2];
                warped[dst + 3] = pixels[src + 3];
            }
        }

        Assert.True(Pdf417Decoder.TryDecode(warped, dstWidth, dstHeight, dstStride, PixelFormat.Rgba32, out var text));
        Assert.Equal("SkewTest", text);
    }


    private static void SetPixel(byte[] pixels, int stride, int x, int y) {
        var p = y * stride + x * 4;
        pixels[p] = 0;
        pixels[p + 1] = 0;
        pixels[p + 2] = 0;
        pixels[p + 3] = 255;
    }

    private static void ApplyLowContrast(byte[] pixels, byte low, byte high) {
        for (var i = 0; i < pixels.Length; i += 4) {
            var isDark = pixels[i] < 128;
            var value = isDark ? low : high;
            pixels[i] = value;
            pixels[i + 1] = value;
            pixels[i + 2] = value;
            pixels[i + 3] = 255;
        }
    }

}
