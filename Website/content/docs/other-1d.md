---
title: Other 1D Symbologies - CodeGlyphX
description: Additional 1D and postal barcode formats.
slug: other-1d
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Other 1D &amp; Postal Barcodes</h1>
<p>CodeGlyphX supports a wide range of additional 1D symbologies beyond the most common retail formats.</p>
<p><strong>Note:</strong> Some postal and stacked formats use the matrix pipeline and must be rendered with <code>MatrixBarcodeEncoder</code> + matrix renderers.</p>

<h2>Supported Types</h2>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Category</th>
<th style="text-align: left; padding: 0.75rem;">Symbologies</th>
<th style="text-align: left; padding: 0.75rem;">Notes</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Interleaved / 2-of-5</td>
<td style="padding: 0.75rem;">ITF, ITF-14, Industrial 2-of-5, Matrix 2-of-5, IATA 2-of-5</td>
<td style="padding: 0.75rem;">Logistics, cartons, older systems</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Specialty / Legacy</td>
<td style="padding: 0.75rem;">Codabar, MSI, Code 11, Plessey, Telepen, Code 32</td>
<td style="padding: 0.75rem;">Warehousing, libraries, healthcare</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Pharma</td>
<td style="padding: 0.75rem;">Pharmacode (one-track), Pharmacode (two-track)</td>
<td style="padding: 0.75rem;">Packaging and pharmaceuticals</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Postal</td>
<td style="padding: 0.75rem;">POSTNET, PLANET, Royal Mail 4-State, Australia Post, Japan Post, USPS IMB</td>
<td style="padding: 0.75rem;">Matrix encoder required</td>
</tr>
<tr>
<td style="padding: 0.75rem;">GS1 DataBar</td>
<td style="padding: 0.75rem;">DataBar Truncated, Omni, Stacked, Expanded, Expanded Stacked</td>
<td style="padding: 0.75rem;">Omni/Stacked/Expanded Stacked use matrix encoder</td>
</tr>
</tbody>
</table>

<h2>Examples</h2>
<pre class="code-block">using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

Barcode.Save(BarcodeType.ITF14, "12345678901231", "itf14.png");
Barcode.Save(BarcodeType.Codabar, "A40156B", "codabar.png");
Barcode.Save(BarcodeType.Telepen, "TELEPEN-123", "telepen.png");
Barcode.Save(BarcodeType.GS1DataBarExpanded, "01095011015300031725010110ABC123", "gs1-expanded.png");

var imb = MatrixBarcodeEncoder.EncodeUspsImb("0123456709498765432101234567891");
MatrixPngRenderer.RenderToFile(
    imb,
    new MatrixPngRenderOptions { ModuleSize = 2, QuietZone = 2 },
    "usps-imb.png");</pre>
