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
| **Neon Dot** | Shape: Dot; Eyes: Target; Palette: Random (cyan/magenta/yellow); Canvas: Dark gradient | — |
| **Candy Checker** | Shape: Rounded; Eyes: Badge; Palette: Checker (pink/yellow); Canvas: Dots pattern | — |
| **Pastel Rings** | Shape: Squircle; Eyes: DoubleRing; Palette: Rings (pastels); Canvas: Checker pattern | Scale map: Radial |
| **Ocean Grid** | Shape: DotGrid; Eyes: Single; Palette: Cycle (ocean blues); Canvas: Grid pattern | — |
| **Mono Badge** | Shape: Square; Eyes: Badge; Palette: None (mono); Canvas: White with black border | — |
| **Bracket Tech** | Shape: Diamond; Eyes: Bracket; Palette: Random (teal/blue/white); Canvas: Dark gradient | Scale map: Rings |
| **Sunset Sticker** | Shape: Rounded; Eyes: Target; Palette: Cycle (sunset warm); Canvas: Warm gradient | Logo: Warm circle |
| **Aurora** | Shape: Circle; Eyes: DoubleRing; Palette: Random (aurora hues); Canvas: Dots pattern | Scale map: Random |
| **Mint Board** | Shape: Squircle; Eyes: Single; Palette: Checker (mint); Canvas: Mint border | — |
| **Deep Space** | Shape: Dot; Eyes: Target; Palette: Rings (purple/teal); Canvas: Night gradient | Logo: Cool circle |
| **Leaf Bloom** | Shape: Leaf; Eyes: DoubleRing; Palette: Cycle (greens); Canvas: Forest gradient | — |
| **Wave Pulse** | Shape: Wave; Eyes: Target; Palette: Random (electric blue); Canvas: Dots pattern | — |
| **Ink Blob** | Shape: Blob; Eyes: Badge; Palette: Random (grayscale); Canvas: Light border | — |
| **Soft Diamond** | Shape: SoftDiamond; Eyes: DoubleRing; Palette: Rings (peach/pink); Canvas: Warm gradient | — |
| **Sticker Grid** | Shape: Square; Eyes: Badge; Palette: Cycle (mono); Canvas: Grid pattern | — |
| **Center Pop** | Shape: Rounded; Eyes: Single; Palette: Cycle (navy) + Zones; Canvas: Clean border | Logo: Warm circle; Zones: Center + corners |

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
