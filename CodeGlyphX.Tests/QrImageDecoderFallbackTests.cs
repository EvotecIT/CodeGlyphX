using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrImageDecoderFallbackTests {
    private const string Payload = "LEGACY-FALLBACK-ROUNDTRIP";

    [Fact]
    public void Fallback_Decodes_Large_Png_Render() {
        var png = RenderPng(moduleSize: 18, quietZone: 6);
        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeImage(png, out var decoded, out var info, new QrPixelDecodeOptions {
                MaxDimension = 2048
            });
            Assert.True(ok);
            Assert.Equal(Payload, decoded.Text);
            Assert.True(info.Dimension >= 21);
        });
    }

    [Fact]
    public void Fallback_Decodes_Large_Jpeg_Render() {
        var jpg = RenderJpeg(moduleSize: 18, quietZone: 6, quality: 100);
        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeImage(jpg, out var decoded, out var info, new QrPixelDecodeOptions {
                MaxDimension = 2048
            });
            Assert.True(ok);
            Assert.Equal(Payload, decoded.Text);
            Assert.True(info.Dimension >= 21);
        });
    }

    private static byte[] RenderPng(int moduleSize, int quietZone) {
        var qr = QrCodeEncoder.EncodeText(Payload);
        return QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone
        });
    }

    private static byte[] RenderJpeg(int moduleSize, int quietZone, int quality) {
        var qr = QrCodeEncoder.EncodeText(Payload);
        return QrJpegRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone
        }, quality);
    }

    private static void WithForcedFallback(System.Action action) {
        var previous = CodeGlyphXFeatures.ForceQrFallbackForTests;
        CodeGlyphXFeatures.ForceQrFallbackForTests = true;
        try {
            action();
        } finally {
            CodeGlyphXFeatures.ForceQrFallbackForTests = previous;
        }
    }
}

