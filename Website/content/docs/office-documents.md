---
title: QR codes and barcodes in OfficeIMO documents
description: Place CodeGlyphX label images into Word, Excel, PowerPoint and PDF with OfficeIMO's image APIs.
---

# QR codes and barcodes in OfficeIMO documents

Use [OfficeIMO](https://github.com/EvotecIT/OfficeIMO) to place CodeGlyphX images in reports, asset registers and handover documents. The same PNG files work through each format's normal image API. Neither library needs to take over the other's job.

First run the [document label assets example](/docs/document-codes/). The snippets below continue in that `DocumentCodes` project and use its `outputDir` folder, containing the service QR, inventory barcode and lot label.

| Document | Package | Image entry point | Size units |
| --- | --- | --- | --- |
| Word | `OfficeIMO.Word` | `WordParagraph.InsertImage` | Pixels at 96 pixels per inch |
| Excel | `OfficeIMO.Excel` | `ExcelSheet.AddImage` | Pixels; row height uses points |
| PowerPoint | `OfficeIMO.PowerPoint` | `PowerPointSlide.AddPicturePoints` | Points |
| PDF | `OfficeIMO.Pdf` | `PdfContentBuilder.Image` | Points |

These examples insert raster images. A code's quiet zone is part of its image: keep the complete bounds and preserve its aspect ratio. One inch is 96 pixels in the Word/Excel image calls or 72 points in the PowerPoint/PDF calls.

## Create a Word dispatch pack

Install the Word package:

```bash
dotnet add package OfficeIMO.Word
```

Add `using OfficeIMO.Word;` at the top of `Program.cs`, then append:

```csharp
const string serviceLink = "https://example.org/assets/AT-1042";
string wordPath = Path.Combine(outputDir, "dispatch-pack.docx");
using (WordDocument document = WordDocument.Create(wordPath)) {
    document.AddParagraph("Asset AT-1042 dispatch pack").Bold = true;
    document.AddParagraph("Service record: " + serviceLink);
    var image = document.AddParagraph().InsertImage(
        Path.Combine(outputDir, "asset-service.png"),
        width: 144, height: 144,
        description: "Open the service record for asset AT-1042.");
    image.Title = "Asset service record";
    document.AddParagraph("Inventory identifier: AT-1042");
    document.AddParagraph().InsertImage(
        Path.Combine(outputDir, "asset-inventory.png"), width: 288);
    document.Save();
}
```

The square QR is placed at 1.5 inches (38.1 mm), including its quiet zone. The barcode supplies only a width so that Word preserves the image's aspect ratio. Keep the link and identifier as ordinary text as well as encoded content.

### Verify the embedded images

Append this code to reopen the DOCX and scan its image bytes:

```csharp
using (WordDocument saved = WordDocument.Load(wordPath)) {
    var expected = new HashSet<string> { serviceLink, "AT-1042" };
    foreach (byte[] embedded in saved.GetImageBytes()) {
        var scan = SymbolScanner.Scan(embedded, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode, SymbolFormat.Code128 },
            TimeoutMilliseconds = 5000,
            MaxSymbols = 1,
            EnableTileScan = false
        });
        if (!scan.IsSuccess || scan.Symbols.Count != 1 ||
            scan.CompletionReason is ScanCompletionReason.Cancelled or ScanCompletionReason.DeadlineExceeded ||
            !expected.Remove(scan.Symbols[0].Text)) {
            throw new InvalidOperationException("An embedded document code did not verify.");
        }
    }
    if (expected.Count != 0) throw new InvalidOperationException("A document code is missing.");
}
```

This checks the saved image payloads. It does not check page clipping, image resampling by another editor or scanner performance after printing; inspect the saved document too.

## Create an Excel asset register

Install `OfficeIMO.Excel` and add `using OfficeIMO.Excel;` to `Program.cs`:

```bash
dotnet add package OfficeIMO.Excel
```

```csharp
using (ExcelDocument document = ExcelDocument.Create(Path.Combine(outputDir, "asset-register.xlsx"))) {
    ExcelSheet sheet = document.AddWorksheet("Assets");
    sheet.CellValue(1, 1, "Asset / lot");
    sheet.CellValue(1, 2, "Service record");
    sheet.CellValue(1, 3, "Manufacturing lot");
    sheet.CellValue(2, 1, "AT-1042 / LOT-2026-1042");
    sheet.SetColumnWidth(1, 34);
    sheet.SetColumnWidth(2, 26);
    sheet.SetColumnWidth(3, 26);
    sheet.SetRowHeight(2, 120);
    sheet.AddImage(2, 2, File.ReadAllBytes(Path.Combine(outputDir, "asset-service.png")),
        widthPixels: 144, heightPixels: 144, altText: "Asset AT-1042 service record");
    sheet.AddImage(2, 3, File.ReadAllBytes(Path.Combine(outputDir, "part-label.png")),
        widthPixels: 144, heightPixels: 144, altText: "Lot LOT-2026-1042");
    document.Save();
}
```

Rows and columns are one-based. The pictures are anchored to cells; they do not become cell values or automatically enlarge the row. Here the row is 120 points tall, leaving room for each 108-point image. For a linear barcode, supply dimensions with the source image's width-to-height ratio.

## Add a QR code to a PowerPoint handover

Install `OfficeIMO.PowerPoint` and add `using OfficeIMO.PowerPoint;`:

```bash
dotnet add package OfficeIMO.PowerPoint
```

```csharp
using (PowerPointPresentation presentation = PowerPointPresentation.Create(
    Path.Combine(outputDir, "handover.pptx"))) {
    PowerPointSlide slide = presentation.AddSlide();
    slide.AddTitlePoints("Asset AT-1042 service handover", 36, 24, 620, 42);
    slide.AddTextBoxPoints(serviceLink, 36, 76, 620, 28);
    var picture = slide.AddPicturePoints(Path.Combine(outputDir, "asset-service.png"),
        leftPoints: 36, topPoints: 120, widthPoints: 108, heightPoints: 108);
    picture.Title = "Asset service record";
    presentation.Save();
}
```

`AddPicturePoints` makes the coordinate units explicit. The returned `PowerPointPicture` supports the document library's normal picture settings.

## Put the same QR image in a PDF

Install `OfficeIMO.Pdf` and add `using OfficeIMO.Pdf;`:

```bash
dotnet add package OfficeIMO.Pdf
```

```csharp
PdfDocument.Create(pdf => pdf.Content(content => content
    .H1("Asset AT-1042 dispatch pack")
    .Paragraph(p => p.Text("Service record: " + serviceLink))
    .Image(File.ReadAllBytes(Path.Combine(outputDir, "asset-service.png")),
        108, 108, "Open the service record for asset AT-1042.")))
    .Save(Path.Combine(outputDir, "dispatch-pack.pdf"));
```

This embeds the PNG at 108 by 108 points. For a vector workflow, the optional [OfficeIMO.Drawing.CodeGlyphX](https://github.com/EvotecIT/OfficeIMO/tree/master/OfficeIMO.Drawing.CodeGlyphX) bridge converts encoded QR codes, matrix modules and linear barcodes to reusable `OfficeDrawing` scenes with `ToOfficeDrawing`. Its README describes supported SVG features and fallback reporting; use the document owner's drawing APIs for placement.

For script-based document creation, continue with [PowerShell documents](/docs/powershell-documents/).
