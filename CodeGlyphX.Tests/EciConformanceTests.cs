using CodeGlyphX.Internal;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class EciConformanceTests {
    [Fact]
    public void Latin9_UsesEci17_NotBalticEci15() {
        Assert.True(QrEncoding.TryGetEciAssignment(QrTextEncoding.Iso8859_15, out var assignment));
        Assert.Equal(17, assignment);
        Assert.True(QrEncoding.TryGetTextEncoding(17, out var encoding));
        Assert.Equal(QrTextEncoding.Iso8859_15, encoding);
        Assert.False(QrEncoding.TryGetTextEncoding(15, out _));
        // ISO-8859-15 byte A4 is EURO SIGN. ECI 15 instead denotes ISO-8859-13.
        var matrix = DataMatrixEncoder.EncodeBytes(new byte[] { 0xA4 }, new DataMatrixEncodingOptions { EciAssignmentNumber = 17, Mode = DataMatrixEncodingMode.Base256 });
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var dm));
        Assert.Equal("€", dm.Text);
        Assert.Equal(17, Assert.Single(dm.EciAssignments));
    }
}
