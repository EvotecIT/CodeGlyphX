---
title: QR codes and barcodes in PowerShell documents
description: Generate code images with ImagePlayground and place them in Word, Excel, PowerPoint and PDF using PSWriteOffice.
---

# QR codes and barcodes in PowerShell documents

Use ImagePlayground to generate the codes, then PSWriteOffice to place them in documents. PSWriteOffice's existing visual commands accept SVG file paths, so the generated symbols can share the same placement workflow as other visuals.

Install the modules once if needed:

```powershell
Install-Module ImagePlayground, PSWriteOffice -Scope CurrentUser
```

## Generate reusable SVG files

Run this in a new PowerShell session. It writes a service-link QR code and a Code 128 asset identifier under `asset-documents` in the current directory:

```powershell
$ErrorActionPreference = 'Stop'
Import-Module ImagePlayground
Import-Module PSWriteOffice

$outputDir = Join-Path $PWD 'asset-documents'
$null = New-Item -ItemType Directory -Path $outputDir -Force
$qrPath = Join-Path $outputDir 'asset-service.svg'
$barcodePath = Join-Path $outputDir 'asset-inventory.svg'

New-ImageQRCode -Content 'https://example.org/assets/AT-1042' `
    -ErrorCorrection H -QuietZone 4 -FilePath $qrPath
New-ImageBarcode -Type Code128 -Value 'AT-1042' `
    -ModuleSize 2 -QuietZone 10 -Height 60 -FilePath $barcodePath

$qrVisual = $qrPath | ConvertTo-OfficeVisual -Width 108 `
    -Title 'Asset service record' `
    -AlternativeText 'Open the service record for asset AT-1042.'
$inventoryVisual = $barcodePath | ConvertTo-OfficeVisual -Width 216 `
    -AlternativeText 'Code 128 asset identifier AT-1042.'
```

`ConvertTo-OfficeVisual` prepares the SVG for reuse and records a conversion report. Width is in points, including the code's quiet zone; 108 points is 1.5 inches. Supplying only width preserves the source aspect ratio. The source's module size controls rendering detail, while the visual width controls its physical size in the document.

## Create a Word dispatch pack

Append this to create `AssetDispatch.docx`:

```powershell
New-OfficeWord -Path (Join-Path $outputDir 'AssetDispatch.docx') {
    WordSection {
        WordParagraph { WordText 'Asset AT-1042 dispatch pack' }
        WordParagraph { WordText 'Service record: https://example.org/assets/AT-1042' }
        WordParagraph { $qrVisual | Add-OfficeWordVisual }
        WordParagraph { WordText 'Inventory identifier: AT-1042' }
        WordParagraph { $inventoryVisual | Add-OfficeWordVisual }
    }
}
```

Keep the service URL and identifier readable as ordinary text. Avoid cropping quiet zones or stretching square symbols. The Word visual command handles image representation and placement; it does not encode the payload again.

## Reuse the visuals in Excel, PowerPoint and PDF

The prepared visuals can be passed to the other document commands:

```powershell
New-OfficeExcel -Path (Join-Path $outputDir 'AssetRegister.xlsx') {
    Add-OfficeExcelSheet -Name Assets -Content {
        $qrVisual | Add-OfficeExcelVisual -Address B2
        $inventoryVisual | Add-OfficeExcelVisual -Address B12
    }
}

New-OfficePowerPoint -Path (Join-Path $outputDir 'AssetHandover.pptx') {
    PptSlide {
        $qrVisual | Add-OfficePowerPointVisual -X 36 -Y 72
        $inventoryVisual | Add-OfficePowerPointVisual -X 36 -Y 220
    }
}

New-OfficePdf -Path (Join-Path $outputDir 'AssetDispatch.pdf') {
    $qrVisual | Add-OfficePdfVisual
    $inventoryVisual | Add-OfficePdfVisual
}
```

Excel uses a cell anchor; PowerPoint positions and visual sizes are in points. These calls do not add worksheet headings or choose row heights for a finished register. Add the normal document layout around the images, and inspect the saved files for overlap and clipping.

## Check the handoff

Inspect the conversion reports before distributing the documents:

```powershell
$qrVisual.Report
$inventoryVisual.Report
```

A conversion report describes the visual handoff, not a barcode scan. To verify saved document payloads, extract their raster images and scan those bytes as shown in [OfficeIMO documents](/docs/office-documents/). SVG files and document containers are not image-scanner inputs. If an embedded asset remains SVG, render it or the saved document through the document library before scanning. Test the final display or print with the readers used in your workflow.

For the underlying C# generation and stream APIs, see [Document label assets](/docs/document-codes/).
