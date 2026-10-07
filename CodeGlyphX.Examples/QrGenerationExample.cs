using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codeglyphx?from=examples";
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.png"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.svg"));
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.html"), null, extras: new OutputOptions { HtmlTitle = "CodeGlyphX QR" });
        QR.Save(payload, Path.Combine(outputDir, "qr-basic.jpg"));
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic.pdf"),
            QR.Render(payload, OutputFormat.Pdf)
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic.eps"),
            QR.Render(payload, OutputFormat.Eps)
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-basic-raster.pdf"),
            QR.Render(payload, OutputFormat.Pdf, null, extras: new OutputOptions { VectorMode = RenderMode.Raster })
        );
    }
}
