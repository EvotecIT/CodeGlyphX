---
title: Output Formats - CodeGlyphX
description: Supported rendering formats and output types.
slug: renderers
collection: docs
layout: docs
---

{{< edit-link >}}

# Output Formats

CodeGlyphX supports a wide variety of output formats. The format is automatically determined by the file extension.

## Format Matrix (Encode + Decode)

| Format | Extensions | Encode | Decode | Notes |
| --- | --- | --- | --- | --- |
| PNG | `.png` | Yes | Yes | tRNS, Adam7, 1/2/4/8/16-bit |
| JPEG | `.jpg`, `.jpeg` | Yes | Yes | Baseline + progressive, EXIF, CMYK/YCCK |
| WebP | `.webp` | Yes | Yes | Managed VP8/VP8L; ImageReader returns first animation frame (use WebpReader/ImageReader multi-frame helpers); animation via WebpWriter or Webp renderers RenderAnimation |
| BMP | `.bmp` | Yes | Yes | 1/4/8/16/24/32-bit, RLE, bitfields |
| GIF | `.gif` | Yes | Yes | Single-frame encode (indexed, 256 colors max; quantizes + dithers if needed); animation via GifReader/GifWriter.WriteAnimation or GIF renderers RenderAnimation (diff-cropped) |
| TIFF | `.tif`, `.tiff` | Yes | Yes | Baseline RGBA encode (multi-page via TiffWriter.WriteRgba32Pages; tiled multi-page via WriteRgba32PagesTiled; planar via WriteRgba32Planar; bilevel 1-bit via WriteBilevel/WriteBilevelFromRgba or RenderBilevel; direct matrix bilevel via RenderBilevelFromModules; direct barcode bilevel via RenderBilevelFromBars; tiled bilevel via WriteBilevelTiled/WriteBilevelTiledFromRgba or RenderBilevelTiled; tiled direct matrix bilevel via RenderBilevelTiledFromModules; tiled direct barcode bilevel via RenderBilevelTiledFromBars; strips via rowsPerStrip; tiles via WriteRgba32Tiled; PackBits/Deflate/LZW via TiffCompression; optional predictor via usePredictor); decode strips/tiles, planar config 1/2, PackBits/LZW/Deflate, 1/8/16-bit |
| PSD | `.psd` | No | Yes | Flattened 8-bit grayscale/RGB; raw or RLE image data |
| PPM/PGM/PAM/PBM | `.ppm`, `.pgm`, `.pam`, `.pbm` | Yes | Yes | Portable pixmaps (8/16-bit maxval) |
| TGA | `.tga` | Yes | Yes | Uncompressed + RLE, true-color/grayscale/color-mapped |
| ICO | `.ico` | Yes | Yes | PNG/BMP payloads (CUR decode supported) |
| XBM/XPM | `.xbm`, `.xpm` | Yes | Yes | Text formats |
| SVG / SVGZ | `.svg`, `.svgz`, `.svg.gz` | Yes | No | Vector output only |
| PDF / EPS | `.pdf`, `.eps`, `.ps` | Yes | Limited | Vector by default, raster via RenderMode (PDF decode: image-only JPEG/Flate) |
| HTML | `.html`, `.htm` | Yes | No | Table-based output |
| ASCII | `.txt` | Yes | No | Text art |
| Raw RGBA | API only | Yes | No | Use `RenderPixels` |

## Programmatic Rendering

```csharp
using CodeGlyphX;

// Get raw PNG bytes
byte[] pngBytes = QrEasy.RenderPng("Hello", QrErrorCorrection.M, moduleSize: 10);

// Get SVG string
string svg = QrEasy.RenderSvg("Hello", QrErrorCorrection.M);

// Get Base64 data URI
string dataUri = QrEasy.RenderPngBase64DataUri("Hello");
```
