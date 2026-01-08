using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class BarcodeExample {
    public static void Run(string outputDir) {
        var options = new BarcodeOptions {
            ModuleSize = 2,
            QuietZone = 10,
            HeightModules = 40,
            Foreground = new Rgba32(15, 24, 39),
            Background = new Rgba32(255, 255, 255),
        };

        Barcode.SavePng(BarcodeType.Code128, "CODEMATRIX-123456", Path.Combine(outputDir, "barcode-code128.png"), options);
        Barcode.SaveSvg(BarcodeType.Code128, "CODEMATRIX-123456", Path.Combine(outputDir, "barcode-code128.svg"), options);
        Barcode.SaveHtml(BarcodeType.Code128, "CODEMATRIX-123456", Path.Combine(outputDir, "barcode-code128.html"), options, title: "Code128");
        Barcode.SaveJpeg(BarcodeType.Code128, "CODEMATRIX-123456", Path.Combine(outputDir, "barcode-code128.jpg"), options);
    }
}
