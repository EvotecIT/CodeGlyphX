using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codeglyphx?from=examples";
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.png"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.svg"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.html"), title: "CodeGlyphX QR");
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.jpg"));
    }
}
