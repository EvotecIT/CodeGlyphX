using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class EvotecExamples {
    public static void Run(string outputDir) {
        var url = QrPayload.Url("https://evotec.xyz");
        QR.SavePng(url, Path.Combine(outputDir, "qr-evotec.png"));

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo", "Logo-evotec.png");
        if (!logoPath.TryReadBinary(out var logoBytes)) {
            "Missing Assets/Logo/Logo-evotec.png".WriteText(outputDir, "qr-evotec-logo-missing.txt");
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

        QR.SavePng(url, Path.Combine(outputDir, "qr-evotec-logo.png"), withLogo);

        QR.SaveSvg(url, Path.Combine(outputDir, "qr-evotec-logo.svg"), withLogo);

        QR.SaveHtml(url, Path.Combine(outputDir, "qr-evotec-logo.html"), withLogo, title: "Evotec QR");
    }
}
