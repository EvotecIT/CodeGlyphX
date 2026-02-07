---
title: Micro QR - CodeGlyphX
description: Micro QR generation and constraints.
slug: microqr
collection: docs
layout: docs
---

{{< edit-link >}}

# Micro QR Code

Micro QR is a smaller version of the standard QR code, designed for applications where space is limited.

## Basic Usage

```csharp
using CodeGlyphX;

// Generate Micro QR
QR.Save("ABC123", "microqr.png", microQr: true);
```

## Comparison with Standard QR

| Feature | Standard QR | Micro QR |
| --- | --- | --- |
| Finder patterns | 3 corners | 1 corner |
| Max capacity | ~3KB | ~35 characters |
| Best for | General use | Small labels, PCBs |
