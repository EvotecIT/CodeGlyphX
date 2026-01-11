using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFormatVariantsTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Decode_ForAllMasks_PreservesFormatInfo(int mask) {
        var text = "FORMAT-MASK";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, minVersion: 1, maxVersion: 1, forceMask: mask);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(1, decoded.Version);
        Assert.Equal(QrErrorCorrectionLevel.M, decoded.ErrorCorrectionLevel);
        Assert.Equal(mask, decoded.Mask);
    }
}
