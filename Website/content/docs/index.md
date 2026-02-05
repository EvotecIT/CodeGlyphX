---
title: Documentation - CodeGlyphX
description: CodeGlyphX documentation - learn how to generate and decode QR codes, barcodes, and 2D matrix codes.
slug: index
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>CodeGlyphX Documentation</h1>
<p>
                Welcome to the CodeGlyphX documentation. CodeGlyphX is a zero-dependency .NET library
                for generating and decoding QR codes, barcodes, and other 2D matrix codes.
</p>
<div class="docs-status">Actively developed - Stable core - Expanding format support</div>

<h2>Key Features</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>Zero external dependencies</strong> - No System.Drawing, SkiaSharp, or ImageSharp required</li>
<li><strong>Full encode &amp; decode</strong> - Round-trip support for all symbologies</li>
<li><strong>Multiple output formats</strong> - PNG, SVG, PDF, EPS, HTML, and many more</li>
<li><strong>Cross-platform</strong> - Windows, Linux, macOS</li>
<li><strong>AOT compatible</strong> - Works with Native AOT and trimming</li>
</ul>

<h2>Supported Symbologies</h2>
<h3>2D Matrix Codes</h3>
<p>QR Code, Micro QR, Data Matrix, PDF417 / MicroPDF417, Aztec, GS1 DataBar (Omni/Stacked), and postal 4-state (IMB/RM4SCC/AusPost)</p>

<h3>1D Linear Barcodes</h3>
<p>Code 128, GS1-128, Code 39/93/11, Codabar, MSI, Plessey, Telepen, Pharmacode, Code 32, EAN/UPC, ITF and 2-of-5 variants, GS1 DataBar (Truncated/Expanded)</p>

<h2>Documentation Map</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>QR &amp; Micro QR</strong> - Core encoding, error correction, and QR specifics. <a href="/docs/qr/">QR docs</a></li>
<li><strong>Styling &amp; presets</strong> - Module shapes, palettes, logos, and the style board gallery. <a href="/docs/qr/#styling-options">Styling options</a></li>
<li><strong>Payload helpers</strong> - WiFi, vCards, OTP, SEPA, and more. <a href="/docs/payloads/">Payload helpers</a></li>
<li><strong>Image decoding</strong> - Decode from images and screenshots. <a href="/docs/decoding/">Image decoding</a></li>
<li><strong>Output formats</strong> - PNG, SVG, PDF, EPS, HTML, and more. <a href="/docs/renderers/">Output formats</a></li>
<li><strong>API Reference</strong> - Full type and method documentation. <a href="/api/">API reference</a></li>
<li><strong>FAQ</strong> - Common questions and troubleshooting. <a href="/faq/">FAQ</a></li>
</ul>

<h2>Quick Example</h2>
<pre class="code-block">using CodeGlyphX;

// Generate a QR code
QR.Save("https://evotec.xyz", "website.png");

// Generate a barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");

// Decode an image
if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine(result.Text);
}</pre>

<h2>Getting Help</h2>
<p>
                If you encounter issues or have questions, please visit the
<a href="https://github.com/EvotecIT/CodeGlyphX/issues" target="_blank">GitHub Issues</a> page.
</p>
<p>
                For planned work and known gaps, see the
<a href="https://github.com/EvotecIT/CodeGlyphX/blob/master/ROADMAP.md" target="_blank">ROADMAP</a>.
</p>


