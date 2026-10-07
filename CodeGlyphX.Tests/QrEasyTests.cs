using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrEasyTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QrEasy_DefaultTextEncoding_HonorsIncludeEci(bool includeEci) {
        const string text = "Zażółć 😀";
        var actual = QR.Encode(text, new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.M,
            IncludeEci = includeEci,
            ForceMask = 0
        });
        var expected = QrCodeEncoder.EncodeText(text, new QrEncodingOptions {
            EciMode = includeEci ? QrEciMode.Auto : QrEciMode.Never,
            ForceMask = 0
        });

        Assert.Equal(expected.Size, actual.Size);
        for (var y = 0; y < expected.Size; y++) {
            for (var x = 0; x < expected.Size; x++) {
                Assert.Equal(expected.Modules[x, y], actual.Modules[x, y]);
            }
        }
    }

    [Fact]
    public void QrEasy_DefaultEcc_IsHighForOtp() {
        var qr = QrEasy.Encode("otpauth://totp/Example?secret=ABCDEF");
        Assert.Equal(QrErrorCorrectionLevel.H, qr.ErrorCorrectionLevel);
    }

    [Fact]
    public void QrEasy_DefaultEcc_IsHighWhenLogoProvided() {
        var opts = new QrEasyOptions { LogoPng = new byte[] { 1, 2, 3 } };
        var qr = QrEasy.Encode("https://example.com", opts);
        Assert.Equal(QrErrorCorrectionLevel.H, qr.ErrorCorrectionLevel);
    }

    [Fact]
    public void QrEasy_DoesNotOverrideExplicitEcc() {
        var opts = new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L, LogoPng = new byte[] { 1, 2, 3 } };
        var qr = QrEasy.Encode("https://example.com", opts);
        Assert.Equal(QrErrorCorrectionLevel.L, qr.ErrorCorrectionLevel);
    }

    [Fact]
    public void QrEasy_BumpsMinVersion_ForLogoBackground() {
        var opts = new QrEasyOptions { LogoPng = new byte[] { 1 }, LogoDrawBackground = true };
        var qr = QrEasy.Encode("hi", opts);
        Assert.True(qr.Version >= 8);
    }
}
