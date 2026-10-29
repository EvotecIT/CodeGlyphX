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

The [showcase](/showcase/#hero-presets) includes twelve editable looks with light and dark compositions. Neon uses the same dark design for both choices. Dark compositions use a palette-tinted light panel, a larger dark field and an accent bloom or wash. The code stays dark on a light background in both variants. `QrArt.Hero` starts at a 1024-pixel code size and returns editable rendering options:

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

A caption stays outside the complete quiet zone. The complete caption block, including its band and icon, stays within the visible code width, excluding the quiet zone. It keeps at least one cap height of clearance from the canvas, frame edges and decorations. Its default cap height is 5.5% of the visible code width; long text shrinks to a readable floor and then wraps to two lines. Camera and handset icons use independent vector outlines, so they require no symbol glyphs in the caption font. Sublines are centred beneath the complete headline group. `FontSizePx` sets an explicit em size when needed. The bundled default is Manrope SemiBold with Latin Extended, including Polish text such as `Zażółć gęślą jaźń`. Its SemiBold outlines render unchanged by default; `Weight = QrCanvasCaptionWeight.Bold` adds synthetic bold. Supply a licensed TTF or OTF for another face; captions use the managed outline engine and do not depend on installed fonts. Strict text handling rejects unavailable glyphs instead of silently substituting another face.

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

`SpeechBubble` is the pill with its tail enabled. Other templates provide a ticket with semicircle notches, a phone silhouette, a rounded card band and viewfinder corners. Every decoration keeps the same outer safe margin of at least one headline cap height. Ticket perforations leave a complete dash-gap inside each notch; caption icons sit 0.35 em before the headline. `BandLogoPng` places a caller-supplied logo beside the caption, independently of a budgeted logo inside the code. Caption, band and stroke colours can be selected separately. For dark compositions, use a dark canvas background; the automatic caption and stroke colours follow it.

### Logo plates and gradients

The default logo plate follows the finder shape and takes the code background colour. Modules touched by the plate are removed whole before rendering, so dots and connected bars end cleanly around it. `LogoPlateShape` also accepts `Circle`, `Squircle`, `RoundedSquare` or `Square`. `LogoRingColor` and `LogoRingWidthPx` add an inset accent ring within the existing knockout budget; `LogoTint` uses monochrome ink while retaining source transparency. Ink, Dot Matrix, Minimal Line and Mono Classy tint the supplied mark to their palette.

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

### Style Board (Homepage Gallery)

The homepage style board shows twenty looks built only from public render options: hero
presets, an art theme, module shapes, eyes, palettes, canvas templates and logos. Every look
encodes `https://codeglyphx.com` with error correction `H` at a 480px code size, plus any canvas.
The test suite renders each look through the same code as the website and requires the exact
payload from strict ZXing and the CodeGlyphX decoder at the delivered size, at 1024, 512 and
256 px, after a 2 degree rotation and after a light blur. Check your own design on the phones
and printed media you intend to use.

The recipes live in
[QrStyleBoardLooks.cs](https://github.com/EvotecIT/CodeGlyphX/blob/master/CodeGlyphX.Examples/QrStyleBoardLooks.cs).
Set `CODEGLYPHX_STYLE_BOARD=1` and run `CodeGlyphX.Examples` to regenerate the images and
`style-board.json`.

| Style | Recipe |
| --- | --- |
| **Sunset** | `QrArt.Hero(QrHeroPreset.Sunset)` |
| **Neon** | `QrArt.Hero(QrHeroPreset.Neon, darkPanel: true)` |
| **Ink** | `QrArt.Hero(QrHeroPreset.Ink)` |
| **Retro Pixel** | `QrArt.Hero(QrHeroPreset.RetroPixel)` |
| **Gold Sparkle** | ConnectedRounded body; `QrEyePresets.NeonSparkle()` recoloured bronze; black-to-gold canvas gradient |
| **Ticket** | ConnectedRounded body; `QrCanvasTemplate.TicketStub` on coral with an "ADMIT ONE" caption |
| **Botanical** | `QrArt.Hero(QrHeroPreset.Botanical)` |
| **Confetti** | Rounded body; Random palette (crimson, violet, blue) on cream |
| **Mono Classy** | `QrArt.Hero(QrHeroPreset.MonoClassy, darkPanel: true)` |
| **Ocean** | `QrArt.Hero(QrHeroPreset.Ocean)` on a pale aqua background |
| **Scan Me** | Rounded body; `QrCanvasTemplate.ScanMePill` on yellow |
| **Berry** | `QrArt.Hero(QrHeroPreset.Berry)` |
| **Glow Frame** | `QrArt.Theme(QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Pastel)` |
| **Paper Cutout** | `QrArt.Hero(QrHeroPreset.PaperCutout)` |
| **Brand Logo** | `QrArt.Hero(QrHeroPreset.Brand)` with a sample logo through `QrArt.RenderLogo` |
| **Soft Diamond** | SoftDiamond body; Rings palette (rust, umber, wine); apricot canvas gradient |
| **Wi-Fi Card** | ConnectedSquircle body; `QrCanvasTemplate.RoundedCardHeader` in blue with a caption |
| **Viewfinder** | ExtraRounded crimson body; `QrCanvasTemplate.CornerViewfinder` with a caption |
| **Dot Matrix** | `QrArt.Hero(QrHeroPreset.DotMatrix)` on a pale blue background |
| **Mint Logo** | Squircle body and eyes; Checker palette (two greens); mint canvas with a green border; sample logo through `QrArt.RenderLogo` |

#### Starting Point (Gold Sparkle)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var eyes = QrEyePresets.NeonSparkle();
eyes.OuterColor = new Rgba32(150, 98, 0);
eyes.InnerColor = new Rgba32(60, 38, 0);
eyes.SparkleColor = new Rgba32(255, 222, 140, 210);

var options = new QrRenderOptions
{
    TargetSizePx = 480,
    QuietZone = 4,
    ModuleShape = QrModuleShape.ConnectedRounded,
    Foreground = new Rgba32(92, 60, 4),
    Background = new Rgba32(255, 250, 232),
    Eyes = eyes,
    Canvas = new QrCanvasOptions
    {
        PaddingPx = 44,
        CornerRadiusPx = 28,
        QrCornerRadiusPx = 16,
        BackgroundGradient = new QrGradientOptions
        {
            Type = QrGradientType.DiagonalDown,
            StartColor = new Rgba32(14, 12, 8),
            EndColor = new Rgba32(70, 50, 12)
        }
    }
};

QR.Save("https://codeglyphx.com", "gold-sparkle.png", options,
    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
```
