using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderedOutputOwnershipTests {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(0, 6)]
    [InlineData(1, 6)]
    [InlineData(2, 6)]
    public void PngResults_OwnTheirBytesAcrossSubsequentRendersAndMutableCopies(int family, int compression) {
        var output = Render("PNG-OUTPUT-OWNERSHIP");
        var original = output.Data.ToArray();
        var copy = output.ToArray();
        copy[0] ^= 255;
        var next = Render("PNG-OTHER-OUTPUT");

        Assert.Equal(original, output.Data.ToArray());
        Assert.NotEqual(original, next.Data.ToArray());
        using var stream = new MemoryStream();
        OutputWriter.Write(stream, output);
        Assert.Equal(original, stream.ToArray());
        Assert.True(ImageReader.TryDecodeRgba32(original, out _, out _, out _));

        RenderedOutput Render(string payload) {
            var options = new OutputOptions { PngCompressionLevel = compression };
            if (family == 1) return DataMatrixCode.Render(payload, OutputFormat.Png, outputOptions: options);
            if (family == 2) return Barcode.Render(SymbolFormat.Code128, payload, OutputFormat.Png, outputOptions: options);
            return QR.Render(payload, OutputFormat.Png, outputOptions: options);
        }
    }

    [Fact]
    public void BinaryOutputOwnsItsBytesAndCopiesAreIndependent() {
        var input = new byte[] { 1, 2, 3 };
        var output = RenderedOutput.FromBinary(OutputFormat.Png, input);
        input[0] = 9;
        var copy = output.ToArray();
        copy[1] = 8;

        Assert.Equal(new byte[] { 1, 2, 3 }, output.Data.ToArray());
        using var stream = new MemoryStream();
        OutputWriter.Write(stream, output);
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    [Fact]
    public void TextAndBytesRemainConsistentWhenCopiesAreChangedOrOutputIsTranscoded() {
        const string text = "Zażółć 😀";
        var output = RenderedOutput.FromText(OutputFormat.Svg, text, Encoding.Unicode);
        var copy = output.ToArray();
        copy[0] = 0;

        Assert.Equal(text, output.GetText());
        Assert.Equal(Encoding.Unicode.GetBytes(text), output.Data.ToArray());
        using var original = new MemoryStream();
        OutputWriter.Write(original, output);
        Assert.Equal(Encoding.Unicode.GetBytes(text), original.ToArray());
        using var transcoded = new MemoryStream();
        OutputWriter.Write(transcoded, output, new UTF8Encoding(false));
        Assert.Equal(Encoding.UTF8.GetBytes(text), transcoded.ToArray());
    }
}
