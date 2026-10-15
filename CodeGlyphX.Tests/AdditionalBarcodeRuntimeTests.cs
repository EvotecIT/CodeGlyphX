using CodeGlyphX.Mailmark;
using CodeGlyphX.Postal;
using Xunit;

namespace CodeGlyphX.Tests;

// These vectors also run in the explicit .NET Framework test target, without
// adding a JSON runtime dependency to its compatibility qualification.
public sealed class AdditionalBarcodeRuntimeTests {
    [Theory]
    [InlineData("1100000000000XY11     ", "TTDTTATTDTAATTDTAATTDTAATTDTTDDAATAADDATAATDDFAFTDDTAADDDTAAFDFAFF")]
    [InlineData("11000000000000000XY11     ", "TTDTTATDDTTATTDTAATTDTAATDDTTATTDTTDATFTAATDDTAATDDTATATFAADDAATAATDDTAADFTFTA")]
    public void MailmarkPublicFacade_DecodesRoyalMailStateVectors(string expected, string states) {
        var matrix = DaftCode.Encode(states).Modules;
        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.Mailmark4State, matrix, out var decoded));
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void MailmarkC40Header_IsPreservedByTheRuntimeEncoderAndDetailedDecoder() {
        var payload = Mailmark2D.Prepare("1001234", "12345678", "AB19XY1A", customerContent: "www.xyz.com");
        var symbol = Mailmark2D.Encode(payload);
        Assert.True(Mailmark2D.TryDecode(symbol.Modules, out var decoded, out var type));
        Assert.Equal(payload, decoded.Text);
        Assert.Equal(Mailmark2DType.Type29, type);
    }
}
