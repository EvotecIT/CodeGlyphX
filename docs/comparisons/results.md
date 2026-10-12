# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 229/267 | 0 | 3 | 0 | 1 |
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
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 26 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 229/267 correct positive images, compared with 227/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

Historical diagnostic and paired-control measurements from source `34bfc0ab2216f02a6d84acbed997b3b9f9513f90`. These counts and costs were not rerun for the current candidate; the current corpus and whole-image costs below are rerun separately.

| Domain | Previous median ms | Current median ms | Current/previous | Previous/current mean managed bytes | Previous/current mean correct requests | Previous/current mean inconclusive requests |
|---|---:|---:|---:|---:|---:|---:|
| CCD0 | 2939.511 | 2709.288 | 0.922 | 177,688,295 / 177,560,335 | 2.667 / 2.750 | 1.333 / 1.250 |
| CCD1 | 2146.916 | 2179.852 | 1.015 | 187,482,001 / 185,419,214 | 3.000 / 3.000 | 1.000 / 1.000 |

Private cache checks compare every run against independently sampled pixels. Instrumented cache counts use two existing negatives, two PDF417-only passes with a 60,000 ms diagnostic allowance to complete all work; their timings are unranked. The actual baseline/current cache source is retained in the archive. Cache byte accounting is an estimate and excludes other decoder working memory; no peak-memory claim is made. The paired controls and whole-image costs preserve the 1,500 ms allowance, preloaded pixels, hints and empty oracles. Shared-host load limits portable latency and completion claims.

[Cache checks, counts and paired costs](evidence/pdf417-cache-cost-proof.json). Whole-image comparison results remain the practical measure of scanner cost and completion.

## Exact QR raster recovery

The two dense version-40 images decode correctly (2/2), versus 0/2 previously. Both have one pixel per module. The single-image API and unrestricted unified scanner preserve their exact payloads.

QR image decoding recognizes exact two-tone integer raster grids before neighborhood sampling. Finder borders determine bounded version 1-40 candidates; three finder patterns and uniform pixel blocks are required. Existing format, error correction, payload validation, acceptance filtering and inherited deadlines remain authoritative. Single-result and collection paths share the candidate.

Four independent image-entrypoint regression cases fail before the fix. 14 focused cases cover integer and anisotropic spacing, padding, rotations, inversion, cancellation, inherited deadlines, damaged codewords and acceptance filtering. Full modern suites rerun on the measured core.

The exact-raster candidate uses the existing modern pixel decoder (net8/net10); the net472 and netstandard module/fallback boundaries remain unchanged and are separately validated.

Both images informed implementation and permanent regression fixtures; they are not unseen inputs. Each is a dense version-40 raster at one pixel per module. The candidate requires exactly two grayscale levels, integer spacing, three recognizable finders and uniform pixel blocks. Ordinary photographs retain the existing fallback and bounded version search. Current main corpus, negative rotation and whole-image costs are rerun with unchanged hints, payload oracles, scoring and deadlines. This does not establish general photograph recovery or overall superiority.

[QR raster proof and provenance](evidence/qr-exact-grid-recovery-proof.json).

## Historical Codabar pixel-run recovery

