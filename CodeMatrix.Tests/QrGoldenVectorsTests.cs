using CodeMatrix.Tests.TestHelpers;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrGoldenVectorsTests {
    [Theory]
    [InlineData("hello", QrErrorCorrectionLevel.M, 1, 0, "64ADC10B726F7671AFE8C187DC6F622C77656DC22E5188DF51AABBA75105C855")]
    [InlineData("https://example.com", QrErrorCorrectionLevel.Q, 7, 3, "5E9C36987590E9A6E73B3FF60191F3ACE706B850D41624B2446E7B8B11F310C5")]
    public void GoldenVectors_HashPackedBits(string text, QrErrorCorrectionLevel ecc, int version, int mask, string expectedSha256Hex) {
        var qr = QrCodeEncoder.EncodeText(text, ecc, minVersion: version, maxVersion: version, forceMask: mask);
        var packed = qr.Modules.ToPackedBytes();
        var actual = Sha256Hex.HashHex(packed);
        Assert.Equal(expectedSha256Hex, actual);
    }
}
