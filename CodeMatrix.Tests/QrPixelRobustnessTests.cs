using CodeMatrix;
using CodeMatrix.Qr;
using CodeMatrix.Rendering.Png;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrPixelRobustnessTests {
    [Fact]
    public void QrDecode_WithNoisyBorderAndExtraNoise() {
        var code = QrCodeEncoder.EncodeText("NoiseTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var pad = 6;
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

        // Inject dark pixels/squares outside the QR to simulate UI noise.
        FillRect(expanded, newStride, 0, 0, 10, 10);
        FillRect(expanded, newStride, newWidth - 12, newHeight - 12, 12, 12);

        Assert.True(QrDecoder.TryDecode(expanded, newWidth, newHeight, newStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("NoiseTest", decoded.Text);
    }

    [Fact]
    public void QrDecode_WithPerspectiveSkew() {
        var code = QrCodeEncoder.EncodeText("SkewTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 5, QuietZone = 3 },
            out var width,
            out var height,
            out var stride);

        var pad = 30;
        var dstWidth = width + pad * 2;
        var dstHeight = height + pad * 2;
        var dstStride = dstWidth * 4;
        var warped = new byte[dstHeight * dstStride];
        FillWhite(warped);

        var x0 = pad + 6;
        var y0 = pad + 2;
        var x1 = dstWidth - pad - 10;
        var y1 = pad;
        var x2 = dstWidth - pad - 4;
        var y2 = dstHeight - pad - 12;
        var x3 = pad + 12;
        var y3 = dstHeight - pad - 6;

        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            x0, y0,
            x1, y1,
            x2, y2,
            x3, y3,
            0, 0,
            width - 1, 0,
            width - 1, height - 1,
            0, height - 1);

        for (var y = 0; y < dstHeight; y++) {
            var row = y * dstStride;
            for (var x = 0; x < dstWidth; x++) {
                transform.Transform(x, y, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) continue;
                var px = (int)Math.Round(sx);
                var py = (int)Math.Round(sy);
                if ((uint)px >= (uint)width || (uint)py >= (uint)height) continue;

                var src = py * stride + px * 4;
                var dst = row + x * 4;
                warped[dst] = pixels[src];
                warped[dst + 1] = pixels[src + 1];
                warped[dst + 2] = pixels[src + 2];
                warped[dst + 3] = pixels[src + 3];
            }
        }

        Assert.True(QrDecoder.TryDecode(warped, dstWidth, dstHeight, dstStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("SkewTest", decoded.Text);
    }

    private static void FillRect(byte[] pixels, int stride, int x, int y, int w, int h) {
        var maxY = y + h;
        var maxX = x + w;
        for (var yy = y; yy < maxY; yy++) {
            var row = yy * stride;
            for (var xx = x; xx < maxX; xx++) {
                var p = row + xx * 4;
                pixels[p] = 0;
                pixels[p + 1] = 0;
                pixels[p + 2] = 0;
                pixels[p + 3] = 255;
            }
        }
    }

    private static void FillWhite(byte[] pixels) {
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = 255;
            pixels[i + 1] = 255;
            pixels[i + 2] = 255;
            pixels[i + 3] = 255;
        }
    }
}
