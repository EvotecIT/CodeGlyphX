# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 227/267 | 0 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 6 | 6 | 6 |
| Code128 | 14 | 5 | 13 | 13 | 13 |
| Code39 | 12 | 1 | 9 | 12 | 12 |
| Code93 | 3 | 2 | 3 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 7 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 32 | 34 | 34 |
| UpcA | 36 | 0 | 26 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 227/267 correct positive images, compared with 225/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All 38 PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The selection was fixed before the first supplemental outcomes; its image bytes, oracles and format hints remain unchanged.

This unchanged 38-image set comes from the same upstream collection as the ranking. Five images have informed permanent regression fixtures across the remediation rounds; it is supplemental regression evidence, not an unseen holdout. All four framing-damage recoveries remain correct for both previous and current source in this cost round.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 38/38 | 38/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: none. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

## MaxiCode image recognition

The same six MaxiCode images now decode correctly (6/6), versus 6/6 previously. Image bytes, format hints and payload oracles are unchanged.

The scanner verifies circular finder bands, samples the staggered module grid and uses the existing primary and secondary error correction. It accepts planar rotation and inverted polarity, and can recover small damage to fixed orientation modules. The existing result carries secondary text and bytes, typed carrier metadata and the searched source region.

Secondary text and bytes remain exact library results. Postal, country and service fields remain typed metadata; the comparison adapter presents them losslessly in the corpus carrier string.

Six unmodified independently authored images and original expected payloads, selected before outcomes. They now also inform permanent regression fixtures and are not an unseen holdout.

One symbol per image; multiple MaxiCode instances, symbol geometry, mirrored lattices and arbitrary perspective are unproved. Planar rotation, inverse polarity and label clutter have focused regression coverage. Frozen corpus recognition and cost do not establish general or equal-accuracy superiority.

The [MaxiCode proof](evidence/maxicode-image-proof.json) records source hashes and search bounds. The capability catalog advertises one-symbol image scanning without geometry. Independent fixture tests do not establish coverage of every numeric MaxiCode mode; fixture filenames are not encoding-mode evidence.

## PDF417 scanline reuse

PDF417 stores short scanline runs in 16-bit arrays and retains 32-bit widths for longer lines. More tilted lines fit within the same 8 MiB estimated cache allowance. Search order, acceptance rules and recognition deadlines are unchanged.

All 227,692 cache reads match independently sampled pixels, including reversed, inverted and tilted lines, width boundaries, entry saturation and byte pressure. The checks also cover pre-cancelled and expired calls.

On the existing difficult negative image, the instrumented PDF417 search retains 16,800 lines instead of 8,192. Capacity rejections fall from 21,664 to 0; sampled pixels fall from 15,636,480 to 9,216,000. These diagnostic counts use a longer allowance to complete all work; their timings are unranked.

The paired control batch contains the same two negative images with PDF417-only and all-format hints: four requests, each with the original 1,500 ms allowance. Correctness and completion accompany cost.

Historical diagnostic and paired-control measurements from source `34bfc0ab2216f02a6d84acbed997b3b9f9513f90`. These counts and costs were not rerun for the current Codabar candidate; the current corpus and whole-image costs below are rerun separately.

| Domain | Previous median ms | Current median ms | Current/previous | Previous/current mean managed bytes | Previous/current mean correct requests | Previous/current mean inconclusive requests |
|---|---:|---:|---:|---:|---:|---:|
| CCD0 | 2939.511 | 2709.288 | 0.922 | 177,688,295 / 177,560,335 | 2.667 / 2.750 | 1.333 / 1.250 |
| CCD1 | 2146.916 | 2179.852 | 1.015 | 187,482,001 / 185,419,214 | 3.000 / 3.000 | 1.000 / 1.000 |

