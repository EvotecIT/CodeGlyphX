using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Barcode2DTests {
    [Fact]
    public void Kix_Size_IsExpected() {
        var matrix = MatrixBarcodeEncoder.EncodeKix("012345");
        Assert.Equal(47, matrix.Width);
        Assert.Equal(8, matrix.Height);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Modules() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("DataMatrixExample");
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("DataMatrixExample", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Pixels() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("MatrixTest");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("MatrixTest", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Modules() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(matrix, out var text));
        Assert.Equal("Pdf417Example", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Pixels() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        Assert.True(Pdf417.Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("Pdf417Example", text);
    }
}
