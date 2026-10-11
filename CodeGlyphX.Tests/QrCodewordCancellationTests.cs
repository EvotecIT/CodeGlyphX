using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrCodewordCancellationTests {
    [Fact]
    public void CancellationDuringExtractionLeavesTheSymbolReusable() {
        var text = new string('X', 900);
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, minVersion: 40, maxVersion: 40, forceMask: 5);
        var polls = 0;

        Assert.False(QrDecoder.TryDecodeInternal(qr.Modules, () => ++polls >= 32, out _, out var diagnostics));
        Assert.Equal(QrDecodeFailure.Cancelled, diagnostics.Failure);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(40, decoded.Version);
        Assert.Equal(5, decoded.Mask);
    }
}
