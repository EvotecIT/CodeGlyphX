# CodeGlyphX TODO (Easiest → Hardest)

## Phase 0 — Platform clarity
- [ ] Docs: add a platform/renderer support matrix (Windows/Linux/macOS + WPF).
- [ ] APIs: clearly mark platform-specific features (WPF/Windows-only).

## Phase 1 — Easy wins (days)
- [x] Reader: polish diagnostics (step-by-step failure reasons surfaced).
- [x] Reader: more tolerant format-info handling (soft fallback when distance > 3).
- [x] QR payloads: one-liner helpers for URL/Text/WiFi/Email/Phone/SMS/Geo/Contact/Calendar/OTP + social.
- [x] Render IO: unify Write/Read helpers for binary/text/streams everywhere.
- [x] Render defaults: consistent module size, quiet zone, and safe default colors.
- [x] Tests: smoke tests for core payload helpers + renderer format.
- [x] Docs: minimal “3 lines to result” examples per format.

## Phase 2 — Solid QR decoding (week)
- [x] QR decode: stronger format-info reconciliation (both copies, validate both).
- [x] QR decode: candidate mask retry (top candidates with RS/payload validation).
- [x] QR decode: structured append metadata (sequence/parity).
- [x] QR decode: ECI segmentation decode + per-segment text assembly.
- [x] Pixel pipeline: better rotation detection (0/90/180/270 + mirrored).
- [x] Pixel pipeline: skew/perspective tuning and softer sampling.
- [x] Tests: golden vectors for format variants + ECI switches.

## Phase 3 — QR advanced features (week+)
- [x] QR: Kanji mode (Shift-JIS) encode/decode.
- [x] QR: Micro QR encode/decode (optional).
- [x] QR: FNC1 / GS1 handling.
- [x] QR: QR “art” safety scoring + warnings.

## Phase 4 — 1D barcode completeness (weeks)
- [x] Encode: Code39, Code93, EAN-8/13, UPC-A, UPC-E, ITF-14.
- [x] Encode: GS1-128 (FNC1 + AI helpers).
- [x] Decode: ITF-14 scanline.
- [x] Decode: 1D scanline decode + checksum validation (EAN/UPC first).
- [x] Render: optional label text beneath bars.

## Phase 5 — 2D non-QR symbologies (weeks)
- [x] DataMatrix: decode C40/Text/X12/EDIFACT + Base256.
- [x] DataMatrix: encode C40/Text/X12/EDIFACT (ECC200).
- [x] PDF417: full encode/decode + ECC tuning.
- [x] PDF417: validate perspective warp sampling on skewed screenshots (test + tuning).
- [ ] Aztec + MaxiCode (advanced, optional).

## Phase 5b — Payload completeness (parallel)
- [ ] Payments: RussiaPaymentOrder payload (GOST R 56042-2014).
- [ ] Payments: additional BezahlCode authorities (single payment/direct debit).
- [ ] Extras: QR "bookmark" title normalization + extended schema validation.

## Phase 6 — Screen reader robustness (hard)
- [x] Multi-QR detection per frame.
- [x] Low-contrast + glare handling (adaptive binarization).
- [x] Anti-alias + gradient background tolerance.
- [x] Speed/accuracy profiles (fast path + robust path).
- [x] Confidence score for pixel decode.
- [x] Optional debug heatmaps for pixel decode.

## Phase 7 — API polish / UX (parallel)
- [x] One-liner “auto encode” (payload type detection + render).
- [x] Unified “CodeGlyph” detect for QR/1D/DataMatrix/PDF417.
- [x] Presets: OTP/Logo/WiFi/Contact with safe defaults.
- [x] Fluent API + static API + options objects (with easy defaults).

## Phase 8 — Performance + AOT polish
- [ ] Reduce allocations in hot decode loops.
- [ ] Span-friendly paths where possible.
- [ ] AOT + trimming hints (no reflection).
- [ ] Optional SIMD for thresholding if worthwhile.
- [x] Image decode: JPEG baseline (SOF0).
- [x] Image decode: JPEG progressive (SOF2) + EXIF orientation.
