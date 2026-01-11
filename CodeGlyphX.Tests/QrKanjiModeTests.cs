using CodeGlyphX;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrKanjiModeTests {
    [Fact]
    public void EncodeDecode_KanjiMode_RoundTrip() {
        var text = "日本語テスト";
        var qr = QrCodeEncoder.EncodeKanji(text, ecc: QrErrorCorrectionLevel.M, minVersion: 1, maxVersion: 10);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void EncodeDecode_ShiftJisByteMode_RoundTrip() {
        var text = "ABC日本語";
        var qr = QrCodeEncoder.EncodeText(text, QrTextEncoding.ShiftJis, includeEci: true);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }
}
