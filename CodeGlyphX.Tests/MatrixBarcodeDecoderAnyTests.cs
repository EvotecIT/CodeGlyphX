using Xunit;

namespace CodeGlyphX.Tests;

public class MatrixBarcodeDecoderAnyTests {
    [Fact]
    public void SymbolDecoder_DataMatrix() {
        var modules = DataMatrixCode.Encode("HELLO-123").Modules;

        Assert.True(SymbolDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(SymbolFormat.DataMatrix, decoded.Format);
        Assert.Equal("HELLO-123", decoded.Text);
    }

    [Fact]
    public void SymbolDecoder_Gs1DataBarOmni() {
        var content = "1234567890123";
        var modules = MatrixBarcodeEncoder.EncodeGs1DataBarOmni(content);

        Assert.True(SymbolDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, decoded.Format);
        Assert.Equal(content, decoded.Text);
    }

    [Fact]
    public void SymbolDecoder_ExpectedMismatch_ReturnsFalse() {
        var modules = DataMatrixCode.Encode("EXPECTED").Modules;

        Assert.False(SymbolDecoder.TryDecode(modules, out _, SymbolFormat.Gs1DataBarOmnidirectional));
    }
}
