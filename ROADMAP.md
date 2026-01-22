# ROADMAP (TODO Only)

This list contains **only work we still want to do** (no already‑done items).

## Now — Performance & Benchmarks (days → weeks)
- QR encode: reduce allocations vs QRCoder (prefer 1‑bit PNG fast paths, pool buffers, avoid duplicate scanline buffers).
- QR decode (clean): close time gap to ZXing without increasing allocations (prune candidates, early exits, skip redundant passes).
- QR decode (noisy): keep robustness while capping candidate explosion and redundant transforms.
- Benchmark reliability: raise minimum work per iteration for quick runs; keep quick/full guidance current; capture hardware + runtime metadata in reports.
- Benchmark automation: make compare runs repeatable and document recommended filters for QR‑focused checks.

## Next — Performance & AOT (weeks)
- Reduce allocations in hot decode loops (buffer pooling, reuse of scratch grids).
- Optional SIMD for thresholding and binarization where it wins on real inputs.
- Evaluate AOT impact on cold start and size; document any AOT‑safe paths.

## DX & Docs (ongoing)
- Publish benchmark summary in README (with link to `BENCHMARK.md` for full tables).
- Keep “quick vs full” benchmark guidance and preflight steps in README.
- Decision guide: “which symbology to pick”.

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
