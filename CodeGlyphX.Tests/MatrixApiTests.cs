using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MatrixApiTests {
    [Fact]
    public void DataMatrixCode_Png_RoundTrip() {
        var png = DataMatrixCode.Render("DM-HELLO", OutputFormat.Png).Data;
        Assert.True(DataMatrixCode.TryDecodePng(png, out var text));
        Assert.Equal("DM-HELLO", text);
    }

    [Fact]
    public void DataMatrixCode_Image_RoundTrip() {
        var png = DataMatrixCode.Render("DM-IMG", OutputFormat.Png).Data;
        Assert.True(DataMatrixCode.TryDecodeImage(png, out var text));
        Assert.Equal("DM-IMG", text);
    }

    [Fact]
    public void Pdf417Code_Png_RoundTrip() {
        var options = new Pdf417EncodeOptions { ErrorCorrectionLevel = 2 };
        var png = Pdf417Code.Render("PDF-HELLO", OutputFormat.Png, options).Data;
        Assert.True(Pdf417Code.TryDecodePng(png, out string text));
        Assert.Equal("PDF-HELLO", text);
    }

    [Fact]
    public void Pdf417Code_Image_RoundTrip() {
        var options = new Pdf417EncodeOptions { ErrorCorrectionLevel = 2 };
        var png = Pdf417Code.Render("PDF-IMG", OutputFormat.Png, options).Data;
        Assert.True(Pdf417Code.TryDecodeImage(png, out string text));
        Assert.Equal("PDF-IMG", text);
    }

    [Fact]
    public void Pdf417Code_Macro_Png_RoundTrip() {
        var macro = new Pdf417MacroOptions {
            SegmentIndex = 0,
            FileId = "123",
            IsLastSegment = true,
            FileName = "macro.txt"
        };
        var png = Pdf417Code.RenderMacro("PDF-MACRO", macro, OutputFormat.Png).Data;

        Assert.True(Pdf417Code.TryDecodePng(png, out Pdf417Decoded decoded));
        Assert.Equal("PDF-MACRO", decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal("123", decoded.Macro!.FileId);
        Assert.True(decoded.Macro.IsLastSegment);
        Assert.Equal("macro.txt", decoded.Macro.FileName);
    }
}
