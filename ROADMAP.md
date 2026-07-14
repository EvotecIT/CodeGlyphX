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

## Performance

- [ ] Reduce decode-loop allocations only where BenchmarkDotNet and scenario-pack evidence shows a material win.
- [ ] Evaluate SIMD thresholding/binarization on supported modern targets with correctness parity and end-to-end timing.
- [ ] Keep quick benchmark reports for iteration and full reports for release decisions; never mix the two as one baseline.

## Product surfaces

- [ ] Keep the website playground and generated API reference on the same public package/API shape as the README examples.
- [ ] Add integration recipes only when their code is compiled or exercised by an owned example.
- [ ] Add symbologies such as MaxiCode, rMQR, DotCode, or Han Xin only with conformance vectors and decode/encode scope stated up front.

## Non-goals

- Reintroducing per-format shortcut methods across every facade or builder.
- Reporting success with placeholder, transparent, repeated, or otherwise fabricated decode output.
- Adding downstream compatibility shims for unpublished or stale packages.
- Claiming universal codec, symbology, trimming, or platform behavior beyond the validated matrix.
