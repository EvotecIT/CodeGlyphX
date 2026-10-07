using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class CodeGlyphDecodeExample {
    public static void Run(string outputDir) {
        var qrPng = QR.Render("Auto decode QR", OutputFormat.Png).ToArray();
        var imageScan = SymbolScanner.Scan(qrPng, ScanOptions.Balanced(2000));
        foreach (var symbol in imageScan.Symbols) {
            symbol.Text.WriteText(outputDir, "decode-any-qr.txt");
        }

        var pixels = QR.RenderPixels("Decode from pixels", out var width, out var height, out var stride);
        var frame = new ImageFrame(pixels, width, height, stride, PixelFormat.Rgba32);
        var pixelScan = SymbolScanner.Scan(frame, new ScanOptions { MaxSymbols = 16, TimeoutMilliseconds = 2000 });
        for (var i = 0; i < pixelScan.Symbols.Count; i++) {
            pixelScan.Symbols[i].Text.WriteText(outputDir, $"decode-all-pixels-{i + 1}.txt");
        }

        var barcodePng = Barcode.Render(SymbolFormat.Code128, "CODE128-ANY", OutputFormat.Png,
            new BarcodeOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 40 }).ToArray();
        using var input = new System.IO.MemoryStream(barcodePng);
        var barcodeScan = SymbolScanner.Scan(input, new ScanOptions {
            Formats = new[] { SymbolFormat.Code128 },
            TimeoutMilliseconds = 2000,
            MaxSymbols = 1
        });
        if (barcodeScan.Symbols.Count > 0) {
            barcodeScan.Symbols[0].Text.WriteText(outputDir, "decode-any-barcode.txt");
        }
    }
}
