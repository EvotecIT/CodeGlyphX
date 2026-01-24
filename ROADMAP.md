# ROADMAP (TODO Only)

This list contains **only work we still want to do** (no already‑done items).

## Now — Benchmarks & Clarity (days → weeks)
- Benchmark scenarios: add **stress + realism packs** (QR with logo overlay, scaled/resampled UI shots, higher noise/antialias, long payloads, quiet‑zone variants). Clearly label “ideal” vs “stress” in reports.
- Benchmark controls: add scenario packs + filters so quick runs stay short while full runs include stress cases.
- Benchmark reliability: enforce minimum iteration time for quick runs (or increase ops); keep quick/full guidance current; capture environment metadata in reports.
- Reporting clarity: add a support matrix (net472 vs net8/10) and a “what these results mean” disclaimer for each pack.

## Now — Platform/UX Decisions (days → weeks)
- net472 parity: document what’s missing vs net8/10 and decide what can be back‑ported without hurting perf.
- Fluent API: audit current API usage and decide if a builder/DSL adds value or just surface area.
- Compatibility guidance: publish “choose net472 vs net8/10” decision notes (features, speed, dependencies).

## Next — Performance & AOT (weeks)
- Mask-specialized decode traversal to remove per-cell mask math in QR data extraction.
- Reduce allocations in hot decode loops (pool block buffers, reuse scratch grids).
- Optional SIMD for thresholding/binarization where it wins on real inputs.
- Evaluate AOT impact on cold start and size; document any AOT‑safe paths.

## DX & Docs (ongoing)
- Publish benchmark summary + support matrix in README (link to `BENCHMARK.md` for full tables).
- Keep “quick vs full” benchmark guidance and preflight steps in README/docs.
- Decision guide: “which symbology to pick” + “which target framework to pick”.

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
