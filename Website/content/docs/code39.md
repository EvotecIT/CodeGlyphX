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
Barcode.Save(SymbolFormat.Code39, "HELLO-123", "code39.png");

// Code 93 (more compact)
Barcode.Save(SymbolFormat.Code93, "HELLO-123", "code93.png");
```

## Valid Characters

Code 39 supports: `A-Z`, `0-9`, `-`, `.`, `$`, `/`, `+`, `%`, `SPACE`

The basic `Barcode` API rejects lowercase letters and other characters outside this set. If your application's identifiers are case-insensitive, normalize them explicitly before encoding:

```csharp
var identifier = "hello-123";
Barcode.Save(SymbolFormat.Code39, identifier.ToUpperInvariant(), "code39.png");
```

To preserve lowercase or other ASCII characters, use the specialist full-ASCII encoder:

```csharp
using CodeGlyphX.Code39;

var fullAscii = Code39Encoder.Encode("hello-123", fullAsciiMode: true);
```

The receiving scanner must also support full-ASCII Code 39. This mode represents additional characters with pairs from the basic character set.

## Comparison

| Feature | Code 39 | Code 93 |
| --- | --- | --- |
| Density | Lower | ~40% more compact |
| Checksum | Optional | Mandatory (2 chars) |
| Industry | Automotive, Defense | Logistics, Postal |
