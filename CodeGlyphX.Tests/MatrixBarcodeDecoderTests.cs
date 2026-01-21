using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MatrixBarcodeDecoderTests {
    [Fact]
    public void Decode_Gs1DataBarOmni_FromModules() {
        var value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarOmni, value);

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1DataBarOmni, modules, out var text));
        Assert.Equal(value, text);
    }

    [Fact]
    public void Decode_Gs1DataBarStacked_FromModules() {
        var value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarStacked, value);

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1DataBarStacked, modules, out var text));
        Assert.Equal(value, text);
    }

    [Fact]
    public void Decode_Gs1DataBarExpandedStacked_FromModules() {
        var value = "1234567890";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarExpandedStacked, value);

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1DataBarExpandedStacked, modules, out var text));
        Assert.Equal(value, text);
    }
}
