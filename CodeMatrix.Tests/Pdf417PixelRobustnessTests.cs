using CodeMatrix.Pdf417;
using CodeMatrix.Rendering.Png;
using Xunit;

namespace CodeMatrix.Tests;

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

    private static void SetPixel(byte[] pixels, int stride, int x, int y) {
        var p = y * stride + x * 4;
        pixels[p] = 0;
        pixels[p + 1] = 0;
        pixels[p + 2] = 0;
        pixels[p + 3] = 255;
    }

}
