using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class CodeGlyphDecodeExample {
    public static void Run(string outputDir) {
        var qrPng = QrCode.Render("Auto decode QR", OutputFormat.Png).Data;
        if (CodeGlyph.TryDecodePng(qrPng, out var qrDecoded)) {
            qrDecoded.Text.WriteText(outputDir, "decode-any-qr.txt");
        }

        var qr = QR.Encode("Decode from pixels");
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4
        }, out var width, out var height, out var stride);
        if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var pixelDecoded)) {
            pixelDecoded.Text.WriteText(outputDir, "decode-any-pixels.txt");
        }
        if (CodeGlyph.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var allDecoded)) {
            for (var i = 0; i < allDecoded.Length; i++) {
                var suffix = i + 1;
                allDecoded[i].Text.WriteText(outputDir, $"decode-all-pixels-{suffix}.txt");
            }
        }

        var barcodePng = Barcode.Render(BarcodeType.Code128, "CODE128-ANY", OutputFormat.Png, new BarcodeOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }).Data;
        if (CodeGlyph.TryDecodePng(barcodePng, out var barcodeDecoded, expectedBarcode: BarcodeType.Code128, preferBarcode: true)) {
            barcodeDecoded.Text.WriteText(outputDir, "decode-any-barcode.txt");
        }
    }
}