Private cache checks compare every run against independently sampled pixels. Instrumented cache counts use two existing negatives, two PDF417-only passes with a 60,000 ms diagnostic allowance to complete all work; their timings are unranked. The actual baseline/current cache source is retained in the archive. Cache byte accounting is an estimate and excludes other decoder working memory; no peak-memory claim is made. The paired controls and whole-image costs preserve the 1,500 ms allowance, preloaded pixels, hints and empty oracles. Shared-host load limits portable latency and completion claims.

[Cache checks, counts and paired costs](evidence/pdf417-cache-cost-proof.json). Whole-image comparison results remain the practical measure of scanner cost and completion.

## Codabar pixel-run recovery

The same six Codabar images decode correctly (6/6), versus 4/6 previously. Two previously failing photographs now pass the hinted decoder, unrestricted decoder and unified scanner.

Codabar candidates use exact thresholded pixel runs before module rounding. Bars and spaces have separately validated narrow and wide groups with a consistent print-gain model. Recovery requires valid A-D framing, consistent gaps and quiet zones; clipped image-edge quiet zones are bounded. Existing module decoding and public APIs remain unchanged.

| Engine | Previous/current main diagnostic positives / 6 | Previous/current completed negatives / 31 | Previous/current supplemental positives / 5 | Current unexpected diagnostic/supplemental images |
|---|---:|---:|---:|---:|
| CodeGlyphX | 4 / 6 | 31 / 31 | 5 / 5 | 0 / 0 |
| ZXing.Net | 6 / 6 | 30 / 30 | 5 / 5 | 1 / 0 |
| ZXing-C++ | 6 / 6 | 31 / 31 | 5 / 5 | 0 / 0 |

Six new image-entrypoint regressions fail on the old core and pass on the candidate. Sixteen focused cases per framework additionally cover print gain, punctuation, orientation, polarity and malformed symbols.

Two main-corpus images informed implementation and permanent regression fixtures. Five additional upstream Codabar images were collected after the fix; all three engines already passed them on the previous binary. The 31 fixed negative inputs use an explicit Codabar hint and are contract diagnostics, not a representative false-positive survey. Main corpus, format hints, payload oracles, scoring and allowances remain unchanged. All-format costs and negative completion are separately rerun; these finite results do not establish overall superiority.

[Codabar proof and provenance](evidence/codabar-run-recovery-proof.json) · [Supplemental input provenance](evidence/codabar-supplemental-provenance.json).

## Historical PDF417 search cost

These retained measurements compare source `02f8cfa12c93c0f01ec889d2e0bd6ad1d9b0d272` with `d10503fa973942b31b1e79e293de71a34eba5858`. They qualify the earlier PDF417 optimization and do not measure the current candidate.

Thirty-one fixed negative images, each with an explicit PDF417 hint. These diagnostics isolate PDF417 search and do not replace the unchanged all-format corpus. PowerForge owns timing, allocation and validation: three warmups and twelve grouped rotated measurements per binary on each verified affinity domain, with no outlier removal. Separate assembly contexts use identical preloaded RGBA32 and options; loading and outcome evaluation are excluded.

The supplemental 38-image PDF417 set remains 38/38 for both previous and current source and includes all four previously recovered framing cases. These inputs inform implementation and are regression evidence, not unseen accuracy evidence. Shared-host contention prevents portable performance claims. An unfinished search can change time and allocation and does not count as a successful rejection. Managed allocation excludes native memory.

| Domain | Before ms | After ms | After / before | Before completed /31 | After completed /31 | Before unfinished batches /12 | After unfinished batches /12 | Before managed MiB | After managed MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 2935.88 | 2793.85 | 0.952 | 31 | 31 | 0 | 0 | 242.66 | 242.65 |
| CCD1 | 2659.96 | 2359.58 | 0.887 | 31 | 30–31 | 0 | 1 | 242.65 | 242.63 |

Completed counts show the range across measured batches; neither binary returns an unexpected payload. An unfinished negative search remains inconclusive.

The actual old and new guard matchers return identical decisions across 401,400 deterministic cases, including 93,382 accepted fits. Existing focused PDF417 and budget checks pass; no public API or production dependency changes.

