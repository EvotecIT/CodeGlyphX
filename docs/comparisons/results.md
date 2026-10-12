# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 229/267 | 0 | 3 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
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

This decoding follow-up records 229/267 correct positive images, compared with 229/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png

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

Both previous and current binaries decode the same six MaxiCode images correctly (6/6). Image bytes, format hints and payload oracles are unchanged.

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

## PDF417 negative-search completion

PDF417 reuses eight-run codeword fits within each thresholded image and skips offset crops whose required prefix cannot decode. It receives two weighted shares of the existing 1500 ms total scanner allowance. Candidate order, fit rules, payload validation and inherited deadlines are unchanged.

The cached and uncached fitters agree on 140,332 decisions, including failed fits, wide runs and cache capacity pressure. Four module-entrypoint controls preserve offset symbols and macro metadata in both row orders.

Across four alternating passes per domain on the same 31 negative images, completed scans are CCD0: previous [28, 29], current [31, 30]; CCD1: previous [29, 29], current [30, 28]. All individual outcomes and unexpected payloads are retained in the rotation packets.

Three prespecified unrestricted negative images: falsepositives-2/01.png, falsepositives-2/09.png and falsepositives/15.png. PowerForge runs 3 warmups and 12 rotated measurements per engine on each verified affinity domain. Both binaries receive identical preloaded RGBA32 images and options; loading and outcome evaluation are excluded.

| Domain | Previous median ms | Current median ms | Current / previous | Previous managed bytes | Current managed bytes | Previous / current mean completed images |
|---|---:|---:|---:|---:|---:|---:|
| CCD0 | 2637.505 | 2767.981 | 1.049 | 224,612,799 | 233,173,571 | 1.000 / 2.417 |
| CCD1 | 2603.640 | 3061.201 | 1.176 | 224,265,915 | 227,675,593 | 0.833 / 1.583 |

Completion and latency depend on the unchanged wall-clock budgets and shared-host load. Managed allocation is measured separately; CPU cost, sustained throughput and portable performance are unproved. Every warmup, measured sample, failure and unfinished search is retained. A lower time with an unfinished search does not establish a speed win.

[Completion proof](evidence/pdf417-negative-completion-proof.json) · [Fitter equivalence](evidence/pdf417-codeword-cache-equivalence.json).

## Historical exact QR raster recovery

The discovery and focused checks below belong to source `7bed5e6b90d79081058661142ee062803bd5e581`. The current corpus recheck preserves 2/2 correct payloads on the current core.

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
| ccd0 | 245.93 | 240.39 | 0.98x | 21.36 | 21.36 | 1.00x | 9/9 → 9/9 |
| ccd1 | 263.49 | 254.32 | 0.97x | 21.35 | 21.34 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 229/267 correct positive images versus 229/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [31, 30, 30, 28]/31 currently and [28, 29, 29, 29]/31 previously. The current change reuses PDF417 eight-run fit decisions and skips impossible module-prefix crops while giving PDF417 two weighted shares of the existing total scanner deadline. Coarse header evidence, candidate order, fit rules and payload validation remain unchanged. Earlier QR and Codabar discovery results are explicitly historical and their payloads are rechecked on the current core. Original hints, payload oracles, scoring, existing search bounds and per-image deadlines remain fixed; the added hypotheses can affect work within those budgets. Both binaries include MaxiCode image recognition. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 and MaxiCode images have informed permanent fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 44.1%, 44.6%, 50.0%. Before the supplementary negative pass: 25.3%, 30.9%, 37.0%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 23/25 | 0 | 2 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 23/25 supplementary negatives versus 24/25. Previously completed images now unfinished: falsepositives-2/14.png, falsepositives-2/21.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 22.6% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 26.6% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 34.7% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 36.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 61.7% |
| ccd1 | 2 | previous | 29/31 | 2 | 0 | 27.1% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 36.9% |
| ccd1 | 4 | current | 28/31 | 3 | 0 | 100.0% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 46.2% | 60.2% | 32.3% |
| decoding-ccd0 | 37.8% | 49.1% | 26.5% |
| decoding-ccd1 | 55.2% | 65.0% | 45.5% |
| diagnostic-final-codabar-diagnostic-current | 54.5% | 67.7% | 41.4% |
| diagnostic-final-codabar-diagnostic-previous | 93.5% | 97.0% | 90.1% |
| diagnostic-final-codabar-supplemental-current | 36.3% | 51.1% | 21.5% |
| diagnostic-final-codabar-supplemental-previous | 53.8% | 72.7% | 34.9% |
| diagnostic-final-retail-negative-current | 22.7% | 38.9% | 6.7% |
| diagnostic-final-retail-negative-previous | 32.6% | 46.7% | 18.5% |
| diagnostic-final-retail-positive-current | 26.6% | 39.6% | 13.7% |
| diagnostic-final-retail-positive-previous | 41.1% | 53.0% | 28.9% |
| generation-ccd0 | 52.4% | 69.3% | 35.5% |
| generation-ccd1 | 53.5% | 68.7% | 38.3% |
| holdout-current | 39.3% | 51.7% | 26.9% |
| negative-rotation-ccd0-0-previous | 22.6% | 40.0% | 5.2% |
| negative-rotation-ccd0-1-current | 26.6% | 45.7% | 7.6% |
| negative-rotation-ccd0-2-current | 34.7% | 49.0% | 20.3% |
| negative-rotation-ccd0-3-previous | 36.4% | 51.8% | 21.0% |
| negative-rotation-ccd1-0-current | 61.7% | 75.5% | 47.9% |
| negative-rotation-ccd1-1-previous | 27.1% | 46.0% | 8.3% |
| negative-rotation-ccd1-2-previous | 36.9% | 56.4% | 17.5% |
| negative-rotation-ccd1-3-current | 100.0% | 100.0% | 100.0% |
| negatives-ccd0 | 31.1% | 53.0% | 9.1% |

