using System;
using System.Text;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Tests.TestHelpers;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class OtpTests {
    [Fact]
    public void Base32_RoundTrip_IsDeterministic_Uppercase_NoPadding() {
        var bytes = Encoding.ASCII.GetBytes("foo");
        var b32 = OtpAuthSecret.ToBase32(bytes);
        Assert.Equal("MZXW6", b32);

        var decoded = OtpAuthSecret.FromBase32("mzxw6===");
        Assert.Equal(bytes, decoded);

        var decoded2 = OtpAuthSecret.FromBase32(" mZxw6 - === ");
        Assert.Equal(bytes, decoded2);
    }

    [Fact]
    public void Otpauth_Totp_Escaping_AndOrdering_AreStable() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var uri = OtpAuthTotp.Create("ACME Co", "john.doe+test@example.com", secret, OtpAlgorithm.Sha256, digits: 8, period: 60);
        Assert.Equal(
            "otpauth://totp/ACME%20Co:john.doe%2Btest%40example.com?secret=MZXW6&issuer=ACME%20Co&algorithm=SHA256&digits=8&period=60",
            uri);
    }

    [Fact]
    public void Otpauth_Totp_Parse_RoundTrip() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var uri = OtpAuthTotp.Create("ACME Co", "john.doe@example.com", secret, OtpAlgorithm.Sha256, digits: 8, period: 60);

        Assert.True(OtpAuthParser.TryParse(uri, out var payload));
        Assert.Equal(OtpAuthType.Totp, payload.Type);
        Assert.Equal("ACME Co", payload.Issuer);
        Assert.Equal("john.doe@example.com", payload.Account);
        Assert.Equal(secret, payload.Secret);
        Assert.Equal(OtpAlgorithm.Sha256, payload.Algorithm);
        Assert.Equal(8, payload.Digits);
        Assert.Equal(60, payload.Period);
        Assert.Null(payload.Counter);
    }

    [Fact]
    public void Otpauth_Hotp_Parse_RoundTrip() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var uri = OtpAuthHotp.Create("ACME", "john", secret, counter: 42, OtpAlgorithm.Sha1, digits: 6);

        Assert.True(OtpAuthParser.TryParse(uri, out var payload));
        Assert.Equal(OtpAuthType.Hotp, payload.Type);
        Assert.Equal("ACME", payload.Issuer);
        Assert.Equal("john", payload.Account);
        Assert.Equal(secret, payload.Secret);
        Assert.Equal(OtpAlgorithm.Sha1, payload.Algorithm);
        Assert.Equal(6, payload.Digits);
        Assert.Null(payload.Period);
        Assert.Equal(42, payload.Counter);
    }

    [Fact]
    public void OtpQrPreset_UsesHighEcc_AndDecodes() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret);

        Assert.Equal(QrErrorCorrectionLevel.H, qr.ErrorCorrectionLevel);
        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.StartsWith("otpauth://totp/", decoded.Text);
    }

    [Fact]
    public void OtpQrSafety_Report_Flags_Unsafe_Rendering() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret);

        var opts = OtpQrPreset.CreatePngRenderOptions(
            moduleSize: 2,
            quietZone: 2,
            foreground: new CodeMatrix.Rendering.Png.Rgba32(200, 200, 200),
            background: new CodeMatrix.Rendering.Png.Rgba32(255, 255, 255));

        var report = OtpQrSafety.Evaluate(qr, opts);
        Assert.False(report.IsOtpSafe);
        Assert.False(report.HasSufficientContrast);
        Assert.False(report.HasSufficientQuietZone);
        Assert.False(report.HasSufficientModuleSize);
        Assert.True(report.Score < 60);
        Assert.NotEmpty(report.Issues);
    }

    [Fact]
    public void OtpQrSafety_Report_Allows_Default_Preset() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret);
        var opts = OtpQrPreset.CreatePngRenderOptions();

        var report = OtpQrSafety.Evaluate(qr, opts);
        Assert.True(report.IsOtpSafe);
        Assert.Equal(100, report.Score);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Otpauth_ParseDetailed_Emits_Warnings() {
        var uri = "otpauth://totp/LabelIssuer:acct?secret=JBSWY3DPEHPK3PXP&issuer=ParamIssuer&algorithm=SHA256&digits=7&period=45&foo=bar";

        Assert.True(OtpAuthParser.TryParseDetailed(uri, out var result, out var error), error);
        Assert.NotNull(result);
        Assert.Equal(OtpAuthType.Totp, result.Payload.Type);
        Assert.Equal("ParamIssuer", result.Payload.Issuer);
        Assert.Contains(result.Warnings, w => w.Contains("Issuer mismatch", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("Digits", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("Algorithm", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("Period", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("Unknown parameter", StringComparison.Ordinal));
    }

    [Fact]
    public void OtpAuthValidator_Strict_Fails_On_NonStandard() {
        var uri = "otpauth://totp/Issuer:acct?secret=JBSWY3DPEHPK3PXP&issuer=Issuer&algorithm=SHA256&digits=7&period=45";
        Assert.True(OtpAuthParser.TryParseDetailed(uri, out var result, out var error), error);

        Assert.False(OtpAuthValidator.TryValidate(result, OtpAuthValidationOptions.Strict, out string[] errors));
        Assert.Contains(errors, e => e.Contains("Digits", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("Algorithm", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("period", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OtpQrDecoder_Decodes_Modules() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret, period: 45, digits: 8);

        Assert.True(OtpQrDecoder.TryDecode(qr.Modules, out var payload));
        Assert.Equal(OtpAuthType.Totp, payload.Type);
        Assert.Equal("ACME", payload.Issuer);
        Assert.Equal("john", payload.Account);
        Assert.Equal(8, payload.Digits);
        Assert.Equal(45, payload.Period);
    }

    [Fact]
    public void OtpQrDecoder_Decodes_Pixels() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret, period: 30, digits: 6);

        var png = QrPngRenderer.Render(qr.Modules, OtpQrPreset.CreatePngRenderOptions());
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        Assert.True(OtpQrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var payload));
        Assert.Equal(OtpAuthType.Totp, payload.Type);
        Assert.Equal("ACME", payload.Issuer);
        Assert.Equal("john", payload.Account);
    }

    [Fact]
    public void OtpQrDecoder_Decodes_Pixels_With_Diagnostics() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret, period: 30, digits: 6);

        var png = QrPngRenderer.Render(qr.Modules, OtpQrPreset.CreatePngRenderOptions());
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        Assert.True(OtpQrDecoder.TryDecodeDetailed(rgba, width, height, stride, PixelFormat.Rgba32, out var result, out var qrDiag, out var error), error);
        Assert.Equal(OtpAuthType.Totp, result.Payload.Type);
        Assert.NotEmpty(qrDiag);
        Assert.NotEqual("qr-decode-failed", qrDiag);
    }

    [Fact]
    public void OtpQrDecoder_Decodes_Pixels_LowContrast_Blurred() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var qr = OtpQrPreset.EncodeTotp("ACME", "john", secret);

        var png = QrPngRenderer.Render(qr.Modules, OtpQrPreset.CreatePngRenderOptions(moduleSize: 8, quietZone: 4));
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        ApplyLowContrast(rgba, width, height, stride, factor: 0.55);
        ApplyGradient(rgba, width, height, stride, deltaStart: -20, deltaEnd: 20);
        BoxBlur3x3(rgba, width, height, stride);

        Assert.True(OtpQrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var payload));
        Assert.Equal("ACME", payload.Issuer);
        Assert.Equal("john", payload.Account);
    }

    private static void ApplyLowContrast(byte[] rgba, int width, int height, int stride, double factor) {
        var mid = 128.0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                rgba[p + 0] = ClampByte(mid + (rgba[p + 0] - mid) * factor);
                rgba[p + 1] = ClampByte(mid + (rgba[p + 1] - mid) * factor);
                rgba[p + 2] = ClampByte(mid + (rgba[p + 2] - mid) * factor);
            }
        }
    }

    private static void ApplyGradient(byte[] rgba, int width, int height, int stride, int deltaStart, int deltaEnd) {
        for (var y = 0; y < height; y++) {
            var t = height <= 1 ? 0.0 : y / (double)(height - 1);
            var delta = deltaStart + (deltaEnd - deltaStart) * t;
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                rgba[p + 0] = ClampByte(rgba[p + 0] + delta);
                rgba[p + 1] = ClampByte(rgba[p + 1] + delta);
                rgba[p + 2] = ClampByte(rgba[p + 2] + delta);
            }
        }
    }

    private static void BoxBlur3x3(byte[] rgba, int width, int height, int stride) {
        var tmp = new byte[rgba.Length];
        Buffer.BlockCopy(rgba, 0, tmp, 0, rgba.Length);

        for (var y = 1; y < height - 1; y++) {
            var row = y * stride;
            for (var x = 1; x < width - 1; x++) {
                var sumR = 0;
                var sumG = 0;
                var sumB = 0;
                for (var dy = -1; dy <= 1; dy++) {
                    var rRow = (y + dy) * stride;
                    for (var dx = -1; dx <= 1; dx++) {
                        var p = rRow + (x + dx) * 4;
                        sumR += tmp[p + 0];
                        sumG += tmp[p + 1];
                        sumB += tmp[p + 2];
                    }
                }

                var dst = row + x * 4;
                rgba[dst + 0] = (byte)(sumR / 9);
                rgba[dst + 1] = (byte)(sumG / 9);
                rgba[dst + 2] = (byte)(sumB / 9);
            }
        }
    }

    private static byte ClampByte(double value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)Math.Round(value);
    }
}