The Codabar discovery below belongs to source `3dfe351da00e598368d21224a01502c90c31be71`. Its gain and diagnostic table describe that earlier change. Current corpus results and costs are measured separately on the current core.

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
| ccd0 | 258.95 | 247.64 | 0.96x | 21.37 | 21.36 | 1.00x | 9/9 → 9/9 |
| ccd1 | 244.30 | 232.78 | 0.95x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 229/267 correct positive images versus 227/267 previously; gained: qrcode-5/18.png, qrcode-5/19.png; previously correct positive images lost: none. Paired negative completion is [30, 29, 30, 29]/31 currently and [29, 29, 29, 30]/31 previously. The current change adds exact two-tone QR raster candidates with integer module spacing before neighborhood sampling, shared by single-result and collection decoding. Original hints, payload oracles, scoring, existing search bounds and per-image deadlines remain fixed; the added hypotheses can affect work within those budgets. Both binaries include MaxiCode image recognition. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 and MaxiCode images have informed permanent fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 13.3%, 14.2%, 15.9%. Before the supplementary negative pass: 32.4%, 26.6%, 44.6%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 18.0% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 19.5% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 17.5% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 53.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 18.9% |
| ccd1 | 2 | previous | 29/31 | 2 | 0 | 9.7% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 25.2% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 19.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 14.5% | 27.5% | 1.5% |
| decoding-ccd0 | 12.8% | 23.2% | 2.7% |
| decoding-ccd1 | 70.0% | 78.5% | 61.6% |
| diagnostic-final-codabar-diagnostic-current | 37.3% | 51.5% | 23.1% |
| diagnostic-final-codabar-diagnostic-previous | 31.0% | 47.5% | 14.4% |
| diagnostic-final-codabar-supplemental-current | 45.0% | 59.2% | 30.8% |
| diagnostic-final-codabar-supplemental-previous | 51.6% | 63.0% | 40.4% |
| diagnostic-final-retail-negative-current | 15.6% | 29.4% | 1.9% |
| diagnostic-final-retail-negative-previous | 11.9% | 20.7% | 3.4% |
| diagnostic-final-retail-positive-current | 21.0% | 37.3% | 4.8% |
| diagnostic-final-retail-positive-previous | 31.8% | 42.7% | 20.8% |
| generation-ccd0 | 13.5% | 25.1% | 2.3% |
| generation-ccd1 | 14.8% | 24.2% | 5.5% |
| holdout-current | 14.0% | 25.2% | 3.3% |
| negative-rotation-ccd0-0-previous | 18.0% | 31.1% | 5.0% |
| negative-rotation-ccd0-1-current | 19.5% | 35.5% | 3.4% |
| negative-rotation-ccd0-2-current | 17.5% | 31.9% | 3.2% |
| negative-rotation-ccd0-3-previous | 53.4% | 63.9% | 42.8% |
| negative-rotation-ccd1-0-current | 18.9% | 29.3% | 8.8% |
| negative-rotation-ccd1-1-previous | 9.7% | 16.8% | 3.0% |
| negative-rotation-ccd1-2-previous | 25.2% | 38.0% | 12.2% |
| negative-rotation-ccd1-3-current | 19.7% | 34.1% | 5.4% |
| negatives-ccd0 | 34.5% | 48.5% | 20.5% |

## Generation on ccd0

Source `7bed5e6b90d79081058661142ee062803bd5e581`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 93.99 | 168.04 | 74.65–219.90 | 3832 |
| Numeric | Modules | QRCoder | 157.82 | 255.05 | 75.59–258.99 | 5568 |
| Numeric | Modules | ZXing-C++ | 43.27 | 73.00 | 28.26–79.38 | 1696 |
| Numeric | Modules | ZXing.Net | 67.95 | 103.00 | 48.27–108.13 | 31304 |
| Numeric | Png | CodeGlyphX | 115.63 | 153.91 | 98.89–180.73 | 40312 |
| Numeric | Png | QRCoder | 185.11 | 253.47 | 159.64–281.94 | 6568 |
| Numeric | Png | ZXing-C++ | 881.35 | 1005.52 | 753.12–1026.26 | 848311 |
| Numeric | Png | ZXing.Net | 875.13 | 1034.53 | 790.51–1115.45 | 877619 |
| Unicode | Modules | CodeGlyphX | 1783.02 | 1858.59 | 1719.34–1892.32 | 23872 |
| Unicode | Modules | QRCoder | 2750.08 | 2940.87 | 2713.74–2980.89 | 23216 |
| Unicode | Modules | ZXing-C++ | 1153.94 | 1264.91 | 1073.70–1300.62 | 14648 |
| Unicode | Modules | ZXing.Net | 1776.69 | 1887.74 | 1713.85–1892.86 | 606584 |
| Unicode | Png | CodeGlyphX | 1930.57 | 2375.55 | 1883.81–2437.95 | 274832 |
| Unicode | Png | QRCoder | 3169.68 | 3738.58 | 3099.19–3939.00 | 26112 |
| Unicode | Png | ZXing-C++ | 7908.88 | 8501.19 | 7568.31–8537.50 | 6206809 |
| Unicode | Png | ZXing.Net | 8686.66 | 9664.92 | 8368.34–9846.56 | 6798366 |
| Url | Modules | CodeGlyphX | 816.40 | 1005.08 | 176.43–1051.92 | 5057 |
| Url | Modules | QRCoder | 990.92 | 1156.52 | 929.61–1172.92 | 7415 |
| Url | Modules | ZXing-C++ | 147.45 | 186.84 | 61.65–196.46 | 2709 |
| Url | Modules | ZXing.Net | 816.25 | 895.14 | 207.54–902.65 | 71141 |
| Url | Png | CodeGlyphX | 199.81 | 294.36 | 153.65–328.95 | 60294 |
| Url | Png | QRCoder | 481.30 | 569.75 | 435.48–578.99 | 8200 |
| Url | Png | ZXing-C++ | 1502.85 | 1713.66 | 1367.32–1774.48 | 1309067 |
| Url | Png | ZXing.Net | 1608.45 | 1757.10 | 1402.60–1806.35 | 1377526 |

