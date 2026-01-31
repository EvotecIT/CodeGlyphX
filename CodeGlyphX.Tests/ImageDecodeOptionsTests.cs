using CodeGlyphX;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageDecodeOptionsTests {
    [Fact]
    public void CodeGlyph_Downscales_NonQr_ImageOptions() {
        var matrix = DataMatrixEncoder.Encode("DOWNSCALE");
        var pixels = MatrixPngRenderer.RenderPixels(
            matrix,
            new MatrixPngRenderOptions { ModuleSize = 20, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var options = new CodeGlyphDecodeOptions {
            Image = new ImageDecodeOptions { MaxDimension = 200, MaxMilliseconds = 200 },
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast, MaxMilliseconds = 200 }
        };

        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal("DOWNSCALE", decoded.Text);
    }

    [Fact]
    public void Barcode_Downscales_WithImageOptions() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "TEST-123");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 12,
            QuietZone = 10,
            HeightModules = 40
        });

        var options = new ImageDecodeOptions { MaxDimension = 200 };

        Assert.True(Barcode.TryDecodePng(png, BarcodeType.Code128, options, out var decoded));
        Assert.Equal("TEST-123", decoded.Text);
    }

    [Fact]
    public void Qr_Downscales_WithImageOptions() {
        var png = QrCode.Render("HELLO-WORLD", OutputFormat.Png, new QrEasyOptions { ModuleSize = 18, QuietZone = 4 }).Data;
        var imageOptions = new ImageDecodeOptions { MaxDimension = 200 };

        Assert.True(QR.TryDecodePng(png, imageOptions, options: null, out var decoded));
        Assert.Equal("HELLO-WORLD", decoded.Text);
    }

    [Fact]
    public void ImageDecodeOptions_Safe_Sets_DefaultCaps() {
        var options = ImageDecodeOptions.Safe();

        Assert.Equal(64 * 1024 * 1024, options.MaxBytes);
        Assert.Equal(20_000_000, options.MaxPixels);
        Assert.Equal(120, options.MaxAnimationFrames);
        Assert.Equal(60_000, options.MaxAnimationDurationMs);
        Assert.Equal(20_000_000, options.MaxAnimationFramePixels);
    }
}
