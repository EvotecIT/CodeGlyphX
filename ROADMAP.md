# ROADMAP (TODO Only — Easiest → Hardest)

This list contains **only work we still want to do** (no already‑done items).

## Phase 4 — API & DX expansion (days → week)
- High-level image decode overloads that accept `BarcodeDecodeOptions` (tile scan + checksum policies).
- Fluent builders for render options (PNG/SVG/HTML/PDF) to match decode fluents.
- Add `DecodeResult<T>` + `DecodeFailureReason` for richer diagnostics (elapsed, failure, format).
- Add `ImageInfo`/header preflight (dimensions + format) before full decode.
- Add `TryDecodeAll` multi-code APIs for Data Matrix / PDF417 / Aztec images.
- Span/Memory overloads for image decode across formats (avoid `byte[]` copy paths).
- Batch decode API with shared settings + aggregated diagnostics.

## Phase 5 — Additional symbologies (weeks)
- GS1 DataBar family (Omni/Truncated/Stacked/Expanded).
- GS1 Composite (CC-A / CC-B / CC-C).
- MicroPDF417 + Macro PDF417.
- MaxiCode (optional).
- DotCode (optional).
- Postal: USPS IMB, POSTNET/PLANET (optional).
- Industrial 2‑of‑5 variants (Interleaved 2‑of‑5 beyond ITF‑14, Matrix 2‑of‑5).
- Telepen (optional), Patch Code (optional).
- Pharmacode (one‑track / two‑track), Code32 (optional).
- Decoder test packs for any newly added symbologies.

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
