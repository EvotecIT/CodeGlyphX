---
title: Aztec - CodeGlyphX
description: Aztec encoding basics and use cases.
slug: aztec
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Aztec Code</h1>
<p>Aztec is a 2D matrix barcode designed for high readability even when printed at low resolution or on curved surfaces.</p>

<h2>Basic Usage</h2>
<pre class="code-block">using CodeGlyphX;

// Simple Aztec code
AztecCode.Save("Ticket: CONF-2024-001", "aztec.png");

// With error correction percentage
AztecCode.Save("Ticket data", "aztec.png", errorCorrectionPercent: 33);</pre>

<h2>Use Cases</h2>
<ul style="color: var(--text-muted); margin-left: 1.5rem;">
<li><strong>Transportation</strong> - Train and airline tickets</li>
<li><strong>Event tickets</strong> - Mobile ticketing apps</li>
<li><strong>Patient wristbands</strong> - Healthcare identification</li>
<li><strong>Curved surfaces</strong> - Bottles, tubes, cylinders</li>
</ul>

<h2>Advantages</h2>
<p>Aztec codes don't require a quiet zone around them and can be read even when partially damaged, making them ideal for mobile ticketing.</p>
