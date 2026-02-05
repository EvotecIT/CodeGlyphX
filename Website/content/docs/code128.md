---
title: Code 128 - CodeGlyphX
description: Code 128 / GS1-128 usage and character sets.
slug: code128
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Code 128 / GS1-128</h1>
<p>Code 128 is a high-density linear barcode supporting the full ASCII character set. GS1-128 is an application standard that uses Code 128.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Code 128
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "code128.png");

// GS1-128 with Application Identifiers
Barcode.Save(BarcodeType.Gs1128, "(01)09501101530003(17)250101", "gs1.png");</pre>

<h2>Character Sets</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Set</th>
<th style="text-align: left; padding: 0.75rem;">Characters</th>
<th style="text-align: left; padding: 0.75rem;">Best For</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><code>A</code></td>
<td style="padding: 0.75rem;">A-Z, 0-9, control chars</td>
<td style="padding: 0.75rem;">Alphanumeric with controls</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;"><code>B</code></td>
<td style="padding: 0.75rem;">A-Z, a-z, 0-9, symbols</td>
<td style="padding: 0.75rem;">General text (most common)</td>
</tr>
<tr>
<td style="padding: 0.75rem;"><code>C</code></td>
<td style="padding: 0.75rem;">00-99 (digit pairs)</td>
<td style="padding: 0.75rem;">Numeric data (most compact)</td>
</tr>
</tbody>
</table>
