using CodeGlyphX;
using CodeGlyphX.Code39;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpRendererTests {
    [Fact]
    public void WebpRenderer_Qr_Lossy_Encodes() {
        var qr = QrCodeEncoder.EncodeText("HELLO");
        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 2
        };

        var webp = QrWebpRenderer.Render(qr.Modules, opts, quality: 75);

        Assert.True(WebpReader.IsWebp(webp));
        Assert.True(WebpReader.TryReadDimensions(webp, out var width, out var height));
        Assert.True(width > 0);
        Assert.True(height > 0);
    }

    [Fact]
    public void WebpRenderer_Matrix_Lossy_Encodes() {
        var matrix = new BitMatrix(4, 4);
        matrix[0, 0] = true;
        matrix[1, 1] = true;
        matrix[2, 2] = true;
        matrix[3, 3] = true;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 2
        };

        var webp = MatrixWebpRenderer.Render(matrix, opts, quality: 80);

        Assert.True(WebpReader.IsWebp(webp));
        Assert.True(WebpReader.TryReadDimensions(webp, out var width, out var height));
        Assert.True(width > 0);
        Assert.True(height > 0);
    }

    [Fact]
    public void WebpRenderer_Barcode_Lossy_Encodes() {
        var barcode = Code39Encoder.Encode("ABC", includeChecksum: false, fullAsciiMode: false);
        var opts = new BarcodePngRenderOptions {
            ModuleSize = 2,
            QuietZone = 2,
            HeightModules = 30
        };

        var webp = BarcodeWebpRenderer.Render(barcode, opts, quality: 70);

        Assert.True(WebpReader.IsWebp(webp));
        Assert.True(WebpReader.TryReadDimensions(webp, out var width, out var height));
        Assert.True(width > 0);
        Assert.True(height > 0);
    }
}
