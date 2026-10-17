---
title: QR and barcode assets for documents
description: Generate and verify reusable QR, Code 128 and Data Matrix images for reports, inventory sheets and labels.
---

# QR and barcode assets for documents

An equipment report can use a QR code to open a service record, a Code 128 barcode for the asset identifier and a Data Matrix code for its manufacturing lot. Generate each symbol once, then reuse its images in documents and labels.

CodeGlyphX owns encoding, rendering and image scanning. The document library owns placement, captions and page layout. PNG works with ordinary document image APIs; SVG suits consumers with vector support.

{{< cgx-figure id="document-assets" caption="The three label images generated below, with payloads decoded from their PNG output." >}}

## Generate the label assets

Create a console project and install CodeGlyphX:

```bash
dotnet new console -n DocumentCodes
cd DocumentCodes
dotnet add package CodeGlyphX
```

Replace `Program.cs` with this code. Each symbol is encoded once and rendered as both PNG and SVG:

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

string outputDir = "document-codes";
Directory.CreateDirectory(outputDir);

var qr = QR.Encode("https://example.org/assets/AT-1042", new QrEncodingOptions {
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H
});
var qrAppearance = new QrRenderOptions { ModuleSize = 6, QuietZone = 4 };
var qrPng = qr.Render(OutputFormat.Png, qrAppearance);
OutputWriter.Write(Path.Combine(outputDir, "asset-service.png"), qrPng);
qr.Save(Path.Combine(outputDir, "asset-service.svg"), qrAppearance);

var inventory = Barcode.Encode(SymbolFormat.Code128, "AT-1042");
var barcodeAppearance = new BarcodeOptions {
    ModuleSize = 2, HeightModules = 60, QuietZone = 10
};
inventory.Save(Path.Combine(outputDir, "asset-inventory.png"), barcodeAppearance);
inventory.Save(Path.Combine(outputDir, "asset-inventory.svg"), barcodeAppearance);

var part = MatrixBarcode.Encode(SymbolFormat.DataMatrix, "LOT-2026-1042");
var matrixAppearance = new MatrixOptions { ModuleSize = 6, QuietZone = 2 };
part.Save(Path.Combine(outputDir, "part-label.png"), matrixAppearance);
part.Save(Path.Combine(outputDir, "part-label.svg"), matrixAppearance);
```

Run `dotnet run` to create six files. Module size controls raster detail; the document's image size determines the printed width. Preserve the quiet zones and aspect ratio when placing the images. Avoid JPEG compression for barcodes.

## Verify the files you wrote

Append this code to scan each PNG and compare its decoded payload with the expected value:

```csharp
var labels = new[] {
    ("asset-service.png", SymbolFormat.QrCode, "https://example.org/assets/AT-1042"),
    ("asset-inventory.png", SymbolFormat.Code128, "AT-1042"),
    ("part-label.png", SymbolFormat.DataMatrix, "LOT-2026-1042")
};

foreach (var (file, format, expected) in labels) {
    var scan = SymbolScanner.ScanFile(Path.Combine(outputDir, file), new ScanOptions {
        Formats = new[] { format },
        TimeoutMilliseconds = 5000,
        MaxSymbols = 1,
        EnableTileScan = false
    });
    if (!scan.IsSuccess || scan.Symbols.Count != 1 ||
        scan.Symbols[0].Text != expected ||
        scan.CompletionReason is ScanCompletionReason.Cancelled or ScanCompletionReason.DeadlineExceeded) {
        throw new InvalidOperationException($"{file}: {scan.Status}, {scan.CompletionReason}, {scan.Failure}");
    }
    Console.WriteLine($"{file}: {scan.Symbols[0].Text} ({scan.CompletionReason})");
}
```

`MaxSymbols = 1` requests the first result, so success can report `SymbolLimitReached`. It does not prove that the image contains only one symbol. For a sheet with several labels, see the bounded scanning options in [Image decoding](/docs/decoding/).

Scan PNG or another supported raster format. SVG is an output format; DOCX and XLSX are document containers. Extract their embedded raster images before scanning, or render the saved document when you need to check placement as well as payload preservation.

## Hand off bytes or a stream

An image API that accepts `byte[]` can use `qrPng.ToArray()`. That returns a mutable copy; `qrPng.Data` remains a read-only view. For a stream-based API:

```csharp
using var imageStream = new MemoryStream();
OutputWriter.Write(imageStream, qrPng);
imageStream.Position = 0;
// Pass imageStream to the document library's image insertion method.
```

Writing leaves the caller-owned stream open at the end of the image. Rewind it before the next reader. `SymbolScanner.Scan(imageStream, options)` likewise reads from the current position and leaves the stream open.

## Run the complete asset example

The [repository example](https://github.com/EvotecIT/CodeGlyphX/blob/master/CodeGlyphX.Examples/DocumentCodeAssetsExample.cs) generates the six files and verifies PNG files, bytes and a reused stream. From a CodeGlyphX checkout:

```powershell
$env:CODEGLYPHX_DOCUMENT_CODES = '1'
dotnet run --project CodeGlyphX.Examples
```

On macOS or Linux:

```bash
CODEGLYPHX_DOCUMENT_CODES=1 dotnet run --project CodeGlyphX.Examples
```

The runner prints its output directory; `CODEGLYPHX_OUTPUT_DIR` selects another location.

Continue with [OfficeIMO documents](/docs/office-documents/) for C# image insertion, or [PowerShell documents](/docs/powershell-documents/) to generate and place codes with ImagePlayground and PSWriteOffice. Check the final display or print with the scanners used in your workflow; reading the original PNG alone does not establish print quality.
