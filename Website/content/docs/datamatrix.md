---
title: Data Matrix - CodeGlyphX
description: Data Matrix encoding basics and use cases.
slug: datamatrix
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Data Matrix</h1>
<p>Data Matrix is a 2D barcode widely used in industrial and commercial applications for marking small items.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Simple Data Matrix
DataMatrixCode.Save("SERIAL-12345", "datamatrix.png");

// With specific size
DataMatrixCode.Save("SERIAL-12345", "datamatrix.svg", size: DataMatrixSize.Square24);</pre>

<h2>Use Cases</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>Electronics manufacturing</strong> - Component marking and tracking</li>
<li><strong>Healthcare</strong> - Medical device identification (UDI)</li>
<li><strong>Aerospace</strong> - Part serialization</li>
<li><strong>Postal services</strong> - High-density mail sorting</li>
</ul>

<h2>Features</h2>
<p>CodeGlyphX supports all standard Data Matrix sizes from 10x10 to 144x144 modules, including rectangular variants.</p>
