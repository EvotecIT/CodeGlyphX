---
title: Downloads - CodeGlyphX
description: Download CodeGlyphX release assets grouped by release and package type.
slug: downloads
collection: pages
layout: page
meta.heading: Downloads
meta.lede: ZIP builds of every CodeGlyphX release, read from GitHub Releases. Most projects should install the NuGet package instead.
meta.glyph: datamatrix
meta.raw_html: true
meta.social: true
meta.social_description: Download CodeGlyphX release assets grouped by release and package type.
meta.structured_data: true
canonical: https://codeglyphx.com/downloads/
---

<div class="cgx-release-lead">
{{< release-button placement="downloads.primary" >}}
  <a class="cgx-btn cgx-btn--quiet" href="/docs/installation/">Install from NuGet</a>
  <p>Release notes are in the <a class="cgx-link" href="/changelog/">changelog</a>; every asset also lives on <a class="cgx-link" href="https://github.com/EvotecIT/CodeGlyphX/releases">GitHub Releases</a>.</p>
</div>

<section class="cgx-section" aria-labelledby="dl-ways">
  <div class="cgx-section__head">
    <h2 id="dl-ways">Four ways in</h2>
    <p>Each code below was drawn by CodeGlyphX and opens the place it names.</p>
  </div>
{{< cgx-channels >}}
</section>

<section class="cgx-section" aria-labelledby="dl-release">
  <div class="cgx-section__head">
    <h2 id="dl-release">By release</h2>
    <p>Stable assets, newest first.</p>
  </div>
{{< release-buttons placement="downloads.stable_by_release" >}}
</section>

<section class="cgx-section" aria-labelledby="dl-kind">
  <div class="cgx-section__head">
    <h2 id="dl-kind">By package type</h2>
    <p>The same assets grouped by what they contain.</p>
  </div>
{{< release-buttons placement="downloads.stable_by_kind" >}}
</section>