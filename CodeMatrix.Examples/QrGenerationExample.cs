using System.IO;
using CodeMatrix;

namespace CodeMatrix.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codematrix?from=examples";
        QR.SavePng(payload, Path.Combine(outputDir, "qr-basic.png"));
        QR.SaveSvg(payload, Path.Combine(outputDir, "qr-basic.svg"));
        QR.SaveHtml(payload, Path.Combine(outputDir, "qr-basic.html"), title: "CodeMatrix QR");
        QR.SaveJpeg(payload, Path.Combine(outputDir, "qr-basic.jpg"));
    }
}