## Generation on ccd0

Source `84e37687c8fc9e46bd34047c72e56a8262db13e4`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 100.50 | 132.94 | 89.09–136.35 | 3832 |
| Numeric | Modules | QRCoder | 128.21 | 173.95 | 82.31–184.18 | 5568 |
| Numeric | Modules | ZXing-C++ | 46.56 | 82.18 | 38.43–85.77 | 1696 |
| Numeric | Modules | ZXing.Net | 64.55 | 93.82 | 36.99–94.66 | 31304 |
| Numeric | Png | CodeGlyphX | 124.90 | 141.98 | 107.71–146.34 | 40312 |
| Numeric | Png | QRCoder | 232.25 | 259.54 | 175.18–263.37 | 6568 |
| Numeric | Png | ZXing-C++ | 956.95 | 1027.06 | 890.06–1040.68 | 848285 |
| Numeric | Png | ZXing.Net | 975.38 | 1051.09 | 819.49–1079.79 | 877603 |
| Unicode | Modules | CodeGlyphX | 2105.44 | 2459.09 | 1911.53–2502.91 | 23872 |
| Unicode | Modules | QRCoder | 3262.75 | 3756.60 | 2907.50–3959.14 | 23216 |
| Unicode | Modules | ZXing-C++ | 1311.83 | 1739.91 | 1146.05–2027.76 | 14648 |
| Unicode | Modules | ZXing.Net | 2073.39 | 2438.12 | 1794.87–2492.09 | 606584 |
| Unicode | Png | CodeGlyphX | 2182.80 | 3145.71 | 1976.39–3715.22 | 274832 |
| Unicode | Png | QRCoder | 3490.87 | 4532.55 | 3315.80–4544.94 | 26112 |
| Unicode | Png | ZXing-C++ | 9220.26 | 10771.68 | 8418.33–11178.83 | 6206743 |
| Unicode | Png | ZXing.Net | 10739.32 | 18280.58 | 9336.40–19674.94 | 6798190 |
| Url | Modules | CodeGlyphX | 944.64 | 1373.39 | 138.17–1490.61 | 5057 |
| Url | Modules | QRCoder | 1219.28 | 1432.38 | 1054.21–1493.29 | 7409 |
| Url | Modules | ZXing-C++ | 182.29 | 317.28 | 89.30–317.94 | 2709 |
| Url | Modules | ZXing.Net | 914.53 | 1321.95 | 237.38–1574.98 | 71136 |
| Url | Png | CodeGlyphX | 279.53 | 1058.23 | 185.68–1058.66 | 60278 |
| Url | Png | QRCoder | 566.82 | 896.19 | 504.39–1020.53 | 8200 |
| Url | Png | ZXing-C++ | 1788.81 | 3589.44 | 1481.70–4091.88 | 1309015 |
| Url | Png | ZXing.Net | 1946.48 | 4987.35 | 1637.32–5005.96 | 1377482 |

