using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class EncodingUtilsTests {
    [Theory]
    [InlineData(20, 932)]
    [InlineData(21, 1250)]
    [InlineData(22, 1251)]
    [InlineData(23, 1252)]
    [InlineData(29, 936)]
    [InlineData(32, 54936)]
    public void TryGetEncoding_ResolvesAdvertisedCodePageAssignments(int assignment, int codePage) {
        Assert.True(EncodingUtils.TryGetEncoding(assignment, out var encoding));
        Assert.Equal(codePage, encoding.CodePage);
    }
}
