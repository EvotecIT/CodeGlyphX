using CodeMatrix.Tests.TestHelpers;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrGoldenVectorsTests {
    [Theory]
    [InlineData("hello", QrErrorCorrectionLevel.M, 1, 0, "64ADC10B726F7671AFE8C187DC6F622C77656DC22E5188DF51AABBA75105C855")]
    [InlineData("https://example.com", QrErrorCorrectionLevel.Q, 7, 3, "9A1BD444CBCFFBA127DF799C050FF4451A163BE583FDFED9D98FF5AF1E3A448A")]
    public void GoldenVectors_HashPackedBits(string text, QrErrorCorrectionLevel ecc, int version, int mask, string expectedSha256Hex) {
        var qr = QrCodeEncoder.EncodeText(text, ecc, minVersion: version, maxVersion: version, forceMask: mask);
        var packed = qr.Modules.ToPackedBytes();
        var actual = Sha256Hex.HashHex(packed);
        Assert.Equal(expectedSha256Hex, actual);
    }
}
