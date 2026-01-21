# ROADMAP (TODO Only — Easiest → Hardest)

This list contains **only work we still want to do** (no already‑done items).

## Phase 4 — API & DX expansion (days → week)
_No open items._

## Phase 5 — Additional symbologies (weeks)
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

## Phase 6 — Image formats (weeks)
- WebP **decode** (lossless + VP8).
- Optional: WebP **encode** (lossless first).

## Phase 8 — Performance + AOT (parallel)
- Reduce allocations in hot decode loops.
- Optional SIMD for thresholding where it helps.

## Phase 9 — Benchmarks + docs (ongoing)
- Benchmarks for encode + decode by symbology + format.
- Publish baseline results in README (summary table).
- Decision guide: “which symbology to pick”.
