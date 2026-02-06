---
title: Code 128 - CodeGlyphX
description: Code 128 / GS1-128 usage and character sets.
slug: code128
collection: docs
layout: docs
---

{{< edit-link >}}

# Code 128 / GS1-128

Code 128 is a high-density linear barcode supporting the full ASCII character set. GS1-128 is an application standard that uses Code 128.

## Basic Usage

```csharp
using CodeGlyphX;

// Code 128
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "code128.png");

// GS1-128 with Application Identifiers
Barcode.Save(BarcodeType.Gs1128, "(01)09501101530003(17)250101", "gs1.png");
```

## Character Sets

| Set | Characters | Best For |
| --- | --- | --- |
| `A` | A-Z, 0-9, control chars | Alphanumeric with controls |
| `B` | A-Z, a-z, 0-9, symbols | General text (most common) |
| `C` | 00-99 (digit pairs) | Numeric data (most compact) |