Raw [CCD0 samples](evidence/pdf417-cost-ccd0-result.json), [CCD1 samples](evidence/pdf417-cost-ccd1-result.json), [unchanged negative inputs](evidence/pdf417-cost-negative-manifest.json), [separate diagnostic outcomes](evidence/pdf417-cost-negative-outcomes.json) and [guard equivalence evidence](evidence/pdf417-cost-guard-equivalence.json) are retained.

## Retail recovery and search cost

Supplementary contract diagnostics, not a representative accuracy or negative-rate survey. Thirty-one fixed negative IDs are each requested with three explicit retail-family hints (93 requests).

The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.

| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 64/87 | 64/87 | 0 |
| ZXing.Net | 55/87 | 55/87 | 2 |
| ZXing-C++ | 54/87 | 54/87 | 1 |

Each of 31 fixed negative images is scanned with EAN, UPC-A and UPC-E hints: 93 requests, rather than 93 independent images.

| Reader | Previous correct rejection | Current correct rejection | Current unexpected output | Current inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 93/93 | 93/93 | 0 | 0 |
| ZXing.Net | 89/93 | 89/93 | 4 | 0 |
| ZXing-C++ | 93/93 | 93/93 | 0 | 0 |

Nine prespecified requests: noise 03, stripes 14 and texture 19, each with EAN, UPC-A and UPC-E hints. PowerForge owns timing: 3 warmups, 12 grouped rotated measurements, no outlier removal, fixed Normal priority/affinity. Baseline and candidate use separate assembly contexts with identical preloaded RGBA32 and options. Loading and validation are excluded. Costs and correctness are measured together; all samples and losses are retained. Shared-host contention prevents portable latency claims.

| Domain | Previous median ms / nine requests | Current median ms / nine requests | Latency ratio | Previous mean managed MiB | Current mean managed MiB | Allocation ratio | Previous/current correct requests |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | 277.32 | 263.01 | 0.95x | 21.36 | 21.36 | 1.00x | 9/9 → 9/9 |
| ccd1 | 258.06 | 240.62 | 0.93x | 21.37 | 21.37 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 227/267 correct positive images versus 225/267 previously; gained: codabar-1/11.png, codabar-1/04.png; previously correct positive images lost: none. Paired negative completion is [28, 30, 30, 28]/31 currently and [29, 29, 29, 29]/31 previously. The current change adds validated Codabar candidates from exact pixel runs before module rounding. Original hints, payload oracles, scoring, existing search bounds and per-image deadlines remain fixed; the added hypotheses can affect work within those budgets. Both binaries include MaxiCode image recognition. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 and MaxiCode images have informed permanent fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 43.1%, 26.2%, 18.6%. Before the supplementary negative pass: 49.9%, 72.4%, 68.3%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 24/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 47.0% |
| ccd0 | 2 | current | 28/31 | 3 | 0 | 29.2% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 49.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 34.6% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 16.4% |
| ccd1 | 2 | previous | 29/31 | 2 | 0 | 14.8% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 88.4% |
| ccd1 | 4 | current | 28/31 | 3 | 0 | 91.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 29.3% | 43.9% | 14.7% |
| decoding-ccd0 | 35.1% | 49.5% | 20.7% |
| decoding-ccd1 | 55.8% | 67.3% | 44.3% |
| diagnostic-final-codabar-diagnostic-current | 48.5% | 60.1% | 37.0% |
| diagnostic-final-codabar-diagnostic-previous | 77.2% | 83.4% | 70.9% |
| diagnostic-final-codabar-supplemental-current | 74.7% | 81.2% | 68.0% |
| diagnostic-final-codabar-supplemental-previous | 42.5% | 56.3% | 28.7% |
| diagnostic-final-retail-negative-current | 75.1% | 82.8% | 67.3% |
| diagnostic-final-retail-negative-previous | 32.1% | 45.7% | 18.4% |
| diagnostic-final-retail-positive-current | 58.8% | 70.9% | 46.6% |
| diagnostic-final-retail-positive-previous | 25.0% | 43.0% | 6.7% |
| generation-ccd0 | 33.8% | 53.1% | 14.6% |
| generation-ccd1 | 34.9% | 50.7% | 19.1% |
| holdout-current | 84.4% | 89.9% | 79.0% |
| negative-rotation-ccd0-0-previous | 47.0% | 64.1% | 29.9% |
| negative-rotation-ccd0-1-current | 29.2% | 46.3% | 12.2% |
| negative-rotation-ccd0-2-current | 49.4% | 60.6% | 38.1% |
| negative-rotation-ccd0-3-previous | 34.6% | 48.1% | 21.0% |
| negative-rotation-ccd1-0-current | 16.4% | 30.3% | 2.5% |
| negative-rotation-ccd1-1-previous | 14.8% | 24.5% | 5.1% |
| negative-rotation-ccd1-2-previous | 88.4% | 90.8% | 86.1% |
| negative-rotation-ccd1-3-current | 91.8% | 94.8% | 88.8% |
| negatives-ccd0 | 63.5% | 74.6% | 52.5% |

