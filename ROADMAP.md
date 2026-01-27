# ROADMAP

This list is mostly **work we still want to do**. The "Recently completed" section is just context.

## Recently completed (for context)
- Benchmark controls + filters for quick vs full runs, plus report clarity/metadata.
- net472 parity documentation (feature matrix + guidance).
- Added stylized art decode pack to scenario runs with pass‑rate + timing summaries.
- CI: PR website build check (static output) before merge.

## Now — High-Impact Wins (days → weeks)
- Benchmark scenarios: extend **stress + realism packs** (more scaled/resampled UI shots, higher noise/antialias, long payloads, quiet‑zone variants).
- Decoder robustness: close remaining heavy-illustration failures in `Assets/DecodingSamples` (targets: qr-art-facebook-splash-grid.png, qr-art-montage-grid.png, qr-art-stripe-eye-grid.png, qr-art-drip-variants.png, qr-art-solid-bg-grid.png, qr-art-gear-illustration-grid.png).
- Decoder robustness: add multi-scale search + adaptive binarization tuned for large, stylized QR inputs; document tradeoffs and expected decode times.

## Near-term (weeks)
### Benchmarks & reporting
- Stress/realism pack expansion with clear "ideal vs stress" labels in reports.
- Keep quick/full guidance current and add a short "how to interpret" blurb per pack.
- Add environment metadata (CPU, OS, runtime) into summaries for quick runs.

### Decoder robustness
- Close the remaining heavy-illustration failures in `Assets/DecodingSamples`.
- Add lightweight multi-scale search before heavy fallback paths to improve screenshots.
- Tune adaptive binarization for stylized inputs; capture perf tradeoffs in docs.

### Performance
- Reduce allocations in hot decode loops (pool block buffers, reuse scratch grids).
- Mask-specialized decode traversal to remove per-cell mask math.
- Optional SIMD for thresholding/binarization where it wins on real inputs.

### Developer experience
- Add regression tests that mirror the website flow (round-trip encode/decode, no committed large binaries).
- Improve console examples (ASCII QR render + screenshot decode walkthrough).

## Next — QR Styling & Presets (weeks)
### End-to-end “Fancy QR” pipeline (for Image #1/#2/#3/#4 style boards)
**Phase 1 — Core rendering options (must come first)**
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
- Add a dedicated “style gallery” page (beyond the home section) if we want a full catalog view.

**Phase 5 — UI integration (last)**
- Playground: add template selector + live preview with preset export/import.
- Website: add marketing gallery + download links (use generated assets, not manual images).

## Mid-term (months)
### Image formats
- WebP decode (lossless + VP8).
- Optional WebP encode (lossless first).
- Expand format corpus (JPEG progressive/CMYK, GIF variants, BMP/ICO edge cases) and schedule a periodic CI run.

### Platform & runtime
- Evaluate AOT impact on cold start and size; document any AOT-safe paths.
- Investigate net472 back-ports that do not hurt perf (targeted, opt-in).

### Symbologies
- Add 2D/stacked formats in a staged order (MaxiCode, Code 16K/49, Codablock F, DotCode, Micro Data Matrix, rMQR, Grid Matrix, Han Xin, GS1 Composite).
- Build decoder test packs + golden vectors for each new format.

## Later — Illustrated QR Art (backlog)
- Sprite/atlas renderer for illustrated module tiles (no API calls).
- Optional AI overlay mode (experimental), gated behind safety checks and decode validation.

## DX & Docs (ongoing)
- Keep “quick vs full” benchmark guidance and preflight steps in README/docs.
- Decision guide: “which symbology to pick” + “which target framework to pick”.
- Publish a supported format matrix + known gaps; keep docs/FAQ in sync.

## Wins by size (guide)
### Fast wins (hours)
- Examples polish: console QR render + screenshot decode example.
- Add decode regression tests without committing large binaries.
- Small benchmark report wording/legend tweaks.

### Small wins (1-3 days)
- Stress/realism pack expansion (scaled UI shots, noise, quiet-zone variants).
- Pack labeling in reports and website tables.
- Lightweight multi-scale decode pass before fallback.

### Big wins (1-3 weeks)
- Close heavy-illustration decode failures in `Assets/DecodingSamples`.
- Mask-specialized traversal + allocation reductions in hot decode loops.
- SIMD binarization for net8+ when it wins on real inputs.
