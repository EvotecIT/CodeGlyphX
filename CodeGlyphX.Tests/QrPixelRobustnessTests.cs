using CodeGlyphX;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

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

    [Fact]
    public void QrDecode_WithInvertedColors() {
        var code = QrCodeEncoder.EncodeText("InvertTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 3 },
            out var width,
            out var height,
            out var stride);

        Invert(pixels);

        Assert.True(QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("InvertTest", decoded.Text);
    }

    [Fact]
    public void QrDecode_WithGradientBackground() {
        var code = QrCodeEncoder.EncodeText("GradientTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 3 },
            out var width,
            out var height,
            out var stride);

        var gradient = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var t = y / (double)Math.Max(1, height - 1);
            var baseVal = (byte)(200 - 80 * t);
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                gradient[p] = baseVal;
                gradient[p + 1] = baseVal;
                gradient[p + 2] = baseVal;
                gradient[p + 3] = 255;
            }
        }

        // Overlay QR: keep gradient for light modules, enforce black for dark modules.
        for (var i = 0; i < pixels.Length; i += 4) {
            var isDark = pixels[i] < 128;
            if (isDark) {
                gradient[i] = 0;
                gradient[i + 1] = 0;
                gradient[i + 2] = 0;
                gradient[i + 3] = 255;
            }
        }

        Assert.True(QrDecoder.TryDecode(gradient, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("GradientTest", decoded.Text);
    }

    [Fact]
    public void QrDecode_WithFalseFinderNoise() {
        var code = QrCodeEncoder.EncodeText("FinderNoiseTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var pad = 20;
        var newWidth = width + pad * 2;
        var newHeight = height + pad * 2;
        var newStride = newWidth * 4;
        var expanded = new byte[newHeight * newStride];
        FillWhite(expanded);

        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(pixels, y * stride, expanded, (y + pad) * newStride + pad * 4, stride);
        }

        // Draw a finder-like pattern outside the real QR.
        DrawFinder(expanded, newStride, 4, 4, moduleSize: 3);

        Assert.True(QrDecoder.TryDecode(expanded, newWidth, newHeight, newStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("FinderNoiseTest", decoded.Text);
    }

    [Fact]
    public void QrDecode_WithRotatedImage() {
        var code = QrCodeEncoder.EncodeText("RotateTest");
        var pixels = QrPngRenderer.RenderPixels(
            code.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var rot90 = Rotate90(pixels, width, height, stride, out var w90, out var h90, out var s90);
        Assert.True(QrDecoder.TryDecode(rot90, w90, h90, s90, PixelFormat.Rgba32, out var decoded90));
        Assert.Equal("RotateTest", decoded90.Text);

        var rot180 = Rotate180(pixels, width, height, stride, out var w180, out var h180, out var s180);
        Assert.True(QrDecoder.TryDecode(rot180, w180, h180, s180, PixelFormat.Rgba32, out var decoded180));
        Assert.Equal("RotateTest", decoded180.Text);
    }

    [Fact]
    public void QrDecode_AllMultipleCodes() {
        var left = QrCodeEncoder.EncodeText("LeftQR");
        var right = QrCodeEncoder.EncodeText("RightQR");

        var leftPixels = QrPngRenderer.RenderPixels(
            left.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var lw,
            out var lh,
            out var ls);

        var rightPixels = QrPngRenderer.RenderPixels(
            right.Modules,
            new QrPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var rw,
            out var rh,
            out var rs);

        var pad = 12;
        var width = lw + rw + pad * 3;
        var height = Math.Max(lh, rh) + pad * 2;
        var stride = width * 4;
        var canvas = new byte[height * stride];
        FillWhite(canvas);

        // Place left QR.
        for (var y = 0; y < lh; y++) {
            Buffer.BlockCopy(leftPixels, y * ls, canvas, (y + pad) * stride + pad * 4, lw * 4);
        }

        // Place right QR.
        var offsetX = pad * 2 + lw;
        for (var y = 0; y < rh; y++) {
            Buffer.BlockCopy(rightPixels, y * rs, canvas, (y + pad) * stride + offsetX * 4, rw * 4);
        }

        Assert.True(QrDecoder.TryDecodeAll(canvas, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Contains(decoded, item => item.Text == "LeftQR");
        Assert.Contains(decoded, item => item.Text == "RightQR");
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

    private static void Invert(byte[] pixels) {
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
    }

    private static void DrawFinder(byte[] pixels, int stride, int x, int y, int moduleSize) {
        // 7x7 modules: black border, white ring, black 3x3 center.
        var size = 7 * moduleSize;
        FillRect(pixels, stride, x, y, size, size);
        FillRect(pixels, stride, x + moduleSize, y + moduleSize, size - 2 * moduleSize, size - 2 * moduleSize, r: 255, g: 255, b: 255);
        FillRect(pixels, stride, x + 2 * moduleSize, y + 2 * moduleSize, 3 * moduleSize, 3 * moduleSize);
    }

    private static void FillRect(byte[] pixels, int stride, int x, int y, int w, int h, byte r, byte g, byte b) {
        var maxY = y + h;
        var maxX = x + w;
        for (var yy = y; yy < maxY; yy++) {
            var row = yy * stride;
            for (var xx = x; xx < maxX; xx++) {
                var p = row + xx * 4;
                pixels[p] = r;
                pixels[p + 1] = g;
                pixels[p + 2] = b;
                pixels[p + 3] = 255;
            }
        }
    }

    private static byte[] Rotate90(byte[] pixels, int width, int height, int stride, out int outWidth, out int outHeight, out int outStride) {
        outWidth = height;
        outHeight = width;
        outStride = outWidth * 4;
        var rotated = new byte[outHeight * outStride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var dx = height - 1 - y;
                var dy = x;
                var dst = dy * outStride + dx * 4;
                rotated[dst] = pixels[src];
                rotated[dst + 1] = pixels[src + 1];
                rotated[dst + 2] = pixels[src + 2];
                rotated[dst + 3] = pixels[src + 3];
            }
        }

        return rotated;
    }

    private static byte[] Rotate180(byte[] pixels, int width, int height, int stride, out int outWidth, out int outHeight, out int outStride) {
        outWidth = width;
        outHeight = height;
        outStride = outWidth * 4;
        var rotated = new byte[outHeight * outStride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var dy = height - 1 - y;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var dx = width - 1 - x;
                var dst = dy * outStride + dx * 4;
                rotated[dst] = pixels[src];
                rotated[dst + 1] = pixels[src + 1];
                rotated[dst + 2] = pixels[src + 2];
                rotated[dst + 3] = pixels[src + 3];
            }
        }

        return rotated;
    }
}
