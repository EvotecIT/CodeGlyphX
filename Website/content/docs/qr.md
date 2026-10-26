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
QR.Save("https://example.com", "qr.png", encodingOptions: new QrEncodingOptions {
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H
});
```

Encoding controls belong to `QrEncodingOptions`; appearance belongs to `QrRenderOptions`. An encoded `QrCode` can be rendered repeatedly without changing its payload or version. See the [3.0 migration guide](/docs/migration-3/) for the option and result changes.

SVG and SVGZ preserve body shapes, eyes, palettes and canvas artwork through vector coverage contours at the selected render resolution. Choose the module or target pixel size before exporting for print. HTML supports a narrower appearance set and rejects unsupported effects with `NotSupportedException`.

## Coordinated looks

The [showcase](/showcase/#hero-presets) includes twelve coordinated looks, with coordinated light and dark compositions. Neon uses the same dark design for both choices. The code stays dark on a light background in both variants. `QrArt.Hero` returns editable rendering options:

```csharp
var appearance = QrArt.Hero(QrHeroPreset.Sunset, darkPanel: true);
QR.Save("https://example.com/menu", "menu.png", appearance,
    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
```

For a logo, use `QrArt.RenderLogo` with the Brand appearance. Auto placement keeps functional patterns intact and sizes the knockout against each affected Reed–Solomon block. The result reports the chosen placement, logo modules, touched blocks and remaining margins. If no readable logo fits, rendering throws rather than omitting it. Version and correction-level increases are attempted only when enabled.

```csharp
var result = QrArt.RenderLogo("https://example.com", File.ReadAllBytes("logo.png"),
    new CodeGlyphX.Rendering.Art.QrLogoRenderOptions {
        Appearance = QrArt.Hero(QrHeroPreset.Brand),
        AllowErrorCorrectionUpgrade = true,
        AllowVersionUpgrade = true
    });
File.WriteAllBytes("brand.png", result.GetPng());
```

`LogoPlacement.CenterOverAlignment` is an explicit alternative that requires native and independent decoder verification, with a warning when the alignment pattern is covered. No external decoder ships with the library. Software checks do not replace checking the final design on the phones and printed media you intend to use.

### Captions and call-to-action frames

A caption stays outside the complete quiet zone. Its default cap height scales with the code width; long text shrinks to a readable floor and then wraps to two lines. `FontSizePx` sets an explicit em size when needed. The bundled default is Manrope Medium with Latin Extended, including Polish text such as `Zażółć gęślą jaźń`. Its Medium outlines render unchanged by default; `Weight = QrCanvasCaptionWeight.Bold` adds synthetic bold. Supply a licensed TTF or OTF for another face; captions use the managed outline engine and do not depend on installed fonts. Strict text handling rejects unavailable glyphs instead of silently substituting another face.

```csharp
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;

var caption = QrCanvasCaptionOptions.FromFontBytes(File.ReadAllBytes("BrandSans.ttf"));
caption.Text = "Explore the collection";
caption.Subline = "Our autumn selection";
caption.Icon = QrCanvasCaptionIcon.Camera;
caption.Url = "evotec.xyz";
caption.Position = QrCanvasCaptionPosition.Bottom;

var appearance = QrArt.Hero(QrHeroPreset.Ink);
appearance.Canvas = new QrCanvasOptions {
    Template = QrCanvasTemplate.ScanMePill,
    CaptionTail = true,
    BandColor = new Rgba32(30, 35, 50),
    StrokeColor = new Rgba32(30, 35, 50),
    Caption = caption
};
QR.Save("https://evotec.xyz", "collection.png", appearance,
    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
```

`SpeechBubble` is the pill with its tail enabled. Other templates provide a ticket with semicircle notches, a phone silhouette, a rounded card band and viewfinder corners. `BandLogoPng` places a caller-supplied logo beside the caption, independently of a budgeted logo inside the code. Caption, band and stroke colours can be selected separately. For dark compositions, use a dark canvas background; the automatic caption and stroke colours follow it.

### Logo plates and gradients

The default logo plate follows the finder shape and takes the code background colour. `LogoPlateShape` also accepts `Circle`, `Squircle`, `RoundedSquare` or `Square`. `LogoRingColor` and `LogoRingWidthPx` add an inset accent ring within the existing knockout budget; `LogoTint` uses monochrome ink while retaining source transparency. Dot Matrix and Brand tint the supplied mark to their palette.

Gradients interpolate in OKLCH with gamut-aware chroma reduction. Choose `QrGradientInterpolation.Oklab` for Cartesian perceptual interpolation or `Srgb` for legacy channel interpolation. Canvas shadows and panel bloom reserve their extent; PNG renders these effects directly. Canonical SVG preserves the rendered coverage as vector contours at the chosen resolution, including text outlines and soft effects. Those contours are resolution-bound rather than editable SVG filter effects.

## Error Correction Levels

| Level | Recovery | Use Case |
| --- | --- | --- |
| `L` | ~7% | Maximum data capacity |
| `M` | ~15% | Default, balanced |
| `Q` | ~25% | Higher reliability |
| `H` | ~30% | Maximum error correction |

{{< cgx-figure id="qr-ecc" caption="The same URL at each level, drawn to one scale by CodeGlyphX. More recovery data means a higher version and more modules." >}}


## Styling Options

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

var options = new QrRenderOptions
{
    ModuleShape = QrModuleShape.Rounded,
    ModuleCornerRadiusPx = 3,
    Eyes = new QrEyeOptions
    {
        UseFrame = true,
        OuterShape = QrModuleShape.Circle,
        InnerShape = QrModuleShape.Circle,
        OuterColor = new Rgba32(220, 20, 60),
        InnerColor = new Rgba32(220, 20, 60)
    }
};

QR.Save("https://example.com", "styled-qr.png", options);
```

For a high-level art theme, start with conservative guardrails and inspect the static report:

```csharp
var options = new QrRenderOptions {
    Art = QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Conservative, intensity: 60)
};

var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
var report = QR.EvaluateScanHeuristics("https://example.com", options, encoding);
QR.Save("https://example.com", "styled-qr.png", options, encoding);
```

`PassesHeuristics` checks configuration properties such as contrast, quiet zone, module scale, functional-pattern protection, and logo coverage. It is not a decode test and cannot guarantee interoperability. Test the final artifact on every scanner, authenticator, camera, display, print process, and device class your product supports.


{{< cgx-figure id="qr-shapes" caption="QrModuleShape values on one payload. Each tile notes whether CodeGlyphX decodes its own PNG; check every style on the scanners your users have." >}}

{{< cgx-figure id="qr-eyes" caption="QrEyeFrameStyle values with rounded modules and coloured finders, rendered and read back by CodeGlyphX." >}}

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

var png = QR.Create("https://example.com", encodingOptions: new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 4 })
    .WithLogoPng(logo)
    .WithLogoScale(0.22)
    .WithLogoPaddingPx(6)
    .WithStyle(QrRenderStyle.Fancy)
    .Render(OutputFormat.Png)
    .ToArray();
```

### Style Board Presets (Homepage Gallery)

The homepage style board is generated from a curated set of presets. Each QR payload
points back to this section and includes a `?style=slug` hint for tracking or
future deep-linking. All presets use error correction `H`, a 384px target size,
and a quiet zone of 4 modules to keep them crisp while remaining web-friendly.
They are visual examples, not scanner-certification fixtures. Several deliberately artistic recipes exceed what some readers or cameras can handle.

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

var options = new QrRenderOptions
{
    TargetSizePx = 384,
    TargetSizeIncludesQuietZone = true,
    ModuleSize = 10,
    QuietZone = 4,
    Foreground = new Rgba32(0, 255, 213),
    ModuleShape = QrModuleShape.Dot,
    Eyes = new QrEyeOptions
    {
        UseFrame = true,
        FrameStyle = QrEyeFrameStyle.Target,
        OuterShape = QrModuleShape.Rounded,
        InnerShape = QrModuleShape.Circle,
        OuterColor = new Rgba32(0, 255, 213),
        InnerColor = new Rgba32(255, 59, 255)
    },
    ForegroundPalette = new QrPaletteOptions
    {
        Mode = QrPaletteMode.Random,
        Seed = 14001,
        RingSize = 2,
        Colors = new[]
        {
            new Rgba32(0, 255, 213),
            new Rgba32(255, 59, 255),
            new Rgba32(255, 214, 0)
        }
    },
    Canvas = new QrCanvasOptions
    {
        PaddingPx = 24,
        CornerRadiusPx = 26,
        BackgroundGradient = new QrGradientOptions
        {
            Type = QrGradientType.DiagonalDown,
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

QR.Save("https://codeglyphx.com/docs/qr?style=neon-dot#styling-options", "neon-dot.png", options,
    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
```
