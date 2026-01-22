using Xunit;

namespace CodeGlyphX.Tests;

public class MatrixBarcodeDecoderAnyTests {
    [Fact]
    public void TryDecodeAny_DataMatrix() {
        var modules = DataMatrixCode.Encode("HELLO-123");

        Assert.True(MatrixBarcodeDecoder.TryDecodeAny(modules, out var decoded));
        Assert.Equal(BarcodeType.DataMatrix, decoded.Type);
        Assert.Equal("HELLO-123", decoded.Text);
    }

    [Fact]
    public void TryDecodeAny_Gs1DataBarOmni() {
        var content = "1234567890123";
        var modules = MatrixBarcodeEncoder.EncodeGs1DataBarOmni(content);

        Assert.True(MatrixBarcodeDecoder.TryDecodeAny(modules, out var decoded));
        Assert.Equal(BarcodeType.GS1DataBarOmni, decoded.Type);
        Assert.Equal(content, decoded.Text);
    }

    [Fact]
    public void TryDecodeAny_ExpectedMismatch_ReturnsFalse() {
        var modules = DataMatrixCode.Encode("EXPECTED");

        Assert.False(MatrixBarcodeDecoder.TryDecodeAny(modules, out _, BarcodeType.GS1DataBarOmni));
    }
}
