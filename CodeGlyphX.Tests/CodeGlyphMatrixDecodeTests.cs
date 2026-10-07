using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphMatrixDecodeTests {
    [Fact]
    public void Decode_Qr_FromModules() {
        var qr = QrCodeEncoder.EncodeText("MATRIX-QR");

        Assert.True(SymbolDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(SymbolFormat.QrCode, decoded.Format);
        Assert.Equal("MATRIX-QR", decoded.Text);
    }

    [Fact]
    public void Decode_DataMatrix_FromModules() {
        var modules = DataMatrixCode.Encode("DM-MODULES").Modules;

        Assert.True(SymbolDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(SymbolFormat.DataMatrix, decoded.Format);
        Assert.Equal("DM-MODULES", decoded.Text);
    }

    [Fact]
    public void Decode_Gs1DataBarOmni_FromModules() {
        var value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarOmni, value);

        Assert.True(SymbolDecoder.TryDecode(modules, out var decoded, format: SymbolFormat.Gs1DataBarOmnidirectional));
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, decoded.Format);
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
        var modules = Pdf417Code.EncodeMacro("MACRO-PDF417", macro).Modules;

        Assert.True(SymbolDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(SymbolFormat.Pdf417, decoded.Format);
        Assert.Equal("MACRO-PDF417", decoded.Text);
        var metadata = Assert.IsType<Pdf417SymbolMetadata>(decoded.Metadata);
        Assert.NotNull(metadata.Macro);
        Assert.Equal(0, metadata.Macro!.SegmentIndex);
        Assert.Equal("123", metadata.Macro.FileId);
        Assert.True(metadata.Macro.IsLastSegment);
        Assert.Equal("file.txt", metadata.Macro.FileName);
        Assert.Equal("sender@example.com", metadata.Macro.Sender);
    }
}
