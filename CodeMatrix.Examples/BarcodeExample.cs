using CodeMatrix;
using CodeMatrix.Rendering.Html;
using CodeMatrix.Rendering.Jpeg;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Rendering.Svg;

namespace CodeMatrix.Examples;

internal static class BarcodeExample {
    public static void Run(string outputDir) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CODEMATRIX-123456");

        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 2,
            QuietZone = 10,
            HeightModules = 40,
            Foreground = new Rgba32(15, 24, 39),
            Background = new Rgba32(255, 255, 255),
        });
        ExampleHelpers.WriteBinary(outputDir, "barcode-code128.png", png);

        var svg = SvgBarcodeRenderer.Render(barcode, new BarcodeSvgRenderOptions {
            ModuleSize = 2,
            QuietZone = 10,
            HeightModules = 40,
            BarColor = "#0f1827",
            BackgroundColor = "#ffffff",
        });
        ExampleHelpers.WriteText(outputDir, "barcode-code128.svg", svg);

        var html = HtmlBarcodeRenderer.Render(barcode, new BarcodeHtmlRenderOptions {
            ModuleSize = 2,
            QuietZone = 10,
            HeightModules = 40,
            BarColor = "#0f1827",
            BackgroundColor = "#ffffff",
        });
        ExampleHelpers.WriteText(outputDir, "barcode-code128.html", ExampleHelpers.WrapHtml("Code128", html));

        var jpeg = BarcodeJpegRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 2,
            QuietZone = 10,
            HeightModules = 40,
            Foreground = new Rgba32(15, 24, 39),
            Background = new Rgba32(255, 255, 255),
        }, quality: 90);
        ExampleHelpers.WriteBinary(outputDir, "barcode-code128.jpg", jpeg);
    }
}
