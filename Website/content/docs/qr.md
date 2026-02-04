---
title: QR Code - CodeGlyphX
description: QR code generation, styling, and presets.
slug: qr
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>QR Code Generation</h1>
<p>CodeGlyphX provides comprehensive QR code support including standard QR and Micro QR formats.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Simple one-liner
QR.Save("https://example.com", "qr.png");

// With error correction level
QR.Save("https://example.com", "qr.png", QrErrorCorrection.H);</pre>

<h2>Error Correction Levels</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Level</th>
<th style="text-align: left; padding: 0.75rem;">Recovery</th>
<th style="text-align: left; padding: 0.75rem;">Use Case</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><code>L</code></td>
<td style="padding: 0.75rem;">~7%</td>
<td style="padding: 0.75rem;">Maximum data capacity</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><code>M</code></td>
<td style="padding: 0.75rem;">~15%</td>
<td style="padding: 0.75rem;">Default, balanced</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><code>Q</code></td>
<td style="padding: 0.75rem;">~25%</td>
<td style="padding: 0.75rem;">Higher reliability</td>
</tr>
<tr>
<td style="padding: 0.75rem;"><code>H</code></td>
<td style="padding: 0.75rem;">~30%</td>
<td style="padding: 0.75rem;">Maximum error correction</td>
</tr>
</tbody>
</table>

<h2 id="styling-options">Styling Options</h2>
<pre class="code-block">using CodeGlyphX;

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

QR.Save("https://example.com", "styled-qr.png", options);</pre>

<h3>Fluent Builder (logo + styling)</h3>
<pre class="code-block">using CodeGlyphX;
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
    .Png();</pre>

<h3>Style Board Presets (Homepage Gallery)</h3>
<p>
                The homepage style board is generated from a curated set of presets. Each QR payload
                points back to this section and includes a <code>?style=slug</code> hint for tracking or
                future deep-linking. All presets use error correction <code>H</code>, a 384px target size,
                and a quiet zone of 4 modules to keep them crisp while remaining web-friendly.
</p>
<p>
                Full source code for these presets lives in
<a href="https://github.com/EvotecIT/CodeGlyphX/blob/master/CodeGlyphX.Examples/QrStyleBoardExample.cs" target="_blank" rel="noopener">QrStyleBoardExample.cs</a>.
</p>

