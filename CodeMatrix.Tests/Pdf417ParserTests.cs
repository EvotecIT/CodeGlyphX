using CodeMatrix.Pdf417;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class Pdf417ParserTests {
    [Fact]
    public void TextCompaction_Simple() {
        var decoded = Pdf417DecodedBitStreamParser.Decode(new[] { 1 });
        Assert.Equal("AB", decoded);
    }

    [Fact]
    public void ByteCompaction_Simple() {
        var decoded = Pdf417DecodedBitStreamParser.Decode(new[] { 901, 72, 105 });
        Assert.Equal("Hi", decoded);
    }

    [Fact]
    public void TextCompaction_WithShiftToByte() {
        var decoded = Pdf417DecodedBitStreamParser.Decode(new[] { 1, 913, 33 });
        Assert.Equal("AB!", decoded);
    }
}
