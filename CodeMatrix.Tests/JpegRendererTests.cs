using CodeMatrix.Rendering.Jpeg;
using CodeMatrix.Rendering.Png;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class JpegRendererTests {
    [Fact]
    public void Render_Qr_Jpeg_Has_Sof0_And_Dimensions() {
        var qr = QrCodeEncoder.EncodeText("HELLO");
        var opts = new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 };
        var jpg = QrJpegRenderer.Render(qr.Modules, opts, quality: 80);

        Assert.True(jpg.Length > 200);
        Assert.Equal(0xFF, jpg[0]);
        Assert.Equal(0xD8, jpg[1]);
        Assert.Equal(0xFF, jpg[^2]);
        Assert.Equal(0xD9, jpg[^1]);

        var (w, h) = ReadSof0(jpg);
        var expected = (qr.Modules.Width + opts.QuietZone * 2) * opts.ModuleSize;
        Assert.Equal(expected, w);
        Assert.Equal(expected, h);
    }

    private static (int width, int height) ReadSof0(byte[] jpeg) {
        var i = 2;
        while (i + 3 < jpeg.Length) {
            if (jpeg[i] != 0xFF) {
                i++;
                continue;
            }
            var marker = jpeg[i + 1];
            i += 2;
            if (marker == 0xD8 || marker == 0xD9) continue;
            if (marker == 0xDA) break;
            if (i + 2 > jpeg.Length) break;
            var len = (jpeg[i] << 8) | jpeg[i + 1];
            if (len < 2 || i + len > jpeg.Length) break;
            if (marker == 0xC0) {
                var height = (jpeg[i + 3] << 8) | jpeg[i + 4];
                var width = (jpeg[i + 5] << 8) | jpeg[i + 6];
                return (width, height);
            }
            i += len;
        }
        return (0, 0);
    }
}
