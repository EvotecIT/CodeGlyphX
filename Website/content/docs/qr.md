---
title: QR Code - CodeGlyphX
description: QR code generation, styling, and presets.
slug: qr
collection: docs
layout: docs
---

{{< edit-link >}}

# QR Code Generation

CodeGlyphX provides comprehensive QR code support including standard QR and Micro QR formats.

## Basic Usage

```csharp
using CodeGlyphX;

// Simple one-liner
QR.Save("https://example.com", "qr.png");

// With error correction level
QR.Save("https://example.com", "qr.png", QrErrorCorrection.H);
```

## Error Correction Levels

| Level | Recovery | Use Case |
| --- | --- | --- |
| `L` | ~7% | Maximum data capacity |
| `M` | ~15% | Default, balanced |
| `Q` | ~25% | Higher reliability |
| `H` | ~30% | Maximum error correction |

## Styling Options

```csharp
using CodeGlyphX;

var options = new QrEasyOptions
{
    ModuleShape = QrPngModuleShape.Rounded,
    ModuleCornerRadiusPx = 3,
    Eyes = new QrPngEyeOptions
    {
        UseFrame = true,
        OuterShape = QrPngModuleShape.Circle,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(220, 20, 60),
        InnerColor = new Rgba32(220, 20, 60)
    }
};

QR.Save("https://example.com", "styled-qr.png", options);
```

### Fluent Builder (logo + styling)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var logo = LogoBuilder.CreateCirclePng(
    size: 96,
    color: new Rgba32(24, 24, 24),
    accent: new Rgba32(240, 240, 240),
    out _,
    out _);

var png = QR.Create("https://example.com")
    .WithLogoPng(logo)
    .WithLogoScale(0.22)
    .WithLogoPaddingPx(6)
    .WithStyle(QrRenderStyle.Fancy)
    .Png();
```

### Style Board Presets (Homepage Gallery)

The homepage style board is generated from a curated set of presets. Each QR payload
points back to this section and includes a `?style=slug` hint for tracking or
future deep-linking. All presets use error correction `H`, a 384px target size,
and a quiet zone of 4 modules to keep them crisp while remaining web-friendly.

Full source code for these presets lives in
[QrStyleBoardExample.cs](https://github.com/EvotecIT/CodeGlyphX/blob/master/CodeGlyphX.Examples/QrStyleBoardExample.cs).

| Style | Recipe | Extras |
| --- | --- | --- |
| **Neon Dot** | Shape: Dot   <br>Eyes: Target   <br>Palette: Random (cyan/magenta/yellow)   <br>Canvas: Dark gradient | — |
| **Candy Checker** | Shape: Rounded   <br>Eyes: Badge   <br>Palette: Checker (pink/yellow)   <br>Canvas: Dots pattern | — |
| **Pastel Rings** | Shape: Squircle   <br>Eyes: DoubleRing   <br>Palette: Rings (pastels)   <br>Canvas: Checker pattern | Scale map: Radial |
| **Ocean Grid** | Shape: DotGrid   <br>Eyes: Single   <br>Palette: Cycle (ocean blues)   <br>Canvas: Grid pattern | — |
| **Mono Badge** | Shape: Square   <br>Eyes: Badge   <br>Palette: None (mono)   <br>Canvas: White with black border | — |
| **Bracket Tech** | Shape: Diamond   <br>Eyes: Bracket   <br>Palette: Random (teal/blue/white)   <br>Canvas: Dark gradient | Scale map: Rings |
| **Sunset Sticker** | Shape: Rounded   <br>Eyes: Target   <br>Palette: Cycle (sunset warm)   <br>Canvas: Warm gradient | Logo: Warm circle |
| **Aurora** | Shape: Circle   <br>Eyes: DoubleRing   <br>Palette: Random (aurora hues)   <br>Canvas: Dots pattern | Scale map: Random |
| **Mint Board** | Shape: Squircle   <br>Eyes: Single   <br>Palette: Checker (mint)   <br>Canvas: Mint border | — |
| **Deep Space** | Shape: Dot   <br>Eyes: Target   <br>Palette: Rings (purple/teal)   <br>Canvas: Night gradient | Logo: Cool circle |
| **Leaf Bloom** | Shape: Leaf   <br>Eyes: DoubleRing   <br>Palette: Cycle (greens)   <br>Canvas: Forest gradient | — |
| **Wave Pulse** | Shape: Wave   <br>Eyes: Target   <br>Palette: Random (electric blue)   <br>Canvas: Dots pattern | — |
| **Ink Blob** | Shape: Blob   <br>Eyes: Badge   <br>Palette: Random (grayscale)   <br>Canvas: Light border | — |
| **Soft Diamond** | Shape: SoftDiamond   <br>Eyes: DoubleRing   <br>Palette: Rings (peach/pink)   <br>Canvas: Warm gradient | — |
| **Sticker Grid** | Shape: Square   <br>Eyes: Badge   <br>Palette: Cycle (mono)   <br>Canvas: Grid pattern | — |
| **Center Pop** | Shape: Rounded   <br>Eyes: Single   <br>Palette: Cycle (navy) + Zones   <br>Canvas: Clean border | Logo: Warm circle   <br>Zones: Center + corners |

#### Preset Starting Point (Neon Dot)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

var options = new QrEasyOptions
{
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
    TargetSizePx = 384,
    TargetSizeIncludesQuietZone = true,
    ModuleSize = 10,
    QuietZone = 4,
    Foreground = new Rgba32(0, 255, 213),
    ModuleShape = QrPngModuleShape.Dot,
    Eyes = new QrPngEyeOptions
    {
        UseFrame = true,
        FrameStyle = QrPngEyeFrameStyle.Target,
        OuterShape = QrPngModuleShape.Rounded,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(0, 255, 213),
        InnerColor = new Rgba32(255, 59, 255)
    },
    ForegroundPalette = new QrPngPaletteOptions
    {
        Mode = QrPngPaletteMode.Random,
        Seed = 14001,
        RingSize = 2,
        Colors = new[]
        {
            new Rgba32(0, 255, 213),
            new Rgba32(255, 59, 255),
            new Rgba32(255, 214, 0)
        }
    },
    Canvas = new QrPngCanvasOptions
    {
        PaddingPx = 24,
        CornerRadiusPx = 26,
        BackgroundGradient = new QrPngGradientOptions
        {
            Type = QrPngGradientType.DiagonalDown,
            StartColor = new Rgba32(18, 18, 28),
            EndColor = new Rgba32(48, 23, 72)
        },
        BorderPx = 2,
        BorderColor = new Rgba32(255, 255, 255, 40),
        ShadowOffsetX = 6,
        ShadowOffsetY = 8,
        ShadowColor = new Rgba32(0, 0, 0, 60)
    }
};

QR.Save("https://codeglyphx.com/docs/qr?style=neon-dot#styling-options", "neon-dot.png", options);
```
