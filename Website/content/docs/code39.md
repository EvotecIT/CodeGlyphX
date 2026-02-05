---
title: Code 39 - CodeGlyphX
description: Code 39 / 93 usage and comparisons.
slug: code39
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Code 39 / Code 93</h1>
<p>Code 39 and Code 93 are widely used linear barcodes, particularly in automotive and defense industries.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Code 39
Barcode.Save(BarcodeType.Code39, "HELLO-123", "code39.png");

// Code 93 (more compact)
Barcode.Save(BarcodeType.Code93, "HELLO-123", "code93.png");</pre>

<h2>Valid Characters</h2>
<p>Code 39 supports: <code>A-Z</code>, <code>0-9</code>, <code>-</code>, <code>.</code>, <code>$</code>, <code>/</code>, <code>+</code>, <code>%</code>, <code>SPACE</code></p>
<p style="margin-top: 0.5rem;"><strong>Note:</strong> Lowercase letters are automatically converted to uppercase.</p>

<h2>Comparison</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Feature</th>
<th style="text-align: left; padding: 0.75rem;">Code 39</th>
<th style="text-align: left; padding: 0.75rem;">Code 93</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Density</td>
<td style="padding: 0.75rem;">Lower</td>
<td style="padding: 0.75rem;">~40% more compact</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Checksum</td>
<td style="padding: 0.75rem;">Optional</td>
<td style="padding: 0.75rem;">Mandatory (2 chars)</td>
</tr>
<tr>
<td style="padding: 0.75rem;">Industry</td>
<td style="padding: 0.75rem;">Automotive, Defense</td>
<td style="padding: 0.75rem;">Logistics, Postal</td>
</tr>
</tbody>
</table>
