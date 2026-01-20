# ROADMAP (TODO Only — Easiest → Hardest)

This list contains **only work we still want to do** (no already‑done items).

## Phase 1 — Quick wins (days)
- [x] Docs: add short decode/encode snippets for each symbology (3–5 lines).
- [x] Tests: golden vectors for all supported input image formats.
- [x] Decode: ensure image budget + downscale options are wired everywhere (audit remaining entrypoints).

## Phase 2 — Reader robustness (week)
- [x] 1D: improve scanline selection for low‑contrast images (more thresholds + adaptive).
- [x] Aztec: stronger bullseye detection on noisy screenshots.
- [x] PDF417: improve start‑pattern candidate ranking on skewed inputs.
- [x] Optional: expose “confidence/diagnostics” for non‑QR decoders.

## Phase 3 — Fancy QR styling (week)
- [x] Dot variants for module shapes (beyond scale/size tweaks).
- Gradients / multi‑color with safe defaults.
- Logo safety scoring + warnings in API.

## Phase 4 — Payload gaps (week)
- [x] Add missing payload helpers (e.g., PayPal payment intent).
- [x] Tighten validation + normalization for all payloads.

## Phase 5 — Additional symbologies (weeks)
- Add missing 1D symbologies: Pharmacode, Code32 (optional).
- Add 2D symbologies: MaxiCode (optional), MicroPDF417 (optional).
- Decoder test packs for any newly added symbologies.

## Phase 6 — Image formats (weeks)
- [x] ICO/CUR **decode** (read embedded PNG/BMP).
- [ ] WebP **decode** (lossless + VP8).
- [x] TIFF **decode** (baseline only).
- [ ] Optional: WebP **encode** (lossless first).

## Phase 7 — API polish (parallel)
- [x] Fluent builders for decode options (QR/1D/2D presets).
- [x] Stream‑first overloads where still missing.
- [x] Async decode helpers with cancellation.
- [x] Auto‑detect helpers: bytes → format → decode.

## Phase 8 — Performance + AOT (parallel)
- Reduce allocations in hot decode loops.
- Optional SIMD for thresholding where it helps.

## Phase 9 — Benchmarks + docs (ongoing)
- Benchmarks for encode + decode by symbology + format.
- Publish baseline results in README (summary table).
- Decision guide: “which symbology to pick”.
