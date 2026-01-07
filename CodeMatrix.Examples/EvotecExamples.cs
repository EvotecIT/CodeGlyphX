using CodeMatrix;
using CodeMatrix.Payloads;

namespace CodeMatrix.Examples;

internal static class EvotecExamples {
    public static void Run(string outputDir) {
        var url = QrPayload.Url("https://evotec.xyz");
        var plain = QrEasy.RenderPng(url);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec.png", plain);

        var logoPath = "Assets/Logo/Logo-evotec.png";
        if (!ExampleHelpers.TryReadRepoFile(logoPath, out var logoBytes, out _)) {
            ExampleHelpers.WriteText(outputDir, "qr-evotec-logo-missing.txt", "Missing Assets/Logo/Logo-evotec.png");
            return;
        }

        var withLogo = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            LogoPng = logoBytes,
            LogoScale = 0.22,
            LogoPaddingPx = 6,
            LogoDrawBackground = true,
            LogoCornerRadiusPx = 8,
        };

        var pngLogo = QrEasy.RenderPng(url, withLogo);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo.png", pngLogo);

        var svg = QrEasy.RenderSvg(url, withLogo);
        ExampleHelpers.WriteText(outputDir, "qr-evotec-logo.svg", svg);

        var html = QrEasy.RenderHtml(url, withLogo);
        ExampleHelpers.WriteText(outputDir, "qr-evotec-logo.html", ExampleHelpers.WrapHtml("Evotec QR", html));
    }
}
