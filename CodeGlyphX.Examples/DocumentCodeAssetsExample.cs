using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>
/// Creates reusable report images and verifies their payloads through the image scanner.
/// </summary>
internal static class DocumentCodeAssetsExample {
    private const string ServiceLink = "https://example.org/assets/AT-1042";
    private const string AssetId = "AT-1042";
    private const string PartLabel = "LOT-2026-1042";

    public static void Run(string outputDir) {
        Directory.CreateDirectory(outputDir);

        var qr = QR.Encode(ServiceLink, new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H
        });
        var qrAppearance = new QrRenderOptions { ModuleSize = 6, QuietZone = 4 };
        var qrPng = qr.Render(OutputFormat.Png, qrAppearance);
        string qrPath = OutputWriter.Write(Path.Combine(outputDir, "asset-service.png"), qrPng);
        qr.Save(Path.Combine(outputDir, "asset-service.svg"), qrAppearance);

        var inventory = Barcode.Encode(SymbolFormat.Code128, AssetId);
        var barcodeAppearance = new BarcodeOptions { ModuleSize = 2, HeightModules = 60, QuietZone = 10 };
        string inventoryPath = inventory.Save(Path.Combine(outputDir, "asset-inventory.png"), barcodeAppearance);
        inventory.Save(Path.Combine(outputDir, "asset-inventory.svg"), barcodeAppearance);

        var part = MatrixBarcode.Encode(SymbolFormat.DataMatrix, PartLabel);
        var matrixAppearance = new MatrixOptions { ModuleSize = 6, QuietZone = 2 };
        string partPath = part.Save(Path.Combine(outputDir, "part-label.png"), matrixAppearance);
        part.Save(Path.Combine(outputDir, "part-label.svg"), matrixAppearance);

        Verify(SymbolScanner.ScanFile(qrPath, Options(SymbolFormat.QrCode)), SymbolFormat.QrCode, ServiceLink);
        Verify(SymbolScanner.ScanFile(inventoryPath, Options(SymbolFormat.Code128)), SymbolFormat.Code128, AssetId);
        Verify(SymbolScanner.ScanFile(partPath, Options(SymbolFormat.DataMatrix)), SymbolFormat.DataMatrix, PartLabel);

        // Image APIs can take an owned byte array; a reused stream must be rewound before reading.
        byte[] qrBytes = qrPng.ToArray();
        Verify(SymbolScanner.Scan(qrBytes, Options(SymbolFormat.QrCode)), SymbolFormat.QrCode, ServiceLink);
        using var stream = new MemoryStream();
        OutputWriter.Write(stream, qrPng);
        stream.Position = 0;
        Verify(SymbolScanner.Scan(stream, Options(SymbolFormat.QrCode)), SymbolFormat.QrCode, ServiceLink);
        if (!stream.CanRead) throw new InvalidOperationException("Scanning closed the caller-owned stream.");
    }

    private static ScanOptions Options(SymbolFormat format) => new() {
        Formats = new[] { format },
        TimeoutMilliseconds = 5000,
        MaxSymbols = 1,
        EnableTileScan = false
    };

    private static void Verify(ScanResult scan, SymbolFormat format, string payload) {
        if (!scan.IsSuccess || scan.Symbols.Count != 1 || scan.Symbols[0].Format != format ||
            scan.Symbols[0].Text != payload ||
            scan.CompletionReason is ScanCompletionReason.Cancelled or ScanCompletionReason.DeadlineExceeded) {
            throw new InvalidOperationException($"The {format} image did not verify: {scan.Status}, {scan.CompletionReason}, {scan.Failure}");
        }
        Console.WriteLine($"Verified {format}: {scan.Symbols[0].Text} ({scan.CompletionReason})");
    }
}
