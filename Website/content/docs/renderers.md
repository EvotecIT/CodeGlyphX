---
title: Output Formats - CodeGlyphX
description: Supported rendering formats and output types.
slug: renderers
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Output Formats</h1>
<p>CodeGlyphX supports a wide variety of output formats. The format is automatically determined by the file extension.</p>

<h2>Format Matrix (Encode + Decode)</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Format</th>
<th style="text-align: left; padding: 0.75rem;">Extensions</th>
<th style="text-align: left; padding: 0.75rem;">Encode</th>
<th style="text-align: left; padding: 0.75rem;">Decode</th>
<th style="text-align: left; padding: 0.75rem;">Notes</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">PNG</td>
<td style="padding: 0.75rem;"><code>.png</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">tRNS, Adam7, 1/2/4/8/16-bit</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">JPEG</td>
<td style="padding: 0.75rem;"><code>.jpg</code>, <code>.jpeg</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Baseline + progressive, EXIF, CMYK/YCCK</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">WebP</td>
<td style="padding: 0.75rem;"><code>.webp</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Managed VP8/VP8L; ImageReader returns first animation frame (use WebpReader/ImageReader multi-frame helpers); animation via WebpWriter or Webp renderers RenderAnimation</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">BMP</td>
<td style="padding: 0.75rem;"><code>.bmp</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">1/4/8/16/24/32-bit, RLE, bitfields</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">GIF</td>
<td style="padding: 0.75rem;"><code>.gif</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Single-frame encode (indexed, 256 colors max; quantizes + dithers if needed); animation via GifReader/GifWriter.WriteAnimation or GIF renderers RenderAnimation (diff-cropped)</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">TIFF</td>
<td style="padding: 0.75rem;"><code>.tif</code>, <code>.tiff</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Baseline RGBA encode (multi-page via TiffWriter.WriteRgba32Pages; tiled multi-page via WriteRgba32PagesTiled; planar via WriteRgba32Planar; bilevel 1-bit via WriteBilevel/WriteBilevelFromRgba or RenderBilevel; direct matrix bilevel via RenderBilevelFromModules; direct barcode bilevel via RenderBilevelFromBars; tiled bilevel via WriteBilevelTiled/WriteBilevelTiledFromRgba or RenderBilevelTiled; tiled direct matrix bilevel via RenderBilevelTiledFromModules; tiled direct barcode bilevel via RenderBilevelTiledFromBars; strips via rowsPerStrip; tiles via WriteRgba32Tiled; PackBits/Deflate/LZW via TiffCompression; optional predictor via usePredictor); decode strips/tiles, planar config 1/2, PackBits/LZW/Deflate, 1/8/16-bit</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">PSD</td>
<td style="padding: 0.75rem;"><code>.psd</code></td>
<td style="padding: 0.75rem;">No</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Flattened 8-bit grayscale/RGB; raw or RLE image data</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">PPM/PGM/PAM/PBM</td>
<td style="padding: 0.75rem;"><code>.ppm</code>, <code>.pgm</code>, <code>.pam</code>, <code>.pbm</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Portable pixmaps (8/16-bit maxval)</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">TGA</td>
<td style="padding: 0.75rem;"><code>.tga</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Uncompressed + RLE, true-color/grayscale/color-mapped</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">ICO</td>
<td style="padding: 0.75rem;"><code>.ico</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">PNG/BMP payloads (CUR decode supported)</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">XBM/XPM</td>
<td style="padding: 0.75rem;"><code>.xbm</code>, <code>.xpm</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Text formats</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">SVG / SVGZ</td>
<td style="padding: 0.75rem;"><code>.svg</code>, <code>.svgz</code>, <code>.svg.gz</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No</td>
<td style="padding: 0.75rem;">Vector output only</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">PDF / EPS</td>
<td style="padding: 0.75rem;"><code>.pdf</code>, <code>.eps</code>, <code>.ps</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Limited</td>
<td style="padding: 0.75rem;">Vector by default, raster via RenderMode (PDF decode: image-only JPEG/Flate)</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">HTML</td>
<td style="padding: 0.75rem;"><code>.html</code>, <code>.htm</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No</td>
<td style="padding: 0.75rem;">Table-based output</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">ASCII</td>
<td style="padding: 0.75rem;"><code>.txt</code></td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No</td>
<td style="padding: 0.75rem;">Text art</td>
</tr>
<tr>
<td style="padding: 0.75rem;">Raw RGBA</td>
<td style="padding: 0.75rem;">API only</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No</td>
<td style="padding: 0.75rem;">Use <code>RenderPixels</code></td>
</tr>
</tbody>
</table>

<h2>Programmatic Rendering</h2>
<pre class="code-block">using CodeGlyphX;

// Get raw PNG bytes
byte[] pngBytes = QrEasy.RenderPng("Hello", QrErrorCorrection.M, moduleSize: 10);

// Get SVG string
string svg = QrEasy.RenderSvg("Hello", QrErrorCorrection.M);

// Get Base64 data URI
string dataUri = QrEasy.RenderPngBase64DataUri("Hello");</pre>
