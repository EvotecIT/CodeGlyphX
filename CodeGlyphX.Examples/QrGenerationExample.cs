using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codeglyphx?from=examples";
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.png"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.svg"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.html"), title: "CodeGlyphX QR");
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.jpg"));
        QR.SavePdf(payload, Path.Combine(outputDir, "qr-basic.pdf"));
        QR.SaveEps(payload, Path.Combine(outputDir, "qr-basic.eps"));
        QR.SavePdf(payload, Path.Combine(outputDir, "qr-basic-raster.pdf"), renderMode: RenderMode.Raster);
    }
}
