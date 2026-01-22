using CodeGlyphX.Pdf417;
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

    [Fact]
    public void Decode_Pdf417_Macro_FromModules() {
        var macro = new Pdf417MacroOptions {
            SegmentIndex = 0,
            FileId = "123",
            IsLastSegment = true,
            FileName = "file.txt",
            Sender = "sender@example.com"
        };
        var modules = Pdf417Code.EncodeMacro("MACRO-PDF417", macro);

        Assert.True(CodeGlyph.TryDecode(modules, out var decoded));
        Assert.Equal(CodeGlyphKind.Pdf417, decoded.Kind);
        Assert.Equal("MACRO-PDF417", decoded.Text);
        Assert.NotNull(decoded.Pdf417Macro);
        Assert.Equal(0, decoded.Pdf417Macro!.SegmentIndex);
        Assert.Equal("123", decoded.Pdf417Macro.FileId);
        Assert.True(decoded.Pdf417Macro.IsLastSegment);
        Assert.Equal("file.txt", decoded.Pdf417Macro.FileName);
        Assert.Equal("sender@example.com", decoded.Pdf417Macro.Sender);
    }
}
