using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFormatVariantsTests {
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void ErasedFormatCopiesRecoverAfterOtherCorrectionLevelsFail(int version) {
        const string text = "DAMAGED FORMAT";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.L, minVersion: version, maxVersion: version, forceMask: 0);
        var modules = qr.Modules.Clone();
        var size = modules.Width;
        // Erase both physical copies. Recovery must try mask zero with M and H
        // before reaching L, including single-block correction that mutates its input.
        for (var i = 0; i <= 5; i++) modules[8, i] = false;
        modules[8, 7] = modules[8, 8] = modules[7, 8] = false;
        for (var i = 9; i < 15; i++) modules[14 - i, 8] = false;
        for (var i = 0; i < 8; i++) modules[size - 1 - i, 8] = false;
        for (var i = 8; i < 15; i++) modules[8, size - 15 + i] = false;

        Assert.True(QrDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(version, decoded.Version);
        Assert.Equal(QrErrorCorrectionLevel.L, decoded.ErrorCorrectionLevel);
        Assert.Equal(0, decoded.Mask);
    }

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
