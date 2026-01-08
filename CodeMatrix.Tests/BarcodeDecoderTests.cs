using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeDecoderTests {
    [Fact]
    public void Decode_Code128_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CODEMATRIX-123");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("CODEMATRIX-123", decoded.Text);
    }

    [Fact]
    public void Decode_Code39_FromPixels() {
        var barcode = BarcodeEncoder.EncodeCode39("ABC123", includeChecksum: true, fullAsciiMode: false);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Code39, decoded.Type);
        Assert.Equal("ABC123", decoded.Text);
    }

    [Fact]
    public void Decode_Ean13_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.EAN, "590123412345");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.EAN, decoded.Type);
        Assert.Equal("5901234123457", decoded.Text);
    }
}
