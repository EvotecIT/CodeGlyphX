using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public class QrPixelRotationTests {
    [Fact]
    public void QrImageDecoder_Decodes_Rotated90() {
        var (pixels, width, height, stride) = RenderSample("ROTATE-90");
        var rotated = Rotate90(pixels, width, height, stride, out var rw, out var rh, out var rstride);

        var ok = QrImageDecoder.TryDecode(rotated, rw, rh, rstride, PixelFormat.Rgba32, out var decoded);

        Assert.True(ok);
        Assert.Equal("ROTATE-90", decoded.Text);
    }

    [Fact]
    public void QrImageDecoder_Decodes_Mirrored() {
        var (pixels, width, height, stride) = RenderSample("MIRROR-X");
        var mirrored = MirrorX(pixels, width, height, stride, out var mw, out var mh, out var mstride);

        var ok = QrImageDecoder.TryDecode(mirrored, mw, mh, mstride, PixelFormat.Rgba32, out var decoded);

        Assert.True(ok);
        Assert.Equal("MIRROR-X", decoded.Text);
    }

    private static (byte[] pixels, int width, int height, int stride) RenderSample(string text) {
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M);
        var pixels = QrPngRenderer.RenderPixels(
            qr.Modules,
            new QrPngRenderOptions {
                ModuleSize = 6,
                QuietZone = 4
            },
            out var width,
            out var height,
            out var stride);
        return (pixels, width, height, stride);
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
                var nx = height - 1 - y;
                var ny = x;
                var dst = ny * outStride + nx * 4;
                rotated[dst + 0] = pixels[src + 0];
                rotated[dst + 1] = pixels[src + 1];
                rotated[dst + 2] = pixels[src + 2];
                rotated[dst + 3] = pixels[src + 3];
            }
        }

        return rotated;
    }

    private static byte[] MirrorX(byte[] pixels, int width, int height, int stride, out int outWidth, out int outHeight, out int outStride) {
        outWidth = width;
        outHeight = height;
        outStride = outWidth * 4;
        var mirrored = new byte[outHeight * outStride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var outRow = y * outStride;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = width - 1 - x;
                var dst = outRow + nx * 4;
                mirrored[dst + 0] = pixels[src + 0];
                mirrored[dst + 1] = pixels[src + 1];
                mirrored[dst + 2] = pixels[src + 2];
                mirrored[dst + 3] = pixels[src + 3];
            }
        }

        return mirrored;
    }
}
