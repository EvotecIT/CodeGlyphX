---
title: EAN / UPC - CodeGlyphX
description: EAN and UPC formats overview.
slug: ean-upc
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>EAN / UPC Barcodes</h1>
<p>EAN (European Article Number) and UPC (Universal Product Code) are the standard retail barcodes found on consumer products worldwide.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// EAN-13 (International)
Barcode.Save(BarcodeType.EAN, "5901234123457", "ean13.png");

// EAN-8 (Smaller packages)
Barcode.Save(BarcodeType.EAN, "96385074", "ean8.png");

// UPC-A (North America)
Barcode.Save(BarcodeType.UPCA, "012345678905", "upca.png");

// UPC-E (Compact)
Barcode.Save(BarcodeType.UPCE, "01234565", "upce.png");</pre>

<h2>Format Guide</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Type</th>
<th style="text-align: left; padding: 0.75rem;">Digits</th>
<th style="text-align: left; padding: 0.75rem;">Region</th>
<th style="text-align: left; padding: 0.75rem;">Use Case</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">EAN-13</td>
<td style="padding: 0.75rem;">13</td>
<td style="padding: 0.75rem;">International</td>
<td style="padding: 0.75rem;">Standard retail products</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">EAN-8</td>
<td style="padding: 0.75rem;">8</td>
<td style="padding: 0.75rem;">International</td>
<td style="padding: 0.75rem;">Small packages</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">UPC-A</td>
<td style="padding: 0.75rem;">12</td>
<td style="padding: 0.75rem;">North America</td>
<td style="padding: 0.75rem;">Retail products</td>
</tr>
<tr>
<td style="padding: 0.75rem;">UPC-E</td>
<td style="padding: 0.75rem;">8</td>
<td style="padding: 0.75rem;">North America</td>
<td style="padding: 0.75rem;">Small items</td>
</tr>
</tbody>
</table>
