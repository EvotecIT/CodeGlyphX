using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderedOutputOwnershipTests {
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
