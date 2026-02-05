---
title: Installation - CodeGlyphX
description: Install CodeGlyphX via NuGet and configure targets.
slug: installation
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Installation</h1>
<p>CodeGlyphX is available as a NuGet package and can be installed in several ways.</p>

<h2>.NET CLI</h2>
<pre class="code-block">dotnet add package CodeGlyphX</pre>

<h2>Package Manager Console</h2>
<pre class="code-block">Install-Package CodeGlyphX</pre>

<h2>PackageReference</h2>
<p>Add the following to your <code>.csproj</code> file:</p>
<pre class="code-block">&lt;PackageReference Include="CodeGlyphX" Version="*" /&gt;</pre>

<h2>Supported Frameworks</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>.NET 8.0+</strong> - Full support, no additional dependencies</li>
<li><strong>.NET Standard 2.0</strong> - Requires System.Memory 4.5.5</li>
<li><strong>.NET Framework 4.7.2+</strong> - Requires System.Memory 4.5.5</li>
</ul>

<h2>Feature Availability</h2>
<p>Most features are available across all targets, but the QR pixel pipeline and Span-based APIs are net8+ only.</p>
<table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
<thead>
<tr style="border-bottom: 1px solid var(--border);">
<th style="text-align: left; padding: 0.75rem;">Feature</th>
<th style="text-align: left; padding: 0.75rem;">net8.0+</th>
<th style="text-align: left; padding: 0.75rem;">net472 / netstandard2.0</th>
</tr>
</thead>
<tbody style="color: var(--text-muted);">
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Encode (QR/Micro QR + 1D/2D)</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Decode from module grids (BitMatrix)</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">Renderers + image file codecs</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">1D/2D pixel decode (Barcode/DataMatrix/PDF417/Aztec)</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">Yes</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">QR pixel decode from raw pixels / screenshots</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No (returns false)</td>
</tr>
<tr style="border-bottom: 1px solid var(--border);">
<td style="padding: 0.75rem;">QR pixel debug rendering</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No</td>
</tr>
<tr>
<td style="padding: 0.75rem;">Span-based overloads</td>
<td style="padding: 0.75rem;">Yes</td>
<td style="padding: 0.75rem;">No (byte[] only)</td>
</tr>
</tbody>
</table>
<p class="text-muted">QR pixel decode APIs are net8+ only (e.g., <code>QrImageDecoder.TryDecodeImage(...)</code> and <code>QrDecoder.TryDecode(...)</code> from pixels).</p>
<p class="text-muted">You can check capabilities at runtime via <code>CodeGlyphXFeatures</code> (for example, <code>SupportsQrPixelDecode</code> and <code>SupportsQrPixelDebug</code>).</p>
<p class="text-muted"><strong>Choosing a target:</strong> pick <code>net8.0+</code> for QR image decoding, pixel debug tools, Span APIs, and maximum throughput. Pick <code>net472</code>/<code>netstandard2.0</code> for legacy apps that only need encoding, rendering, and module-grid decode.</p>