<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Style</th>
<th style="text-align: left; padding: 0.75rem;">Recipe</th>
<th style="text-align: left; padding: 0.75rem;">Extras</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Neon Dot</strong><br /><span style="color: var(--text-dim);">Slug: <code>neon-dot</code></span></td>
<td style="padding: 0.75rem;">Shape: Dot<br />Eyes: Target<br />Palette: Random (cyan/magenta/yellow)<br />Canvas: Dark gradient</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Candy Checker</strong><br /><span style="color: var(--text-dim);">Slug: <code>candy-checker</code></span></td>
<td style="padding: 0.75rem;">Shape: Rounded<br />Eyes: Badge<br />Palette: Checker (pink/yellow)<br />Canvas: Dots pattern</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Pastel Rings</strong><br /><span style="color: var(--text-dim);">Slug: <code>pastel-rings</code></span></td>
<td style="padding: 0.75rem;">Shape: Squircle<br />Eyes: DoubleRing<br />Palette: Rings (pastels)<br />Canvas: Checker pattern</td>
<td style="padding: 0.75rem;">Scale map: Radial</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Ocean Grid</strong><br /><span style="color: var(--text-dim);">Slug: <code>ocean-grid</code></span></td>
<td style="padding: 0.75rem;">Shape: DotGrid<br />Eyes: Single<br />Palette: Cycle (ocean blues)<br />Canvas: Grid pattern</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Mono Badge</strong><br /><span style="color: var(--text-dim);">Slug: <code>mono-badge</code></span></td>
<td style="padding: 0.75rem;">Shape: Square<br />Eyes: Badge<br />Palette: None (mono)<br />Canvas: White with black border</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Bracket Tech</strong><br /><span style="color: var(--text-dim);">Slug: <code>bracket-tech</code></span></td>
<td style="padding: 0.75rem;">Shape: Diamond<br />Eyes: Bracket<br />Palette: Random (teal/blue/white)<br />Canvas: Dark gradient</td>
<td style="padding: 0.75rem;">Scale map: Rings</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Sunset Sticker</strong><br /><span style="color: var(--text-dim);">Slug: <code>sunset-sticker</code></span></td>
<td style="padding: 0.75rem;">Shape: Rounded<br />Eyes: Target<br />Palette: Cycle (sunset warm)<br />Canvas: Warm gradient</td>
<td style="padding: 0.75rem;">Logo: Warm circle</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Aurora</strong><br /><span style="color: var(--text-dim);">Slug: <code>aurora</code></span></td>
<td style="padding: 0.75rem;">Shape: Circle<br />Eyes: DoubleRing<br />Palette: Random (aurora hues)<br />Canvas: Dots pattern</td>
<td style="padding: 0.75rem;">Scale map: Random</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Mint Board</strong><br /><span style="color: var(--text-dim);">Slug: <code>mint-board</code></span></td>
<td style="padding: 0.75rem;">Shape: Squircle<br />Eyes: Single<br />Palette: Checker (mint)<br />Canvas: Mint border</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Deep Space</strong><br /><span style="color: var(--text-dim);">Slug: <code>deep-space</code></span></td>
<td style="padding: 0.75rem;">Shape: Dot<br />Eyes: Target<br />Palette: Rings (purple/teal)<br />Canvas: Night gradient</td>
<td style="padding: 0.75rem;">Logo: Cool circle</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Leaf Bloom</strong><br /><span style="color: var(--text-dim);">Slug: <code>leaf-bloom</code></span></td>
<td style="padding: 0.75rem;">Shape: Leaf<br />Eyes: DoubleRing<br />Palette: Cycle (greens)<br />Canvas: Forest gradient</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Wave Pulse</strong><br /><span style="color: var(--text-dim);">Slug: <code>wave-pulse</code></span></td>
<td style="padding: 0.75rem;">Shape: Wave<br />Eyes: Target<br />Palette: Random (electric blue)<br />Canvas: Dots pattern</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Ink Blob</strong><br /><span style="color: var(--text-dim);">Slug: <code>ink-blob</code></span></td>
<td style="padding: 0.75rem;">Shape: Blob<br />Eyes: Badge<br />Palette: Random (grayscale)<br />Canvas: Light border</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Soft Diamond</strong><br /><span style="color: var(--text-dim);">Slug: <code>soft-diamond</code></span></td>
<td style="padding: 0.75rem;">Shape: SoftDiamond<br />Eyes: DoubleRing<br />Palette: Rings (peach/pink)<br />Canvas: Warm gradient</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><strong>Sticker Grid</strong><br /><span style="color: var(--text-dim);">Slug: <code>sticker-grid</code></span></td>
<td style="padding: 0.75rem;">Shape: Square<br />Eyes: Badge<br />Palette: Cycle (mono)<br />Canvas: Grid pattern</td>
<td style="padding: 0.75rem;">—</td>
</tr>
<tr>
<td style="padding: 0.75rem;"><strong>Center Pop</strong><br /><span style="color: var(--text-dim);">Slug: <code>center-pop</code></span></td>
<td style="padding: 0.75rem;">Shape: Rounded<br />Eyes: Single<br />Palette: Cycle (navy) + Zones<br />Canvas: Clean border</td>
<td style="padding: 0.75rem;">Logo: Warm circle<br />Zones: Center + corners</td>
</tr>
</tbody>
</table>

<h4>Preset Starting Point (Neon Dot)</h4>
<pre class="code-block">using CodeGlyphX;
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

QR.Save("https://codeglyphx.com/docs/qr?style=neon-dot#styling-options", "neon-dot.png", options);</pre>
