using CodeMatrix;
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
}
