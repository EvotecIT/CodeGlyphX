// Home-page print specimens: measured labels with the bundled OCR faces, exported to PNG, SVG and PDF from one layout.
// Each PNG is scanned back, so the page only shows labels that CodeGlyphX reads.
using CodeGlyphX;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Website.Specimens;

internal static class PrintSpecimens {
    public static List<object> Render(string output) {
        // EAN digit groups have fixed reserved spans, so the retail label uses the smaller size from the library's print example.
        // Code 39 adds an optional mod-43 check by default; the label encodes only its value so the caption and decode match.
        var labels = new (string Id, string Name, string Font, BarcodeFont Face, double Points, SymbolFormat Format, string Value, BarcodeEncodingOptions? Encoding)[] {
            ("ean13", "EAN-13", "CodeGlyphX Scan B (OCR-B)", BarcodeFont.OcrB, 8, SymbolFormat.Ean, "5901234123457", null),
            ("code39", "Code 39", "GG Ocra (OCR-A)", BarcodeFont.OcrA, 9, SymbolFormat.Code39, "CGX-2026", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.None }),
        };
        var results = new List<object>();
        foreach (var label in labels) {
            var options = new BarcodePrintOptions { Dpi = 300, Font = label.Face, FontSizePoints = label.Points, IncludeHumanReadable = true };
            var layout = label.Encoding is null
                ? BarcodePrint.Layout(Barcode.Encode(label.Format, label.Value), options)
                : Barcode.Create(label.Format, label.Value).WithEncodingOptions(label.Encoding).LayoutPrint(options);
            var errors = layout.Findings.Concat(layout.Preflight(format: OutputFormat.Pdf)).Where(f => f.IsError).ToList();
            if (layout.HasErrors || errors.Count > 0)
                throw new InvalidOperationException($"The {label.Name} print specimen failed preflight: {string.Join("; ", errors.Select(e => e.Code + " " + e.Message))}");

            var png = layout.Render(OutputFormat.Png).ToArray();
            var scan = SymbolScanner.Scan(png, new ScanOptions {
                Formats = new[] { label.Format }, MaxSymbols = 1,
                EnableTileScan = false, TimeoutMilliseconds = 5000
            });
            if (!scan.IsSuccess || scan.Symbols.Count != 1 || scan.Symbols[0].Text != label.Value)
                throw new InvalidOperationException($"The {label.Name} print specimen did not decode to {label.Value}: {scan.CompletionReason}, " +
                    $"[{string.Join(", ", scan.Symbols.Select(s => s.Format + " '" + s.Text + "'"))}].");

            var file = "print-" + label.Id;
            File.WriteAllBytes(Path.Combine(output, file + ".png"), png);
            layout.Save(Path.Combine(output, file + ".svg"));
            layout.Save(Path.Combine(output, file + ".pdf"));
            results.Add(new {
                id = label.Id, name = label.Name, font = label.Font, value = label.Value, decoded = scan.Symbols[0].Text,
                dpi = layout.Dpi,
                widthMm = Math.Round(layout.Bounds.Width, 1),
                heightMm = Math.Round(layout.Bounds.Height, 1),
                svg = "/specimens/" + file + ".svg", png = "/specimens/" + file + ".png", pdf = "/specimens/" + file + ".pdf"
            });
            Console.WriteLine($"{file}: {layout.Bounds.Width:0.#} x {layout.Bounds.Height:0.#} mm at {layout.Dpi} DPI, decoded {scan.Symbols[0].Text}");
        }
        return results;
    }
}