## Generation on ccd0

Source `3dfe351da00e598368d21224a01502c90c31be71`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 100.46 | 141.08 | 86.07–154.90 | 3832 |
| Numeric | Modules | QRCoder | 118.73 | 152.27 | 79.52–155.14 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.02 | 75.78 | 28.05–78.89 | 1696 |
| Numeric | Modules | ZXing.Net | 66.56 | 100.20 | 35.61–108.95 | 31304 |
| Numeric | Png | CodeGlyphX | 121.21 | 147.24 | 87.45–149.01 | 40312 |
| Numeric | Png | QRCoder | 209.53 | 259.56 | 139.88–259.69 | 6568 |
| Numeric | Png | ZXing-C++ | 900.18 | 972.48 | 771.74–990.32 | 848286 |
| Numeric | Png | ZXing.Net | 875.15 | 947.77 | 755.64–964.78 | 877628 |
| Unicode | Modules | CodeGlyphX | 1847.76 | 1945.20 | 1692.08–1947.45 | 23872 |
| Unicode | Modules | QRCoder | 2840.89 | 2924.20 | 2696.94–2956.44 | 23216 |
| Unicode | Modules | ZXing-C++ | 1162.50 | 1258.42 | 1104.27–1269.44 | 14648 |
| Unicode | Modules | ZXing.Net | 1761.13 | 1852.43 | 1617.41–1869.53 | 606584 |
| Unicode | Png | CodeGlyphX | 1992.29 | 2152.20 | 1896.45–2171.98 | 274832 |
| Unicode | Png | QRCoder | 3345.76 | 3787.86 | 3094.47–4100.02 | 26112 |
| Unicode | Png | ZXing-C++ | 8179.94 | 9031.46 | 7576.82–9634.57 | 6206701 |
| Unicode | Png | ZXing.Net | 8839.64 | 10360.20 | 8538.26–10789.37 | 6798258 |
| Url | Modules | CodeGlyphX | 934.42 | 1084.29 | 241.32–1101.64 | 5057 |
| Url | Modules | QRCoder | 1112.45 | 1286.79 | 951.27–1296.62 | 7409 |
| Url | Modules | ZXing-C++ | 166.45 | 185.21 | 85.49–188.62 | 2709 |
| Url | Modules | ZXing.Net | 907.03 | 984.22 | 268.99–996.37 | 71141 |
| Url | Png | CodeGlyphX | 243.52 | 456.00 | 161.22–517.52 | 60278 |
| Url | Png | QRCoder | 501.28 | 657.79 | 429.20–669.77 | 8200 |
| Url | Png | ZXing-C++ | 1671.55 | 1849.90 | 1366.66–1863.07 | 1309015 |
| Url | Png | ZXing.Net | 1740.88 | 1972.23 | 1571.16–2015.45 | 1377503 |

## Generation on ccd1