## Generation on ccd1

Source `84e37687c8fc9e46bd34047c72e56a8262db13e4`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 102.27 | 168.02 | 72.76–211.80 | 3832 |
| Numeric | Modules | QRCoder | 123.42 | 272.09 | 83.81–294.44 | 5568 |
| Numeric | Modules | ZXing-C++ | 42.83 | 92.04 | 29.80–92.25 | 1696 |
| Numeric | Modules | ZXing.Net | 81.79 | 136.56 | 46.94–142.02 | 31304 |
| Numeric | Png | CodeGlyphX | 126.66 | 149.47 | 86.83–154.45 | 40312 |
| Numeric | Png | QRCoder | 224.48 | 374.12 | 166.98–378.92 | 6568 |
| Numeric | Png | ZXing-C++ | 1043.86 | 1222.73 | 878.68–1242.50 | 848317 |
| Numeric | Png | ZXing.Net | 1110.37 | 1295.76 | 881.61–1349.46 | 877593 |
| Unicode | Modules | CodeGlyphX | 1595.73 | 1784.64 | 1542.07–1912.83 | 23872 |
| Unicode | Modules | QRCoder | 2500.32 | 2720.64 | 2340.62–2890.31 | 23216 |
| Unicode | Modules | ZXing-C++ | 1075.18 | 1277.21 | 1002.10–1326.62 | 14648 |
| Unicode | Modules | ZXing.Net | 1608.32 | 1727.54 | 1565.84–1736.76 | 606585 |
| Unicode | Png | CodeGlyphX | 1826.72 | 2131.19 | 1682.32–2134.40 | 274832 |
| Unicode | Png | QRCoder | 2987.08 | 3560.44 | 2761.82–3600.84 | 26112 |
| Unicode | Png | ZXing-C++ | 7549.51 | 8740.46 | 6740.40–8916.44 | 6206853 |
| Unicode | Png | ZXing.Net | 8041.91 | 9788.51 | 7194.19–10212.93 | 6798575 |
| Url | Modules | CodeGlyphX | 1059.67 | 1501.11 | 127.60–1529.42 | 5057 |
| Url | Modules | QRCoder | 1211.69 | 1853.89 | 384.61–2035.49 | 7402 |
| Url | Modules | ZXing-C++ | 158.80 | 285.58 | 66.54–296.58 | 2707 |
| Url | Modules | ZXing.Net | 970.51 | 1366.24 | 214.48–1465.76 | 71125 |
| Url | Png | CodeGlyphX | 195.05 | 291.85 | 147.46–300.08 | 60278 |
| Url | Png | QRCoder | 447.97 | 549.10 | 417.04–567.71 | 8200 |
| Url | Png | ZXing-C++ | 1533.75 | 1593.31 | 1353.85–1595.35 | 1309044 |
| Url | Png | ZXing.Net | 1557.78 | 1734.05 | 1389.26–1763.44 | 1377493 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1602.76 | 1807.26 | 1520.56–1836.61 | 82.06 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 26.48 | 30.74 | 23.24–33.70 | 2.21 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 191.64 | 218.98 | 177.84–226.76 | 12.97 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1540.64 | 1582.21 | 1437.60–1601.87 | 82.95 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 22.76 | 33.47 | 19.75–38.48 | 2.20 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 168.11 | 203.92 | 156.99–233.25 | 12.96 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 1.2% lower on CCD0, 3.1% lower on CCD1.

Current unfinished image searches occur in 0/24 measured batches, versus 1/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2184.04 | 2367.54 | 2094.56–2378.94 | 82.31 | 19–19 | 0–0 | 0–0 | 0.988 |
| ccd0 | Candidate | 2158.11 | 2330.85 | 2036.55–2371.00 | 82.48 | 19–19 | 0–0 | 0–0 | 0.988 |
| ccd1 | Baseline | 2250.09 | 2511.89 | 2068.49–2533.66 | 81.73 | 18–19 | 0–0 | 0–1 | 0.969 |
| ccd1 | Candidate | 2179.81 | 2326.09 | 2096.38–2331.65 | 82.55 | 19–19 | 0–0 | 0–0 | 0.969 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
