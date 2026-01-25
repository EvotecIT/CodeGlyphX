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
- Expand module shapes beyond square/rounded/circle (dot, blob, diamond, leaf, wave, etc.).
- Separate eye frame + eye ball shape palettes with presets.
- Curved “soft” corners for frames/eyes (vector + raster parity).
- More logo treatments (background plate variants, inner padding presets, safe defaults).
- Gradient presets and multi-color palettes for modules/eyes.
- Add QR style presets gallery (exportable settings) for consistent “high-quality” looks.

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
