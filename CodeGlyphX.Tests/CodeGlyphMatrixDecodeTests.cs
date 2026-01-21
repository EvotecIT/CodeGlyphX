using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphMatrixDecodeTests {
    [Fact]
    public void Decode_Qr_FromModules() {
        var qr = QrCodeEncoder.EncodeText("MATRIX-QR");

        Assert.True(CodeGlyph.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(CodeGlyphKind.Qr, decoded.Kind);
        Assert.Equal("MATRIX-QR", decoded.Text);
    }

    [Fact]
    public void Decode_DataMatrix_FromModules() {
        var modules = DataMatrixCode.Encode("DM-MODULES");

        Assert.True(CodeGlyph.TryDecode(modules, out var decoded));
        Assert.Equal(CodeGlyphKind.DataMatrix, decoded.Kind);
        Assert.Equal("DM-MODULES", decoded.Text);
    }

    [Fact]
    public void Decode_Gs1DataBarOmni_FromModules() {
        var value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarOmni, value);

        Assert.True(CodeGlyph.TryDecode(modules, out var decoded, expectedBarcode: BarcodeType.GS1DataBarOmni));
        Assert.Equal(CodeGlyphKind.Barcode1D, decoded.Kind);
        Assert.Equal(value, decoded.Text);
    }
}
