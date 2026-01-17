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
    public void DataMatrix_RoundTrip_C40() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC123", DataMatrix.DataMatrixEncodingMode.C40);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Text() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("hello123", DataMatrix.DataMatrixEncodingMode.Text);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("hello123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_X12() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC123", DataMatrix.DataMatrixEncodingMode.X12);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Edifact() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC_", DataMatrix.DataMatrixEncodingMode.Edifact);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC_", text);
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
    public void DataMatrix_RoundTrip_Pixels_Mirrored() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("MatrixMirror");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var mirrored = MirrorPixels(pixels, width, height);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(mirrored, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("MatrixMirror", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Modules() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(matrix, out var text));
        Assert.Equal("Pdf417Example", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Modules_WithPadding() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        var padded = PadColumns(matrix, left: 4, right: 3);
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(padded, out var text));
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

    [Fact]
    public void Pdf417_RoundTrip_Pixels_Mirrored() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Mirror");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var mirrored = MirrorPixels(pixels, width, height);
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(mirrored, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("Pdf417Mirror", text);
    }


    private static BitMatrix PadColumns(BitMatrix matrix, int left, int right) {
        var width = matrix.Width + left + right;
        var height = matrix.Height;
        var padded = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                padded[left + x, y] = matrix[x, y];
            }
        }
        return padded;
    }

    private static byte[] MirrorPixels(byte[] pixels, int width, int height) {
        var output = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = width - 1 - x;
                var dst = row + nx * 4;
                output[dst + 0] = pixels[src + 0];
                output[dst + 1] = pixels[src + 1];
                output[dst + 2] = pixels[src + 2];
                output[dst + 3] = pixels[src + 3];
            }
        }
        return output;
    }
}
