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

This decoding follow-up records 229/267 correct positive images, compared with 229/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

## Data Matrix image-search cost

Data Matrix bounds use each row first dark pixel and only search beyond the current right bound. Components too small to enter the ordered top-eight candidate list skip corner accumulation. Pixel classification, candidate selection, payload validation and scanner allowances are unchanged.

The production bounding-box method matches a full-raster reference in 8,800 cases. Another 8,800 cases reject cancelled tokens, and live and expired ambient deadlines are checked. Ordered component area and corner coordinates match the retained binary in 2,400 cases, including equal-area ties.

One prespecified unrestricted texture negative, falsepositives/19.png, from the unchanged nineteen-image decoding workload. Both binaries receive the same preloaded RGBA32 pixels and 1500 ms scanner allowance. PowerForge runs 3 warmups and 12 rotated measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded.

| Domain | Previous → current median ms | Current / previous | Previous → current managed MiB | Previous → current mean completed / 1 |
|---|---:|---:|---:|---:|
| CCD0 | 1105.948 → 984.880 | 0.891 | 68.86 → 68.95 | 1.000 → 1.000 |
| CCD1 | 856.107 → 814.777 | 0.952 | 66.25 → 66.25 | 1.000 → 1.000 |

The additional paired control uses three unchanged unrestricted negatives: falsepositives-2/01.png, falsepositives-2/09.png and falsepositives/15.png. The same 3 warmups, 12 rotated measurements, preloaded pixels and 1500 ms scanner allowance apply.

| Domain | Previous → current median ms | Current / previous | Previous → current managed MiB | Previous → current mean completed / 3 |
|---|---:|---:|---:|---:|
| CCD0 | 2826.150 → 2665.373 | 0.943 | 229.99 → 227.24 | 2.083 → 2.000 |
| CCD1 | 2375.322 → 2191.310 | 0.923 | 226.88 → 228.16 | 2.833 → 2.667 |

This isolates one texture image and does not establish performance on other images. Wall-clock allowances and shared-host load affect latency and completion. Managed allocation is measured separately; sustained throughput, capacity and portable performance are unproved. All warmups, measurements, unfinished searches and losses remain available. The full nineteen-image comparison is reported separately.

[Spatial-search proof and raw cost references](evidence/datamatrix-spatial-search-proof.json) · [Bounds equivalence](evidence/datamatrix-spatial-bounds-equivalence.json) · [Ordered component checks](evidence/datamatrix-components-current.json).

## Historical PDF417 negative-search completion

These completion and cost measurements belong to source `84e37687c8fc9e46bd34047c72e56a8262db13e4`. They describe the earlier PDF417 change; current corpus, alternating negatives and whole-image costs are measured separately on the current binary.

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
| ccd0 | 266.16 | 260.88 | 0.98x | 21.39 | 21.36 | 1.00x | 9/9 → 9/9 |
| ccd1 | 243.67 | 235.78 | 0.97x | 21.36 | 21.36 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 229/267 correct positive images versus 229/267 previously; gained: none; previously correct positive images lost: none. Data Matrix skips pixel and corner work that cannot change image bounds or the ordered top-eight components. Pixel classification, candidate ordering and selection, payload validation, hints, oracles and recognition allowances are unchanged. Direct bounds and component checks preserve exact output, including stable area ties. Earlier PDF417, QR and Codabar improvement measurements remain explicitly historical. Current corpus and whole-image costs are rerun on the frozen core. Both binaries include MaxiCode recognition. Supplemental PDF417 and MaxiCode images have informed fixtures and are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and wall-clock budgets affect results. Multi-instance and mirrored MaxiCode, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 46.4%, 49.0%, 61.9%. Before the supplementary negative pass: 31.3%, 30.0%, 23.8%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 23/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 22.2% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 23.0% |
| ccd0 | 3 | current | 31/31 | 0 | 0 | 25.1% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 12.6% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 31.8% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 16.9% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 14.3% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 44.1% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 52.4% | 65.7% | 39.2% |
| decoding-ccd0 | 35.1% | 52.5% | 17.6% |
| decoding-ccd1 | 25.5% | 42.3% | 8.8% |
| diagnostic-final-codabar-diagnostic-current | 29.9% | 40.7% | 19.2% |
| diagnostic-final-codabar-diagnostic-previous | 19.7% | 32.6% | 6.9% |
| diagnostic-final-codabar-supplemental-current | 12.9% | 23.9% | 2.1% |
| diagnostic-final-codabar-supplemental-previous | 29.9% | 40.4% | 19.3% |
| diagnostic-final-retail-negative-current | 37.5% | 52.3% | 22.8% |
| diagnostic-final-retail-negative-previous | 20.3% | 34.7% | 5.9% |
| diagnostic-final-retail-positive-current | 39.9% | 53.8% | 26.0% |
| diagnostic-final-retail-positive-previous | 43.1% | 55.1% | 31.2% |
| generation-ccd0 | 47.0% | 61.8% | 32.2% |
| generation-ccd1 | 34.2% | 53.0% | 15.3% |
| holdout-current | 32.9% | 47.6% | 18.2% |
| negative-rotation-ccd0-0-previous | 22.2% | 38.9% | 5.6% |
| negative-rotation-ccd0-1-current | 23.0% | 37.6% | 8.4% |
| negative-rotation-ccd0-2-current | 25.1% | 35.9% | 14.3% |
| negative-rotation-ccd0-3-previous | 12.6% | 24.5% | 0.9% |
| negative-rotation-ccd1-0-current | 31.8% | 50.7% | 13.0% |
| negative-rotation-ccd1-1-previous | 16.9% | 29.2% | 4.7% |
| negative-rotation-ccd1-2-previous | 14.3% | 24.2% | 4.8% |
| negative-rotation-ccd1-3-current | 44.1% | 55.6% | 33.0% |
| negatives-ccd0 | 28.4% | 44.1% | 12.7% |

