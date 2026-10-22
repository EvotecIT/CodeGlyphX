using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>Portable bundled or supplied-font labels and the newer matrix, colour and linked formats.</summary>
internal static class BarcodePrintExample {
    internal static void Run(string outputDir) {
        var fontPath = Environment.GetEnvironmentVariable("CODEGLYPHX_PRINT_FONT");
        var font = string.IsNullOrWhiteSpace(fontPath) ? BarcodeFont.OcrB : BarcodeFont.FromBytes(File.ReadAllBytes(fontPath));
        var options = new BarcodePrintOptions { Dpi = 300, Font = font, FontSizePoints = 8, IncludeHumanReadable = true };
        var labels = new (string Name, BarcodePrintLayout Layout)[] {
            ("retail-print", BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, "5901234123457+12"), options)),
            ("code39-print", Barcode.Create(SymbolFormat.Code39, "aX").WithEncodingOptions(new BarcodeEncodingOptions {
                Code39FullAscii = true, CheckMode = BarcodeCheckMode.Compute, PrintCheckCharacters = false
            }).LayoutPrint(options)),
            ("rune-print", BarcodePrint.Layout(AztecRune.Encode(42), options)),
            ("code-one-print", BarcodePrint.Layout(CodeOneEncoder.EncodeText("CODE ONE LABEL"), options)),
            ("ultracode-print", BarcodePrint.Layout(UltracodeEncoder.EncodeText("COLOUR LABEL"), options)),
            ("composite-print", BarcodePrint.Layout(Gs1CompositeEncoder.Encode("5901234123457", "(10)ABC123", new Gs1CompositeEncodingOptions {
                Carrier = Gs1CompositeCarrier.Ean13
            }), options))
        };
        foreach (var label in labels) {
            foreach (var finding in label.Layout.Preflight(format: OutputFormat.Pdf))
                Console.WriteLine($"{label.Name}: {finding.Code}: {finding.Message}");
            label.Layout.Save(Path.Combine(outputDir, label.Name + ".png"));
            label.Layout.Save(Path.Combine(outputDir, label.Name + ".svg"));
            label.Layout.Save(Path.Combine(outputDir, label.Name + ".pdf"));
            Console.WriteLine($"{label.Name}: {label.Layout.Bounds.Width:0.##} x {label.Layout.Bounds.Height:0.##} mm");
        }
        var codeOne = SymbolScanner.Scan(File.ReadAllBytes(Path.Combine(outputDir, "code-one-print.png")), new ScanOptions {
            Formats = new[] { SymbolFormat.CodeOne }, TimeoutMilliseconds = 5000
        });
        if (codeOne.Symbols.Count != 1 || codeOne.Symbols[0].Text != "CODE ONE LABEL")
            throw new InvalidOperationException("The printed Code One example did not decode.");
    }
}
