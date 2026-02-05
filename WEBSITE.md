# CodeGlyphX Website (PowerForge.Web)

This repo now uses the PowerForge.Web pipeline in `Website/` as the single source of truth
for the CodeGlyphX website. The legacy website build scripts under `Build/` were removed.

## Build
```powershell
pwsh Website/build.ps1
pwsh Website/build.ps1 -Serve
```

If PowerForge.Web.Cli is not found, set `POWERFORGE_ROOT` to the PSPublishModule repo:
```powershell
$env:POWERFORGE_ROOT = "C:\Support\GitHub\PSPublishModule"
pwsh Website/build.ps1
```

## Structure
- `Website/content/pages/` – homepage + marketing pages (FAQ, pricing, showcase, benchmarks)
- `Website/content/docs/` – documentation pages (one file = one page)
- `Website/data/` – JSON data for sections (FAQ, benchmarks, stats, etc.)
- `Website/themes/codeglyphx/` – theme layouts/partials
- `Website/static/` – assets, API fragments, vendor libs, CNAME

## What drives what
- **Docs + pages**: edit markdown under `Website/content/`.
- **Data-driven sections**: edit JSON under `Website/data/` (FAQ, stats, pricing, feature groups).
- **Benchmarks**: UI comes from `Website/data/benchmarks.json`; the live tables read JSON from
  `Website/static/data/benchmark*.json`.
- **API reference**: generated from `CodeGlyphX` XML + assembly into `/api`
  using `Website/static/api-fragments/*` for header/footer.
- **Playground**: published from the Blazor project (`CodeGlyphX.Website`) into `/playground`
  during the pipeline.

## Pipeline (Website/pipeline.json)
1) Build static pages into `Website/_site`
2) Build CodeGlyphX (Release)
3) Publish Blazor playground into `/playground`
4) Generate API reference into `/api` (docs template + JSON)
5) Generate `llms.txt` and `sitemap.xml`
6) Optimize + audit

## API Reference
The API reference is generated under `/api`. `/docs/api` is a redirect for legacy links.

## Notes
- The Blazor project (`CodeGlyphX.Website`) is still used for the playground publish step.
- Generated outputs live in `Website/_site` and are ignored by git.
- If you change only website content or styling, edit `Website/` and rebuild — no other folders are required.

---

# Website Improvement Ideas

## Current State Assessment

The website has a solid foundation: landing page with feature sections, 15 docs pages covering core API surface, showcase page with theme screenshots, playground (iframe to Blazor app), and API reference generated from XML docs. Dark/light theme support is working. Inter font is self-hosted.

### What's Working Well
- Clean visual design with consistent purple accent
- Comprehensive docs covering QR, Barcode, DataMatrix, PDF417, Aztec, Decoding
- Showcase page with 40+ style board images and theme screenshots
- API reference with sidebar navigation, member filtering, code examples
- Playground integration via iframe

