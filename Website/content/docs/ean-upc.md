---
title: EAN / UPC - CodeGlyphX
description: EAN and UPC formats overview.
slug: ean-upc
collection: docs
layout: docs
---

{{< edit-link >}}

# EAN / UPC Barcodes

EAN (European Article Number) and UPC (Universal Product Code) are the standard retail barcodes found on consumer products worldwide.

## Basic Usage

```csharp
using CodeGlyphX;

// EAN-13 (International)
Barcode.Save(BarcodeType.EAN, "5901234123457", "ean13.png");

// EAN-8 (Smaller packages)
Barcode.Save(BarcodeType.EAN, "96385074", "ean8.png");

// UPC-A (North America)
Barcode.Save(BarcodeType.UPCA, "012345678905", "upca.png");

// UPC-E (Compact)
Barcode.Save(BarcodeType.UPCE, "01234565", "upce.png");
```

## Format Guide

| Type | Digits | Region | Use Case |
| --- | --- | --- | --- |
| EAN-13 | 13 | International | Standard retail products |
| EAN-8 | 8 | International | Small packages |
| UPC-A | 12 | North America | Retail products |
| UPC-E | 8 | North America | Small items |