## Generation on ccd0

Source `7dc89ae6ba182b0868ef6d983f9737f07316cd5c`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 96.44 | 124.46 | 65.23–128.94 | 3832 |
| Numeric | Modules | QRCoder | 127.21 | 177.28 | 107.14–204.81 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.41 | 75.10 | 28.59–75.49 | 1696 |
| Numeric | Modules | ZXing.Net | 65.28 | 100.94 | 40.47–103.91 | 31304 |
| Numeric | Png | CodeGlyphX | 124.86 | 165.02 | 81.57–177.24 | 40312 |
| Numeric | Png | QRCoder | 216.82 | 305.11 | 188.49–311.66 | 6568 |
| Numeric | Png | ZXing-C++ | 894.03 | 1119.35 | 816.44–1148.81 | 848317 |
| Numeric | Png | ZXing.Net | 997.30 | 1163.91 | 839.23–1198.28 | 877601 |
| Unicode | Modules | CodeGlyphX | 2776.26 | 4624.68 | 1897.30–6024.39 | 23872 |
| Unicode | Modules | QRCoder | 3863.40 | 6138.16 | 3078.04–7325.20 | 23216 |
| Unicode | Modules | ZXing-C++ | 1634.89 | 2627.51 | 1281.99–2740.48 | 14648 |
| Unicode | Modules | ZXing.Net | 3071.48 | 3766.15 | 1917.42–3780.82 | 606584 |
| Unicode | Png | CodeGlyphX | 2299.13 | 3390.77 | 1971.25–3668.49 | 274832 |
| Unicode | Png | QRCoder | 3629.51 | 4541.97 | 3187.25–5060.42 | 26167 |
| Unicode | Png | ZXing-C++ | 10242.71 | 11589.98 | 8222.65–12140.51 | 6206820 |
| Unicode | Png | ZXing.Net | 11136.29 | 13987.49 | 9240.77–14850.96 | 6798401 |
| Url | Modules | CodeGlyphX | 886.16 | 989.78 | 483.04–1012.92 | 5057 |
| Url | Modules | QRCoder | 1043.28 | 1165.29 | 941.91–1179.87 | 7409 |
| Url | Modules | ZXing-C++ | 172.57 | 203.75 | 101.54–212.34 | 2711 |
| Url | Modules | ZXing.Net | 860.26 | 952.94 | 208.06–963.88 | 71141 |
| Url | Png | CodeGlyphX | 186.07 | 222.94 | 122.37–235.95 | 60283 |
| Url | Png | QRCoder | 450.22 | 490.42 | 386.89–495.11 | 8200 |
| Url | Png | ZXing-C++ | 1361.50 | 1446.24 | 1231.47–1450.46 | 1309042 |
| Url | Png | ZXing.Net | 1423.25 | 1600.93 | 1342.98–1685.91 | 1377483 |