### Content Gaps
- No visual getting-started guide (screenshots of output)
- No integration guides (ASP.NET, Blazor, MAUI, WPF)
- No migration/comparison content
- No cookbook/recipes for common tasks
- No changelog or release notes page
- No troubleshooting/FAQ page
- Docs code blocks lack language annotation (Prism highlighting doesn't activate)

---

## Ideas

### 1. Visual Getting Started Guide

**Priority**: High

Add a step-by-step onboarding page with actual output screenshots showing what each code snippet produces. Current docs show code but not the resulting images.

Content:
- Install CodeGlyphX (terminal screenshot or styled command block)
- Generate your first QR code (code + resulting PNG screenshot)
- Generate a barcode (code + resulting barcode image)
- Save as SVG/PDF (code + rendered output)
- Decode an existing code (code + result text)

Each step should be a card with code on one side and the visual output on the other (or stacked on mobile).

### 2. Output Gallery / Examples Page

**Priority**: High

A visual gallery showing the range of what CodeGlyphX can produce. Users landing on the site want to see what's possible before reading docs.

Sections:
- QR code styles (rounded modules, colored eyes, gradient fills, logo overlay)
- Barcode types side by side (Code128, EAN-13, UPC-A, Code39, etc.)
- 2D codes (DataMatrix, PDF417, Aztec) with size comparisons
- Format outputs (PNG vs SVG vs PDF rendering)
- Error correction levels visualized (L/M/Q/H with increasing damage)

Could reuse some showcase style-board images but organized by category rather than theme.

### 3. Integration Guides

**Priority**: Medium-High

Dedicated pages for each platform showing real-world usage patterns:

- **ASP.NET Core / Minimal API** - Generate QR on-the-fly, serve as image endpoint, caching strategies
- **Blazor** - Data URI approach, component wrapper, SignalR streaming for batch generation
- **MAUI** - Image binding, platform-specific considerations, AOT notes
- **WPF** - BitmapImage conversion, MVVM binding pattern
- **Console / CLI tools** - Batch generation scripts, piping output
- **Azure Functions** - Serverless QR generation, blob storage output

Each guide should have a complete working example, not just snippets.

### 4. Cookbook / Recipes

**Priority**: Medium

Short, focused how-to articles for specific tasks:

- WiFi QR code for guest network
- vCard QR for business cards (with print sizing advice)
- TOTP/2FA setup QR for authentication apps
- SEPA/Girocode payment QR
- Inventory/asset labels with DataMatrix
- Shipping labels with Code128/GS1-128
- Batch generation from CSV/Excel data
- Custom styling: branded QR codes with company colors
- QR with logo overlay (center image)
- High-DPI output for print vs screen
- Smallest possible QR (optimizing data capacity)

### 5. Comparison / Migration Page

**Priority**: Medium

Help users coming from other libraries:

- **QRCoder migration guide** - API mapping, feature comparison
- **ZXing.Net comparison** - When to use which
- **SkiaSharp-based solutions** - Dependency comparison
- **JavaScript libraries** - Why server-side generation matters

Include a feature comparison table (zero-dependency, AOT support, format coverage, decode support).

### 6. Performance & Sizing Guide

**Priority**: Medium

Technical reference for production use:

- Benchmark results (generation time by size/format)
- Memory allocation characteristics
- Recommended pixel sizes for print (300 DPI) vs screen (72-96 DPI)
- QR version selection guide (data capacity by version)
- Error correction trade-offs (capacity vs resilience)
- AOT compilation results and binary size

### 7. Changelog / Release Notes

**Priority**: Medium

A `/changelog/` page showing version history:

- Version number, date, highlights
- Breaking changes called out clearly
- Links to GitHub releases for full details
- Could be auto-generated from GitHub releases API or a CHANGELOG.md

### 8. Troubleshooting / FAQ

**Priority**: Medium-Low

Common questions and issues:

- "QR code won't scan" - Check error correction, size, quiet zone
- "Barcode doesn't validate" - Check digit issues, data format requirements
- "Image is blurry" - Pixel size too small, scaling artifacts
- ".NET Framework compatibility" - System.Memory dependency note
- "AOT trimming warnings" - How to resolve
- "Decode returns nothing" - Image quality, contrast, angle considerations

### 9. Architecture / How It Works

**Priority**: Low

For technically curious users:

- How QR encoding works (data analysis, error correction, masking)
- Reed-Solomon error correction explained
- Why zero dependencies matters (no ImageSharp, no SkiaSharp)
- How the PNG encoder works without System.Drawing
- Decode pipeline (binarization, finder patterns, perspective correction)

### 10. Docs Code Block Improvements

**Priority**: High (Technical)

Current docs pages use `<pre class="code-block">` without language annotation. Prism.js can't detect the language and falls back to plain text (no highlighting).

Options:
- Add `language-csharp` class to code blocks in all 15 docs pages
- Or update the engine/template to auto-add language class based on content detection
- Or change site.js default from `language-plain` to `language-csharp`

### 10.1. Docs Markdown Migration (future)

**Priority**: Medium

Convert HTML-heavy docs (`<pre class="code-block">`, inline `<h1>`, `<p>`, etc.)
to proper Markdown with fenced code blocks and language tags. This keeps content
clean, makes syntax highlighting reliable, and removes the need for raw HTML.

### 11. Interactive Examples Enhancement

**Priority**: Low

The playground page embeds the Blazor app via iframe. Could enhance with:
- Pre-loaded example URLs that demonstrate specific features
- "Copy to playground" buttons on docs code snippets
- Share links for playground configurations

### 12. SEO & Social

**Priority**: Low

- Add Open Graph / Twitter Card meta tags with QR code preview images
- Add structured data (JSON-LD) for software library
- Generate a sitemap.xml
- Add canonical URLs to docs pages

---

## Content Assets Needed

| Asset | Purpose | Format |
|-------|---------|--------|
| QR output screenshots | Getting started, gallery | PNG (light + dark bg) |
| Barcode type samples | Gallery, docs | PNG |
| 2D code samples | Gallery, docs | PNG |
| Integration code projects | Guides | GitHub links or inline |
| Benchmark charts | Performance page | SVG or inline |
| Architecture diagrams | How it works | SVG |
| Error correction comparison | FAQ, docs | PNG (damaged QR examples) |

## Priority Order

1. Docs code highlighting fix (technical, high impact, low effort)
2. Visual getting started guide (first impression for new users)
3. Output gallery (showcases capabilities)
4. Integration guides (ASP.NET Core + Blazor first)
5. Cookbook recipes (WiFi, vCard, TOTP first)
6. Comparison/migration page
7. Changelog
8. Troubleshooting/FAQ
9. Everything else
