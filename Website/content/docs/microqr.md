---
title: Micro QR - CodeGlyphX
description: Micro QR generation and constraints.
slug: microqr
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Micro QR Code</h1>
<p>Micro QR is a smaller version of the standard QR code, designed for applications where space is limited.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Generate Micro QR
QR.Save("ABC123", "microqr.png", microQr: true);</pre>

<h2>Comparison with Standard QR</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Feature</th>
<th style="text-align: left; padding: 0.75rem;">Standard QR</th>
<th style="text-align: left; padding: 0.75rem;">Micro QR</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Finder patterns</td>
<td style="padding: 0.75rem;">3 corners</td>
<td style="padding: 0.75rem;">1 corner</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Max capacity</td>
<td style="padding: 0.75rem;">~3KB</td>
<td style="padding: 0.75rem;">~35 characters</td>
</tr>
<tr>
<td style="padding: 0.75rem;">Best for</td>
<td style="padding: 0.75rem;">General use</td>
<td style="padding: 0.75rem;">Small labels, PCBs</td>
</tr>
</tbody>
</table>
