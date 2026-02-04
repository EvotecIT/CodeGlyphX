---
title: Quick Start - CodeGlyphX
description: Get started with CodeGlyphX in minutes.
slug: quickstart
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Quick Start</h1>
<p>Get up and running with CodeGlyphX in under a minute.</p>

<h2>1. Install the Package</h2>
<pre class="code-block">dotnet add package CodeGlyphX</pre>

<h2>2. Generate Your First QR Code</h2>
<pre class="code-block">using CodeGlyphX;

// Create a QR code and save to file
QR.Save("Hello, World!", "hello.png");

// The output format is determined by the file extension
QR.Save("Hello, World!", "hello.svg");  // Vector SVG
QR.Save("Hello, World!", "hello.pdf");  // PDF document</pre>

<h3>Render In-Memory</h3>
<pre class="code-block">using CodeGlyphX;
using CodeGlyphX.Rendering;

var svg = Barcode.Render(BarcodeType.Code128, "PRODUCT-12345", OutputFormat.Svg).GetText();
var png = QrCode.Render("Hello, World!", OutputFormat.Png).Data;

// HTML title + raster PDF/EPS
var extras = new RenderExtras { HtmlTitle = "My Code", VectorMode = RenderMode.Raster };
QR.Save("Hello, World!", "hello.html", extras: extras);</pre>

<h2>3. Generate Barcodes</h2>
<pre class="code-block">using CodeGlyphX;

// Code 128 barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "barcode.png");

// EAN-13 (retail products)
Barcode.Save(BarcodeType.EAN, "5901234123457", "ean.png");</pre>

<h2>4. Decode Images</h2>
<pre class="code-block">using CodeGlyphX;

var imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
}</pre>
