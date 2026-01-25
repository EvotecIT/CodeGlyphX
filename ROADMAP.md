# ROADMAP (TODO Only)

This list contains **only work we still want to do** (no already‑done items).

## Now — Benchmarks & Clarity (days → weeks)
- Benchmark scenarios: extend **stress + realism packs** (scaled/resampled UI shots, higher noise/antialias, long payloads, quiet‑zone variants). Clearly label “ideal” vs “stress” in reports.
- Benchmark controls: add scenario packs + filters so quick runs stay short while full runs include stress cases.
- Benchmark reliability: enforce minimum iteration time for quick runs (or increase ops); keep quick/full guidance current; capture environment metadata in reports.
- Reporting clarity: add pack labels + “what these results mean” disclaimers per pack.

## Now — Platform/UX Decisions (days → weeks)
- net472 parity: document what’s missing vs net8/10 and decide what can be back‑ported without hurting perf.

## Next — Performance & AOT (weeks)
- Mask-specialized decode traversal to remove per-cell mask math in QR data extraction.
- Reduce allocations in hot decode loops (pool block buffers, reuse scratch grids).
- Optional SIMD for thresholding/binarization where it wins on real inputs.
- Evaluate AOT impact on cold start and size; document any AOT-safe paths.

## Next — QR Styling & Presets (weeks)
### End-to-end “Fancy QR” pipeline (for Image #1/#2/#3/#4 style boards)
**Phase 1 — Core rendering options (must come first)**
- Add remaining module shapes (soft-diamond, leaf, wave, blob) with PNG/SVG/HTML parity.
- Add per-module color rules beyond palette zones (by mask bit/position, optional finder proximity rules).
- Add safe defaults for logo overlays + padding (sizing presets + ECC/version guidance).
- Add optional eye palette modes (multi-color) and finalize preset coverage for eye styles.

**Phase 2 — Safety + constraints**
- Add scan-safety constraints: minimum eye clarity, timing pattern protection, and eye contrast checks.
- Add timing/eye protection warnings surfaced in the safety report.

**Phase 3 — Presets + assets**
- Define preset schema (serializable) for full style (modules/eyes/palette/background/logo).
- Preset pack v1 (names + intent):
  - Classic, Bold, Soft, Dot, Grid, Tech, Stamp, Poster, Neon, Minimal, Sticker, Candy, Mono-High.
- Shape set v1 (modules + eyes):
  - Modules: square, rounded, circle, dot, squircle, diamond, soft-diamond, leaf, wave, blob, dot-grid.
  - Eyes (frame/ball): square, rounded, circle, squircle, diamond, bracket, double-ring, target, badge.
- Add curated palette sets (brand, neon, pastel, mono-contrast, duotone).

**Phase 4 — Examples + gallery (after core options are in)**
- Produce a versioned “style board” asset pack for docs/website (wire build copy targets).
- Add a “style gallery” page that consumes the generated assets.

**Phase 5 — UI integration (last)**
- Playground: add template selector + live preview with preset export/import.
- Website: add marketing gallery + download links (use generated assets, not manual images).

## DX & Docs (ongoing)
- Keep “quick vs full” benchmark guidance and preflight steps in README/docs.
- Decision guide: “which symbology to pick” + “which target framework to pick”.
- CI: add PR website build check (compile + static output) before merge.
- Publish a supported format matrix + known gaps; keep docs/FAQ in sync.

## Additional symbologies (weeks)
- MaxiCode — decode + encode.
- Code 16K + Code 49 (stacked 1D).
- Codablock F (stacked).
- DotCode.
- Micro Data Matrix.
- rMQR (rectangular Micro QR).
- Grid Matrix.
- Han Xin.
- GS1 Composite (CC-A / CC-B / CC-C) — decode first, then encode.
- Decoder test packs + golden vectors for any newly added symbologies.

## Image formats (weeks)
- WebP **decode** (lossless + VP8).
- Optional: WebP **encode** (lossless first).
- Expand the image format corpus (JPEG progressive/CMYK, GIF variants, BMP/ICO edge cases) and add a scheduled CI run.
