---
title: Code 39 - CodeGlyphX
description: Code 39 / 93 usage and comparisons.
slug: code39
collection: docs
layout: docs
---

{{< edit-link >}}

# Code 39 / Code 93

Code 39 and Code 93 are widely used linear barcodes, particularly in automotive and defense industries.

## Basic Usage

```csharp
using CodeGlyphX;

// Code 39
Barcode.Save(BarcodeType.Code39, "HELLO-123", "code39.png");

// Code 93 (more compact)
Barcode.Save(BarcodeType.Code93, "HELLO-123", "code93.png");
```

## Valid Characters

Code 39 supports: `A-Z`, `0-9`, `-`, `.`, `$`, `/`, `+`, `%`, `SPACE`

**Note:** Lowercase letters are automatically converted to uppercase.

## Comparison

| Feature | Code 39 | Code 93 |
| --- | --- | --- |
| Density | Lower | ~40% more compact |
| Checksum | Optional | Mandatory (2 chars) |
| Industry | Automotive, Defense | Logistics, Postal |
