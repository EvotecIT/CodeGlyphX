using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests.Net472;

// Keep this suite small and net472-safe: it should run fast on Windows CI.
public sealed class QrNet472SmokeTests {
    private const string Payload = "NET472-CI-SMOKE";

    [Fact]
    public void Net472_QrImageDecoder_Decodes_RenderedPng() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 6
        });

        var ok = QrImageDecoder.TryDecodeImage(png, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_Decodes_RenderedJpeg() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var jpg = QrJpegRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 6
        }, quality: 100);

        var ok = QrImageDecoder.TryDecodeImage(jpg, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrDecoder_Decodes_Modules() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var ok = QrDecoder.TryDecode(qr.Modules, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_ImageOptions_ByteArray_Overload_Works() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 24,
            QuietZone = 8
        });

        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 600, maxDimension: 900);
        var ok = QrImageDecoder.TryDecodeImage(png, imageOptions, options: null, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_ImageOptions_Stream_Overload_Works() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 20,
            QuietZone = 6
        });

        using var stream = new System.IO.MemoryStream(png);
        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 600, maxDimension: 1000);
        var ok = QrImageDecoder.TryDecodeImage(stream, imageOptions, options: null, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }
}
