using System.IO;
using System.Text;
using CodeMatrix;
using CodeMatrix.Rendering;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class OtpExample {
    public static void Run(string outputDir) {
        var totp = Otp.Totp("CodeMatrix", "alice@example.com", "JBSWY3DPEHPK3PXP");
        var hotp = Otp.Hotp("CodeMatrix", "alice@example.com", "JBSWY3DPEHPK3PXP", counter: 42);

        totp.SavePng(Path.Combine(outputDir, "otp-totp.png"));
        hotp.SavePng(Path.Combine(outputDir, "otp-hotp.png"));

        var totpUri = totp.Uri();
        var hotpUri = hotp.Uri();

        var totpQr = totp.Encode();
        var renderOpts = new QrPngRenderOptions {
            ModuleSize = totp.Options.ModuleSize,
            QuietZone = totp.Options.QuietZone,
            Foreground = totp.Options.Foreground,
            Background = totp.Options.Background,
        };

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

        sb.ToString().WriteText(outputDir, "otp-report.txt");
    }
}
