using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrPrintExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/print-ready";

        var opts = new QrEasyOptions {
            TargetSizePx = 4000,
            TargetSizeIncludesQuietZone = true,
            BackgroundSupersample = 2
        };

        QR.Save(payload, Path.Combine(outputDir, "qr-print-4k.png"), opts);

        opts.TargetSizePx = 8000;
        QR.Save(payload, Path.Combine(outputDir, "qr-print-8k.png"), opts);
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-print-8k.pdf"),
            QrCode.Render(payload, OutputFormat.Pdf, opts, new RenderExtras { VectorMode = RenderMode.Raster })
        );
    }
}
