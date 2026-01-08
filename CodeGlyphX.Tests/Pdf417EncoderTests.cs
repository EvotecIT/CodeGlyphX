using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417EncoderTests {
    [Fact]
    public void Pdf417_TextCompaction_RoundTrip() {
        var options = new Pdf417EncodeOptions { Compaction = Pdf417Compaction.Text };
        var matrix = Pdf417Encoder.Encode("Hello-World", options);
        Assert.True(Pdf417Decoder.TryDecode(matrix, out var text));
        Assert.Equal("Hello-World", text);
    }

    [Fact]
    public void Pdf417_NumericCompaction_RoundTrip() {
        var options = new Pdf417EncodeOptions { Compaction = Pdf417Compaction.Numeric };
        var value = "1234567890123456789012345";
        var matrix = Pdf417Encoder.Encode(value, options);
        Assert.True(Pdf417Decoder.TryDecode(matrix, out var text));
        Assert.Equal(value, text);
    }
}
