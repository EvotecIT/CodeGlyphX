# ROADMAP

This list is mostly **work we still want to do**. The "Recently completed" section is just context.

## Recently completed (for context)
- Benchmark controls + filters for quick vs full runs, plus report clarity/metadata.
- net472 parity documentation (feature matrix + guidance).
- Added stylized art decode pack to scenario runs with pass‑rate + timing summaries.
- CI: PR website build check (static output) before merge.

## Now — High-Impact Wins (days → weeks)
- Benchmark scenarios: extend **stress + realism packs** (more scaled/resampled UI shots, higher noise/antialias, long payloads, quiet‑zone variants). [bench]
- Decoder robustness: close remaining heavy-illustration failures in `Assets/DecodingSamples` (targets: qr-art-facebook-splash-grid.png, qr-art-montage-grid.png, qr-art-stripe-eye-grid.png, qr-art-drip-variants.png, qr-art-solid-bg-grid.png, qr-art-gear-illustration-grid.png). [decoder]
- Decoder robustness: add multi-scale search + adaptive binarization tuned for large, stylized QR inputs; document tradeoffs and expected decode times. [decoder]

## Near-term (weeks)
### Benchmarks & reporting
- Stress/realism pack expansion with clear "ideal vs stress" labels in reports. [bench]
- Keep quick/full guidance current and add a short "how to interpret" blurb per pack. [bench][docs]
- Add environment metadata (CPU, OS, runtime) into summaries for quick runs. [bench]

### Decoder robustness
- Close the remaining heavy-illustration failures in `Assets/DecodingSamples`. [decoder]
- Add lightweight multi-scale search before heavy fallback paths to improve screenshots. [decoder]
- Tune adaptive binarization for stylized inputs; capture perf tradeoffs in docs. [decoder][docs]

### Performance
- Reduce allocations in hot decode loops (pool block buffers, reuse scratch grids). [perf]
- Mask-specialized decode traversal to remove per-cell mask math. [perf]
- Optional SIMD for thresholding/binarization where it wins on real inputs. [perf]

### Developer experience
- Add regression tests that mirror the website flow (round-trip encode/decode, no committed large binaries). [dx][tests]
- Improve console examples (ASCII QR render + screenshot decode walkthrough). [dx]

## Next — QR Styling & Presets (weeks)
### End-to-end “Fancy QR” pipeline (for Image #1/#2/#3/#4 style boards)
**Phase 1 — Core rendering options (must come first)**
- Add per-module color rules beyond palette zones (by mask bit/position, optional finder proximity rules). [style]
- Add safe defaults for logo overlays + padding (sizing presets + ECC/version guidance). [style]
- Add optional eye palette modes (multi-color) and finalize preset coverage for eye styles. [style]

**Phase 2 — Safety + constraints**
- Add scan-safety constraints: minimum eye clarity, timing pattern protection, and eye contrast checks. [style][safety]
- Add timing/eye protection warnings surfaced in the safety report. [style][safety]

**Phase 3 — Presets + assets**
- Define preset schema (serializable) for full style (modules/eyes/palette/background/logo). [style]
- Preset pack v1 (names + intent): [style]
  - Classic, Bold, Soft, Dot, Grid, Tech, Stamp, Poster, Neon, Minimal, Sticker, Candy, Mono-High.
- Shape set v1 (modules + eyes): [style]
  - Modules: square, rounded, circle, dot, squircle, diamond, soft-diamond, leaf, wave, blob, dot-grid.
  - Eyes (frame/ball): square, rounded, circle, squircle, diamond, bracket, double-ring, target, badge.
- Add curated palette sets (brand, neon, pastel, mono-contrast, duotone). [style]

**Phase 4 — Examples + gallery (after core options are in)**
- Add a dedicated “style gallery” page (beyond the home section) if we want a full catalog view. [style][ux]

**Phase 5 — UI integration (last)**
- Playground: add template selector + live preview with preset export/import. [style][ux]
- Website: add marketing gallery + download links (use generated assets, not manual images). [style][ux]

## Mid-term (months)
### Image formats
- WebP decode (lossless + VP8). [formats][webp]
- Optional WebP encode (lossless first). [formats][webp]
- Expand format corpus (JPEG progressive/CMYK, GIF variants, BMP/ICO edge cases) and schedule a periodic CI run. [formats][ci]

### Platform & runtime
- Evaluate AOT impact on cold start and size; document any AOT-safe paths. [aot]
- Investigate net472 back-ports that do not hurt perf (targeted, opt-in). [platform]

### Symbologies
- Add 2D/stacked formats in a staged order (MaxiCode, Code 16K/49, Codablock F, DotCode, Micro Data Matrix, rMQR, Grid Matrix, Han Xin, GS1 Composite). [symbology]
- Build decoder test packs + golden vectors for each new format. [symbology][tests]

## Later — Illustrated QR Art (backlog)
- Sprite/atlas renderer for illustrated module tiles (no API calls). [art]
- Optional AI overlay mode (experimental), gated behind safety checks and decode validation. [art][safety]

## DX & Docs (ongoing)
- Keep “quick vs full” benchmark guidance and preflight steps in README/docs. [docs]
- Decision guide: “which symbology to pick” + “which target framework to pick”. [docs]
- Publish a supported format matrix + known gaps; keep docs/FAQ in sync. [docs]

## Wins by size (guide)
### Fast wins (hours)
- Examples polish: console QR render + screenshot decode example. [dx]
- Add decode regression tests without committing large binaries. [tests]
- Small benchmark report wording/legend tweaks. [bench][docs]

### Small wins (1-3 days)
- Stress/realism pack expansion (scaled UI shots, noise, quiet-zone variants). [bench]
- Pack labeling in reports and website tables. [bench][docs]
- Lightweight multi-scale decode pass before fallback. [decoder]

### Big wins (1-3 weeks)
- Close heavy-illustration decode failures in `Assets/DecodingSamples`. [decoder]
- Mask-specialized traversal + allocation reductions in hot decode loops. [perf]
- SIMD binarization for net8+ when it wins on real inputs. [perf]
