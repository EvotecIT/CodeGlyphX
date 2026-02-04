---
title: Image Decoding - CodeGlyphX
description: Decode barcodes and QR codes from images.
slug: decoding
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Image Decoding</h1>
<p>CodeGlyphX includes a built-in decoder for reading QR codes and barcodes from images.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Decode from file
byte[] imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
    Console.WriteLine($"Format: {result.BarcodeFormat}");
}

// Decode from stream
using var stream = File.OpenRead("barcode.png");
var decodeResult = QrImageDecoder.DecodeImage(stream);</pre>

<h2>Supported Formats for Decoding</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>Images:</strong> PNG, JPEG, WebP, BMP, GIF, TIFF, PPM/PGM/PBM/PAM, TGA, ICO/CUR, XBM, XPM</li>
<li><strong>QR Codes:</strong> Standard QR, Micro QR</li>
<li><strong>1D Barcodes:</strong> Code 128, Code 39/93, EAN/UPC, ITF, Codabar, MSI, Telepen, Plessey, more</li>
<li><strong>2D Matrix:</strong> Data Matrix, PDF417, Aztec</li>
</ul>

<h2>Known Gaps (Decoding)</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li>AVIF, HEIC, JPEG2000</li>
<li>ImageReader.DecodeRgba32 returns the first GIF/WebP frame; use ImageReader.DecodeGifAnimationFrames / DecodeWebpAnimationFrames (or GifReader/WebpReader) for animation frames</li>
<li>ImageReader.DecodeRgba32 returns the first TIFF page; use ImageReader.TryDecodeTiffPagesRgba32 (or TiffReader.DecodePagesRgba32) for multi-page</li>
<li>PDF decode supports image-only PDFs with embedded JPEG/Flate; PS decode still needs rasterization</li>
</ul>

<h2>Format Corpus (Optional)</h2>
<p class="text-muted">We maintain external image samples to validate edge cases (PNG/TIFF suites, packed bit-depths, interlace, etc.). These are not stored in the repo.</p>
<pre class="code-block">// Download the image format corpus
pwsh Build/Download-ImageSamples.ps1

// Download external barcode/QR samples
pwsh Build/Download-ExternalSamples.ps1</pre>

<h2>Handling Multiple Results</h2>
<pre class="code-block">// Decode all barcodes in an image
var results = QrImageDecoder.DecodeAllImages(imageBytes);

foreach (var barcode in results)
{
    Console.WriteLine($"{barcode.BarcodeFormat}: {barcode.Text}");
}</pre>

<h2>Diagnostics</h2>
<pre class="code-block">using CodeGlyphX;

if (!CodeGlyph.TryDecodeImage(imageBytes, out var decoded, out var diagnostics, options: null))
{
    Console.WriteLine(diagnostics.FailureReason);
    Console.WriteLine(diagnostics.Failure);
}</pre>
