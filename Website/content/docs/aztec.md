---
title: Aztec - CodeGlyphX
description: Aztec encoding basics and use cases.
slug: aztec
collection: docs
layout: docs
---

{{< edit-link >}}

# Aztec Code

Aztec is a 2D matrix barcode designed for high readability even when printed at low resolution or on curved surfaces.

## Basic Usage

```csharp
using CodeGlyphX;
using CodeGlyphX.Aztec;

// Simple Aztec code
AztecCode.Save("Ticket: CONF-2024-001", "aztec.png");

// With error correction percentage
AztecCode.Save("Ticket data", "aztec.png", new AztecEncodeOptions {
    ErrorCorrectionPercent = 33
});
```

Binary payloads use the same generic rendering and file APIs:

```csharp
using CodeGlyphX.Rendering;

byte[] payload = { 0x00, 0x01, 0xFE, 0xFF };

var png = AztecCode.Render(payload, OutputFormat.Png).Data;
AztecCode.Save(payload, "binary-ticket.svg");
```

## Use Cases

- **Transportation** - Train and airline tickets
- **Event tickets** - Mobile ticketing apps
- **Patient wristbands** - Healthcare identification
- **Curved surfaces** - Bottles, tubes, cylinders

## Advantages

Aztec codes don't require a quiet zone around them and can be read even when partially damaged, making them ideal for mobile ticketing.