Source `3dfe351da00e598368d21224a01502c90c31be71`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 88.46 | 122.62 | 73.75–129.28 | 3832 |
| Numeric | Modules | QRCoder | 125.96 | 160.33 | 103.58–165.50 | 5568 |
| Numeric | Modules | ZXing-C++ | 42.36 | 72.27 | 34.50–75.87 | 1696 |
| Numeric | Modules | ZXing.Net | 68.94 | 126.38 | 52.39–152.58 | 31304 |
| Numeric | Png | CodeGlyphX | 103.53 | 133.05 | 77.95–148.03 | 40312 |
| Numeric | Png | QRCoder | 169.39 | 290.85 | 146.58–415.19 | 6568 |
| Numeric | Png | ZXing-C++ | 797.06 | 1213.46 | 715.78–1236.10 | 848321 |
| Numeric | Png | ZXing.Net | 851.31 | 1216.01 | 766.41–1494.90 | 877618 |
| Unicode | Modules | CodeGlyphX | 1760.17 | 1967.03 | 1656.32–1977.57 | 23872 |
| Unicode | Modules | QRCoder | 2781.09 | 3129.78 | 2574.93–3206.83 | 23216 |
| Unicode | Modules | ZXing-C++ | 1198.85 | 1358.94 | 1031.68–1374.69 | 14648 |
| Unicode | Modules | ZXing.Net | 1820.26 | 1970.11 | 1564.22–2053.23 | 606584 |
| Unicode | Png | CodeGlyphX | 1960.74 | 2280.79 | 1740.29–2533.46 | 274832 |
| Unicode | Png | QRCoder | 3282.68 | 3679.64 | 2863.83–3822.29 | 26112 |
| Unicode | Png | ZXing-C++ | 7936.07 | 9169.59 | 6903.03–9555.29 | 6206739 |
| Unicode | Png | ZXing.Net | 9944.61 | 10679.31 | 7915.45–10764.60 | 6798227 |
| Url | Modules | CodeGlyphX | 755.39 | 976.59 | 642.00–1060.68 | 5057 |
| Url | Modules | QRCoder | 958.00 | 1201.82 | 805.06–1331.57 | 7409 |
| Url | Modules | ZXing-C++ | 143.51 | 195.98 | 99.33–218.76 | 2707 |
| Url | Modules | ZXing.Net | 711.35 | 885.61 | 620.04–942.57 | 71146 |
| Url | Png | CodeGlyphX | 224.10 | 323.02 | 152.45–327.44 | 60294 |
| Url | Png | QRCoder | 511.17 | 850.43 | 392.28–915.39 | 8200 |
| Url | Png | ZXing-C++ | 1484.35 | 3351.92 | 1337.77–3739.30 | 1309052 |
| Url | Png | ZXing.Net | 1616.34 | 2376.71 | 1359.35–2604.88 | 1377520 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1598.42 | 1830.33 | 1523.98–1832.62 | 81.26 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 26.82 | 31.90 | 19.77–36.76 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 190.75 | 237.61 | 178.37–253.04 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1541.21 | 1725.80 | 1422.40–1737.18 | 82.11 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 20.85 | 27.47 | 18.97–27.82 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 166.89 | 207.97 | 151.89–242.36 | 12.98 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 0.7% lower on CCD0, 4.2% higher on CCD1.

Current unfinished image searches occur in 3/24 measured batches, versus 2/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2474.40 | 2750.04 | 2188.73–2789.42 | 80.60 | 18–19 | 0–0 | 0–1 | 0.993 |
| ccd0 | Candidate | 2457.49 | 2871.80 | 2180.97–2874.73 | 78.56 | 18–19 | 0–0 | 0–1 | 0.993 |
| ccd1 | Baseline | 2165.96 | 2459.93 | 2011.76–2643.49 | 82.15 | 19–19 | 0–0 | 0–0 | 1.042 |
| ccd1 | Candidate | 2255.91 | 2663.90 | 1975.80–2689.33 | 81.42 | 18–19 | 0–0 | 0–1 | 1.042 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
