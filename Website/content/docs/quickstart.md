---
title: Quick Start - CodeGlyphX
description: Get started with CodeGlyphX in minutes.
slug: quickstart
collection: docs
layout: docs
---

{{< edit-link >}}

# Quick Start

Get up and running with CodeGlyphX in under a minute.

## 1. Install the Package

```bash
dotnet add package CodeGlyphX
```

## 2. Generate Your First QR Code

```csharp
using CodeGlyphX;

// Create a QR code and save to file
QR.Save("Hello, World!", "hello.png");

// The output format is determined by the file extension
QR.Save("Hello, World!", "hello.svg");  // Vector SVG
QR.Save("Hello, World!", "hello.pdf");  // PDF document
```

### Render In-Memory

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var svg = Barcode.Render(BarcodeType.Code128, "PRODUCT-12345", OutputFormat.Svg).GetText();
var png = QrCode.Render("Hello, World!", OutputFormat.Png).Data;

// HTML title + raster PDF/EPS
var extras = new RenderExtras { HtmlTitle = "My Code", VectorMode = RenderMode.Raster };
QR.Save("Hello, World!", "hello.html", extras: extras);
```

## 3. Generate Barcodes

```csharp
using CodeGlyphX;

// Code 128 barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "barcode.png");

// EAN-13 (retail products)
Barcode.Save(BarcodeType.EAN, "5901234123457", "ean.png");
```

## 4. Decode Images

```csharp
using CodeGlyphX;

var imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
}
```
