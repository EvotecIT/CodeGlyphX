# CodeGlyphX roadmap

The roadmap lists unresolved product work only. Completed implementation history belongs in releases and pull requests.

## Current release boundary: 2.0

- [ ] Ship the smaller rendering API and documented migration path.
- [ ] Keep solution, package, symbol, NativeAOT, docs, and website checks green on the release commit.
- [ ] Publish 2.0 and verify the public NuGet package contents before recommending it to consumers.

## Decoder reliability

- [ ] Close remaining real stylized/illustrated QR failures with corpus-backed changes and per-image latency limits.
- [ ] Expand QR screenshot, rotation, mirror, low-contrast, blur, and multi-code fixtures without making synthetic tests the only evidence.
- [ ] Add golden/reference-backed pixels for managed image decoders where current tests only assert dimensions or non-empty output.
- [ ] Keep `net472`/`netstandard2.0` fallback expectations separate from the full modern QR pixel pipeline.

## Codec conformance

- [ ] Either implement VP8 animation interframes or keep them as an explicit hard failure with fixtures.
- [ ] Expand JPEG, GIF, TIFF, BMP, ICO, WebP, and Netpbm corpus coverage around malformed and boundary inputs.
- [ ] Add new formats only with a clear support matrix, bounded decode behavior, and reference-backed tests.

## Industrial and logistics follow-ups

- [ ] Add image recognition for rMQR, MaxiCode, DotCode, Han Xin, DataBar Limited/stacked variants, and GS1 Composite only with real labels, device captures, geometry, and latency budgets.
- [ ] Extend GS1 Composite beyond its GS1-128 carrier to EAN/UPC and DataBar carriers, then add optimized date and AI 90 compression methods where they materially improve capacity.
- [ ] Add native Han Xin GB18030 region compaction and interoperability vectors instead of relying on UTF-8 ECI binary encoding for Chinese text.

## Output and print readiness

- [ ] Add printer-native ZPL, EPL, CPCL, ESC/POS, and PCL output through shared symbol-layout primitives, with device-backed golden fixtures.
- [ ] Build symbol preflight and print-quality analysis around measurable ISO/IEC 15415, ISO/IEC 15416, and ISO/IEC 29158 criteria without presenting heuristics as certified grading.

## Live capture and image formats

- [ ] Add a bounded live-frame pipeline with tracking, duplicate suppression, backpressure, cancellation, and thin platform camera adapters.
- [ ] Add optional HEIC/HEIF, AVIF, JPEG 2000, and JPEG XL codecs behind explicit packages or adapters so the dependency-free core remains pure managed.

## Additional symbologies

- [ ] Add niche or regional formats only when a real consumer, public specification, independent encoder/decoder, and representative fixture corpus establish a maintainable contract.

## Performance

- [ ] Reduce decode-loop allocations only where BenchmarkDotNet and scenario-pack evidence shows a material win.
- [ ] Evaluate SIMD thresholding/binarization on supported modern targets with correctness parity and end-to-end timing.
- [ ] Keep quick benchmark reports for iteration and full reports for release decisions; never mix the two as one baseline.

## Product surfaces

- [ ] Keep the website playground and generated API reference on the same public package/API shape as the README examples.
- [ ] Add integration recipes only when their code is compiled or exercised by an owned example.

## Non-goals

- Reintroducing per-format shortcut methods across every facade or builder.
- Reporting success with placeholder, transparent, repeated, or otherwise fabricated decode output.
- Adding downstream compatibility shims for unpublished or stale packages.
- Claiming universal codec, symbology, trimming, or platform behavior beyond the validated matrix.
