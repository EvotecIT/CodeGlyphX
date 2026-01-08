using System.Text;
using CodeMatrix;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class OtpExample {
    public static void Run(string outputDir) {
        var secret = OtpAuthSecret.FromBase32("JBSWY3DPEHPK3PXP");
        var totpUri = OtpAuthTotp.Create("CodeMatrix", "alice@example.com", secret, OtpAlgorithm.Sha1, digits: 6, period: 30);
        var hotpUri = OtpAuthHotp.Create("CodeMatrix", "alice@example.com", secret, counter: 42, OtpAlgorithm.Sha1, digits: 6);

        var totpQr = OtpQrPreset.EncodeUri(totpUri, QrErrorCorrectionLevel.H, 1, 10, null);
        var hotpQr = OtpQrPreset.EncodeUri(hotpUri, QrErrorCorrectionLevel.H, 1, 10, null);

        var renderOpts = OtpQrPreset.CreatePngRenderOptions(moduleSize: 6, quietZone: 4);

        var totpPng = QrPngRenderer.Render(totpQr.Modules, renderOpts);
        var hotpPng = QrPngRenderer.Render(hotpQr.Modules, renderOpts);
        ExampleHelpers.WriteBinary(outputDir, "otp-totp.png", totpPng);
        ExampleHelpers.WriteBinary(outputDir, "otp-hotp.png", hotpPng);

        var report = OtpQrSafety.Evaluate(totpQr, renderOpts, requireHighEcc: true);

        var sb = new StringBuilder();
        sb.AppendLine("TOTP URI:");
        sb.AppendLine(totpUri);
        sb.AppendLine();
        sb.AppendLine("HOTP URI:");
        sb.AppendLine(hotpUri);
        sb.AppendLine();
        sb.AppendLine("Safety:");
        sb.AppendLine($"Score={report.Score} Safe={report.IsOtpSafe}");
        sb.AppendLine($"Contrast={report.ContrastRatio:0.00} QuietZoneOk={report.HasSufficientQuietZone} ModuleSizeOk={report.HasSufficientModuleSize} EccOk={report.HasRecommendedErrorCorrection}");
        if (report.Issues.Length > 0) {
            sb.AppendLine("Issues:");
            for (var i = 0; i < report.Issues.Length; i++) {
                sb.AppendLine("- " + report.Issues[i]);
            }
        }
        sb.AppendLine();

        if (OtpQrDecoder.TryDecode(totpQr.Modules, out var parsedModules)) {
            sb.AppendLine("Decode modules: ok");
            sb.AppendLine($"Issuer={parsedModules.Issuer} Account={parsedModules.Account} Type={parsedModules.Type}");
        } else {
            sb.AppendLine("Decode modules: failed");
        }
        sb.AppendLine();

        var pixels = QrPngRenderer.RenderPixels(totpQr.Modules, renderOpts, out var width, out var height, out var stride);
        if (OtpQrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var parsedPixels)) {
            sb.AppendLine("Decode pixels: ok");
            sb.AppendLine($"Issuer={parsedPixels.Issuer} Account={parsedPixels.Account} Type={parsedPixels.Type}");
        } else {
            sb.AppendLine("Decode pixels: failed");
        }

        ExampleHelpers.WriteText(outputDir, "otp-report.txt", sb.ToString());
    }
}