## Generation on ccd1

Source `7bed5e6b90d79081058661142ee062803bd5e581`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 77.84 | 99.05 | 63.36–102.04 | 3832 |
| Numeric | Modules | QRCoder | 105.33 | 143.15 | 82.36–143.77 | 5568 |
| Numeric | Modules | ZXing-C++ | 38.15 | 75.91 | 28.71–94.45 | 1696 |
| Numeric | Modules | ZXing.Net | 58.16 | 83.48 | 39.34–88.58 | 31304 |
| Numeric | Png | CodeGlyphX | 97.79 | 128.13 | 77.45–135.86 | 40312 |
| Numeric | Png | QRCoder | 180.03 | 204.88 | 155.08–209.01 | 6568 |
| Numeric | Png | ZXing-C++ | 753.14 | 809.64 | 730.91–821.03 | 848303 |
| Numeric | Png | ZXing.Net | 770.24 | 828.98 | 713.85–848.75 | 877632 |
| Unicode | Modules | CodeGlyphX | 1607.70 | 1967.19 | 1494.43–2258.39 | 23872 |
| Unicode | Modules | QRCoder | 2542.33 | 3018.49 | 2459.46–3160.85 | 23216 |
| Unicode | Modules | ZXing-C++ | 1089.66 | 1186.60 | 1005.01–1200.65 | 14648 |
| Unicode | Modules | ZXing.Net | 1633.93 | 2006.11 | 1538.22–2241.12 | 606584 |
| Unicode | Png | CodeGlyphX | 1819.46 | 1932.52 | 1641.12–1944.43 | 274832 |
| Unicode | Png | QRCoder | 3021.47 | 3084.09 | 2833.66–3106.80 | 26112 |
| Unicode | Png | ZXing-C++ | 7212.74 | 7681.53 | 6640.83–7746.90 | 6207111 |
| Unicode | Png | ZXing.Net | 7631.13 | 8393.09 | 7353.18–8484.60 | 6798762 |
| Url | Modules | CodeGlyphX | 762.49 | 1072.27 | 630.81–1250.64 | 5057 |
| Url | Modules | QRCoder | 980.02 | 1078.76 | 835.85–1123.76 | 7409 |
| Url | Modules | ZXing-C++ | 124.38 | 211.94 | 93.56–254.90 | 2713 |
| Url | Modules | ZXing.Net | 832.05 | 901.79 | 579.06–917.80 | 71146 |
| Url | Png | CodeGlyphX | 169.98 | 277.73 | 128.94–284.86 | 60282 |
| Url | Png | QRCoder | 431.12 | 493.83 | 379.73–520.08 | 8200 |
| Url | Png | ZXing-C++ | 1389.07 | 1742.46 | 1191.24–1764.95 | 1309068 |
| Url | Png | ZXing.Net | 1515.54 | 1772.98 | 1206.43–1843.23 | 1377501 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1573.02 | 1669.14 | 1488.99–1681.49 | 81.54 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 25.81 | 31.26 | 22.53–32.94 | 2.23 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 190.00 | 222.43 | 176.99–242.26 | 13.27 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1534.17 | 1619.05 | 1433.59–1649.88 | 82.70 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 22.21 | 25.41 | 20.15–27.26 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 165.99 | 264.53 | 153.28–272.60 | 12.97 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 1.3% lower on CCD0, less than 0.1% lower on CCD1.

Current unfinished image searches occur in 0/24 measured batches, versus 0/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2150.72 | 2320.64 | 2069.41–2338.60 | 82.08 | 19–19 | 0–0 | 0–0 | 0.987 |
| ccd0 | Candidate | 2123.58 | 2330.57 | 2081.81–2342.96 | 82.12 | 19–19 | 0–0 | 0–0 | 0.987 |
| ccd1 | Baseline | 2009.61 | 2085.15 | 1943.18–2085.63 | 82.79 | 19–19 | 0–0 | 0–0 | 1.000 |
| ccd1 | Candidate | 2009.13 | 2072.76 | 1908.46–2073.89 | 82.84 | 19–19 | 0–0 | 0–0 | 1.000 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