## Generation on ccd1

Source `7dc89ae6ba182b0868ef6d983f9737f07316cd5c`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 69.04 | 99.06 | 58.95–101.54 | 3832 |
| Numeric | Modules | QRCoder | 97.58 | 135.05 | 74.88–154.19 | 5568 |
| Numeric | Modules | ZXing-C++ | 34.61 | 61.61 | 28.29–71.71 | 1696 |
| Numeric | Modules | ZXing.Net | 49.68 | 106.47 | 37.74–120.41 | 31304 |
| Numeric | Png | CodeGlyphX | 104.13 | 132.07 | 74.77–144.95 | 40312 |
| Numeric | Png | QRCoder | 163.27 | 185.43 | 142.75–187.64 | 6568 |
| Numeric | Png | ZXing-C++ | 753.16 | 808.95 | 683.64–812.10 | 848309 |
| Numeric | Png | ZXing.Net | 777.64 | 866.34 | 703.59–873.76 | 877639 |
| Unicode | Modules | CodeGlyphX | 1594.38 | 1829.05 | 1519.28–1864.97 | 23872 |
| Unicode | Modules | QRCoder | 2582.76 | 2880.04 | 2412.56–2888.75 | 23216 |
| Unicode | Modules | ZXing-C++ | 1068.88 | 1230.76 | 1000.20–1271.23 | 14648 |
| Unicode | Modules | ZXing.Net | 1528.93 | 1785.30 | 1452.23–1830.09 | 606584 |
| Unicode | Png | CodeGlyphX | 2317.07 | 2660.06 | 1908.85–2706.85 | 274942 |
| Unicode | Png | QRCoder | 3451.12 | 3947.64 | 2958.51–4102.07 | 26681 |
| Unicode | Png | ZXing-C++ | 9359.96 | 12176.95 | 6922.45–13024.60 | 6207377 |
| Unicode | Png | ZXing.Net | 11425.72 | 15847.85 | 7346.33–16113.34 | 6799028 |
| Url | Modules | CodeGlyphX | 806.44 | 1034.26 | 605.56–1096.21 | 5057 |
| Url | Modules | QRCoder | 862.26 | 1236.48 | 751.61–1312.16 | 7409 |
| Url | Modules | ZXing-C++ | 122.18 | 199.69 | 101.46–207.58 | 2711 |
| Url | Modules | ZXing.Net | 704.67 | 978.08 | 598.06–978.51 | 71152 |
| Url | Png | CodeGlyphX | 170.61 | 273.92 | 139.74–275.51 | 60294 |
| Url | Png | QRCoder | 409.05 | 516.31 | 378.47–546.51 | 8200 |
| Url | Png | ZXing-C++ | 1210.63 | 1551.17 | 1082.21–1685.33 | 1309101 |
| Url | Png | ZXing.Net | 1315.87 | 1617.82 | 1160.01–1635.17 | 1377569 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1516.16 | 1601.99 | 1424.10–1626.98 | 88.28 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 28.65 | 36.55 | 24.42–41.32 | 2.23 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 194.78 | 219.23 | 179.47–229.67 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1395.93 | 1506.68 | 1364.21–1517.48 | 83.47 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 23.27 | 31.95 | 21.02–35.28 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 172.39 | 201.26 | 156.35–202.84 | 13.05 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 4.3% lower on CCD0, 3.5% lower on CCD1.

Current unfinished image searches occur in 0/24 measured batches, versus 0/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2232.84 | 2365.53 | 2097.09–2385.04 | 82.43 | 19–19 | 0–0 | 0–0 | 0.957 |
| ccd0 | Candidate | 2136.69 | 2351.90 | 2031.53–2364.19 | 82.24 | 19–19 | 0–0 | 0–0 | 0.957 |
| ccd1 | Baseline | 1985.20 | 2154.84 | 1934.37–2167.89 | 83.41 | 19–19 | 0–0 | 0–0 | 0.965 |
| ccd1 | Candidate | 1915.46 | 1975.90 | 1858.60–2007.85 | 83.49 | 19–19 | 0–0 | 0–0 | 0.965 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
