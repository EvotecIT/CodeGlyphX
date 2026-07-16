using Xunit;

namespace CodeGlyphX.Tests.Net472;

public sealed class Gs1DigitalLinkNet472Tests {
    [Fact]
    public void Net472_Gs1DigitalLink_ParsesAndCanonicalizes() {
        var result = Gs1DigitalLink.Parse(
            "https://brand.example/01/09520123456788/10/ABC1/21/12345?17=180426");

        Assert.Equal("01", result.PrimaryIdentifier.Ai);
        Assert.Equal(2, result.KeyQualifiers.Count);
        Assert.Equal(
            "https://id.gs1.org/01/09520123456788/10/ABC1/21/12345?17=180426",
            result.CanonicalUri);
    }
}
