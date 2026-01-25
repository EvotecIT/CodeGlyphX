using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrEasyTests {
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
