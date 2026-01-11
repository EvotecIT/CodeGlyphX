using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MatrixApiTests {
    [Fact]
    public void DataMatrixCode_Png_RoundTrip() {
        var png = DataMatrixCode.Png("DM-HELLO");
        Assert.True(DataMatrixCode.TryDecodePng(png, out var text));
        Assert.Equal("DM-HELLO", text);
    }

    [Fact]
    public void Pdf417Code_Png_RoundTrip() {
        var options = new Pdf417EncodeOptions { ErrorCorrectionLevel = 2 };
        var png = Pdf417Code.Png("PDF-HELLO", options);
        Assert.True(Pdf417Code.TryDecodePng(png, out var text));
        Assert.Equal("PDF-HELLO", text);
    }
}
