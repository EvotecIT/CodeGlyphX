using CodeMatrix;
using CodeMatrix.Rendering.Html;
using CodeMatrix.Rendering.Jpeg;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Rendering.Svg;

namespace CodeMatrix.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codematrix?from=examples";
        var qr = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.M, 1, 10, null);

        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = new Rgba32(15, 24, 39),
            Background = new Rgba32(255, 255, 255),
        });
        ExampleHelpers.WriteBinary(outputDir, "qr-basic.png", png);

        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            DarkColor = "#0f1827",
            LightColor = "#ffffff",
        });
        ExampleHelpers.WriteText(outputDir, "qr-basic.svg", svg);

        var html = HtmlQrRenderer.Render(qr.Modules, new QrHtmlRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            DarkColor = "#0f1827",
            LightColor = "#ffffff",
        });
        ExampleHelpers.WriteText(outputDir, "qr-basic.html", ExampleHelpers.WrapHtml("CodeMatrix QR", html));

        var jpeg = QrJpegRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = new Rgba32(15, 24, 39),
            Background = new Rgba32(255, 255, 255),
        }, quality: 90);
        ExampleHelpers.WriteBinary(outputDir, "qr-basic.jpg", jpeg);
    }
}
