using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Website.Specimens;

internal static partial class SiteVisuals {
    /// <summary>Draws the document-guide assets and verifies the payloads used in their captions.</summary>
    static Figure BuildDocumentAssetsFigure() {
        const string service = "https://example.org/assets/AT-1042";
        var qr = QR.Encode(service, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var assets = new[] {
            (File: "service", Label: "Service record", Value: service, Format: SymbolFormat.QrCode,
                Bytes: qr.Render(OutputFormat.Png, new QrRenderOptions { ModuleSize = 6, QuietZone = 4 }).ToArray()),
            (File: "inventory", Label: "Asset identifier", Value: "AT-1042", Format: SymbolFormat.Code128,
                Bytes: Barcode.Render(SymbolFormat.Code128, "AT-1042", OutputFormat.Png,
                    new BarcodeOptions { ModuleSize = 2, HeightModules = 60, QuietZone = 10 }).ToArray()),
            (File: "lot", Label: "Manufacturing lot", Value: "LOT-2026-1042", Format: SymbolFormat.DataMatrix,
                Bytes: MatrixBarcode.Render(SymbolFormat.DataMatrix, "LOT-2026-1042", OutputFormat.Png,
                    new MatrixOptions { ModuleSize = 6, QuietZone = 2 }).ToArray())
        };
        var items = new List<FigItem>();
        foreach (var asset in assets) {
            var scan = SymbolScanner.Scan(asset.Bytes, new ScanOptions {
                Formats = new[] { asset.Format }, MaxSymbols = 1,
                EnableTileScan = false, TimeoutMilliseconds = 5000
            });
            if (!scan.IsSuccess || scan.Symbols.Count != 1 || scan.Symbols[0].Text != asset.Value ||
                scan.CompletionReason is ScanCompletionReason.Cancelled or ScanCompletionReason.DeadlineExceeded) {
                throw new InvalidOperationException($"The document figure {asset.File} did not decode to its caption.");
            }
            items.Add(Item(SavePng($"docs/document-{asset.File}.png", asset.Bytes),
                asset.Label, asset.Format.ToString(), scan.Symbols[0].Text));
        }
        return new Figure("row", items);
    }
}
