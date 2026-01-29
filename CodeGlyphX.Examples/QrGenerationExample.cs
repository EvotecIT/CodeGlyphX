using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codeglyphx?from=examples";
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.png"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.svg"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.html"), null, new RenderExtras { HtmlTitle = "CodeGlyphX QR" });
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.jpg"));
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic.pdf"),
            QrCode.Render(payload, OutputFormat.Pdf)
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic.eps"),
            QrCode.Render(payload, OutputFormat.Eps)
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic-raster.pdf"),
            QrCode.Render(payload, OutputFormat.Pdf, null, new RenderExtras { VectorMode = RenderMode.Raster })
        );
    }
}
