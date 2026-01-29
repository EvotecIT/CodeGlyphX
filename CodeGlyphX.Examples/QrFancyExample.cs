using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrFancyExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/fancy");
        var logoPng = LogoBuilder.CreateCirclePng(48, new Rgba32(12, 44, 96), new Rgba32(235, 192, 92), out _, out _);

        var options = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            ModuleSize = 8,
            Background = new Rgba32(250, 252, 255),
            Foreground = new Rgba32(18, 44, 78),
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.88,
            LogoPng = logoPng,
            LogoScale = 0.22,
            LogoPaddingPx = 6,
        };

        QR.Save(payload, Path.Combine(outputDir, "qr-fancy.png"), options);
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-fancy.pdf"),
            QrCode.Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = RenderMode.Raster })
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-fancy.eps"),
            QrCode.Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = RenderMode.Raster })
        );
    }
}
