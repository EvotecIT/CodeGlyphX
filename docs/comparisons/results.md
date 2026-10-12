# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 234/267 | 0 | 3 | 0 | 4 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 8 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 6 | 6 | 6 |
| Code128 | 14 | 5 | 13 | 13 | 13 |
| Code39 | 12 | 1 | 12 | 12 | 12 |
| Code93 | 3 | 2 | 3 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 7 | 8 | 8 |
| Itf | 12 | 10 | 12 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 26 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 234/267 correct positive images, compared with 232/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives-2/15.png

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

## ITF complete-symbol recovery

ITF locates complete start/data/stop runs in either scanline direction and fits narrow and wide bars separately from spaces. Both recovered independent images pass through pixel and image entrypoints. Interior quiet zones, digit structure, ITF-14 checksums and clipped-region checks remain enforced. Codabar shares the unchanged width-fitting calculation.

Twelve unchanged ITF positives from the main corpus and the same thirty-one negative image IDs with an explicit ITF hint. These supplementary checks do not establish a general recognition or false-positive rate.

The actual previous and current Codabar validators agree on 20,000 decisions and exact fit outputs across printing bias, rounding, framing, direction and cancellation. Two independent ITF images and direction, polarity, framing and ITF-14 checksum regressions pass through public entrypoints.

| Reader | Previous → current positives / 12 | Previous → current completed negative rejections / 31 | Current unexpected negative images |
|---|---:|---:|---:|
| CodeGlyphX | 10 → 12 | 31 → 31 | 0 |
| ZXing.Net | 12 → 12 | 31 → 31 | 0 |
| ZXing-C++ | 12 → 12 | 31 → 31 | 0 |

Paired cost for the same twelve ITF images. Both binaries use preloaded RGBA32, identical hints and 1500 ms allowances; 3 warmups and 12 rotated samples per engine and domain. Input loading and outcome evaluation are excluded.

| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 12 |
|---|---:|---:|---:|---:|
| CCD0 | 141.421 → 147.783 | 1.045 | 8.50 → 8.74 | 10.000 → 12.000 |
| CCD1 | 192.053 → 186.941 | 0.973 | 8.51 → 8.76 | 10.000 → 12.000 |

Both binaries receive identical hints, pixels, validation and allowances. The earlier binary reads fewer positives, so latency ratios include different completed recognition work. Managed allocation excludes native costs; shared-host measurements do not establish portable speed, throughput or capacity. Every measured sample, warmup failure, completion and unexpected payload is retained; successful warmup samples are not retained.

[ITF proof, outcomes and every timing sample](evidence/itf-run-recovery-proof.json) · [Codabar width-model compatibility](evidence/itf-codabar-model-equivalence.json).

## Historical Code 39 clipped and reversed scanlines

These diagnostics and paired costs belong to source `417f25c101aada2148f1d9f7ebdc8a8f505f7a29`. They describe the earlier Code 39 change; current corpus and whole-image measurements are separate.

Code 39 locates complete guarded symbols in either scanline direction and accepts quiet runs clipped only at the physical image boundary. Interior quiet-zone, character-width and payload checks remain. Fixed nine-run normalization avoids temporary LINQ arrays. RequireValid now checks short payloads and validates and strips the checksum for the encoder-supported empty payload; optional checksum behavior remains unchanged.

Twelve unchanged Code 39 positives from the main corpus and the same thirty-one negative image IDs with an explicit Code 39 hint. These are supplementary contract checks; they do not establish a general recognition or false-positive rate.

The actual previous and current width normalizers agree on 205,040 decisions, including reversed run lists. Three independent images and direction, polarity, framing and short-checksum regressions pass through public entrypoints.

| Reader | Previous → current positives / 12 | Previous → current completed negative rejections / 31 | Current unexpected negative images |
|---|---:|---:|---:|
| CodeGlyphX | 9 → 12 | 31 → 31 | 0 |
| ZXing.Net | 12 → 12 | 30 → 30 | 1 |
| ZXing-C++ | 12 → 12 | 31 → 31 | 0 |

Paired cost for the same twelve Code 39 images. Both binaries use preloaded RGBA32, identical hints and 1500 ms allowances; 3 warmups and 12 rotated samples per engine and domain. Input loading and outcome evaluation are excluded.

| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 12 |
|---|---:|---:|---:|---:|
| CCD0 | 137.538 → 120.054 | 0.873 | 22.24 → 17.20 | 9.000 → 12.000 |
| CCD1 | 123.009 → 109.052 | 0.887 | 22.26 → 17.23 | 9.000 → 12.000 |

Both binaries receive identical hints, pixels, validation and allowances. The earlier binary reads fewer positive images, so latency ratios include different completed recognition work. Managed allocation excludes native costs; shared-host measurements do not establish portable speed, throughput or capacity. Every measured sample, warmup failure, completion and unexpected payload is retained; successful warmup samples are not retained.

[Code 39 proof, outcomes and every timing sample](evidence/code39-run-recovery-proof.json) · [Normalizer equivalence](evidence/code39-normalizer-equivalence.json).

## Historical Data Matrix image-search cost

These spatial-search checks and costs belong to source `7dc89ae6ba182b0868ef6d983f9737f07316cd5c`. They describe the earlier Data Matrix change. Current corpus and whole-image costs are measured separately on the current binary.

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

Completion and latency depend on the unchanged wall-clock budgets and shared-host load. Managed allocation is measured separately; CPU cost, sustained throughput and portable performance are unproved. Every measured sample, warmup failure and unfinished search is retained; successful warmup samples are not retained. A lower time with an unfinished search does not establish a speed win.

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
| ccd0 | 324.24 | 312.49 | 0.96x | 21.38 | 21.37 | 1.00x | 9/9 → 9/9 |
| ccd1 | 247.93 | 233.89 | 0.94x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 234/267 correct positive images versus 232/267 previously; gained: itf-2/14.png, itf-2/13.png; previously correct positive images lost: none. ITF recovery preserves complete framing, digit validation, interior quiet zones, ITF-14 checksum checks and clipped-region protection. Hints, oracles, format priorities, presentation and recognition allowances are unchanged. Earlier Code 39, Data Matrix, PDF417, QR and Codabar improvement measurements remain historical. Current corpus and whole-image costs are rerun on the frozen core. Both binaries include MaxiCode recognition. Supplemental PDF417 and MaxiCode images have informed fixtures and are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and wall-clock budgets affect results. Multi-instance and mirrored MaxiCode, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 21.0%, 30.6%, 23.9%. Before the supplementary negative pass: 92.9%, 55.5%, 66.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 25/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 87.7% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 45.7% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 54.1% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 33.6% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 43.6% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 73.1% |
| ccd1 | 3 | previous | 27/31 | 4 | 0 | 56.6% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 32.6% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 25.2% | 40.0% | 10.4% |
| decoding-ccd0 | 55.1% | 68.7% | 41.4% |
| decoding-ccd1 | 43.7% | 59.3% | 28.1% |
| diagnostic-final-codabar-diagnostic-current | 92.9% | 95.6% | 90.2% |
| diagnostic-final-codabar-diagnostic-previous | 73.6% | 79.9% | 67.3% |
| diagnostic-final-codabar-supplemental-current | 68.0% | 78.9% | 57.2% |
| diagnostic-final-codabar-supplemental-previous | 90.6% | 94.7% | 86.3% |
| diagnostic-final-code39-negative-current | 21.8% | 35.4% | 8.2% |
| diagnostic-final-code39-negative-previous | 88.0% | 91.3% | 84.8% |
| diagnostic-final-code39-positive-current | 20.2% | 33.0% | 7.5% |
| diagnostic-final-code39-positive-previous | 62.4% | 74.2% | 50.7% |
| diagnostic-final-itf-negative-current | 20.3% | 36.4% | 4.3% |
| diagnostic-final-itf-negative-previous | 74.6% | 85.6% | 63.6% |
| diagnostic-final-itf-positive-current | 34.4% | 52.4% | 16.4% |
| diagnostic-final-itf-positive-previous | 40.7% | 54.7% | 26.7% |
| diagnostic-final-retail-negative-current | 45.1% | 59.5% | 30.8% |
| diagnostic-final-retail-negative-previous | 65.1% | 78.6% | 51.7% |
| diagnostic-final-retail-positive-current | 34.2% | 51.5% | 16.8% |
| diagnostic-final-retail-positive-previous | 95.7% | 97.8% | 93.6% |
| generation-ccd0 | 77.9% | 84.8% | 70.9% |
| generation-ccd1 | 36.4% | 54.3% | 18.5% |
| holdout-current | 41.8% | 57.6% | 25.9% |
| negative-rotation-ccd0-0-previous | 87.7% | 93.0% | 82.4% |
| negative-rotation-ccd0-1-current | 45.7% | 66.5% | 24.9% |
| negative-rotation-ccd0-2-current | 54.1% | 68.0% | 40.2% |
| negative-rotation-ccd0-3-previous | 33.6% | 55.1% | 12.1% |
| negative-rotation-ccd1-0-current | 43.6% | 59.2% | 28.0% |
| negative-rotation-ccd1-1-previous | 73.1% | 80.8% | 65.3% |
| negative-rotation-ccd1-2-previous | 56.6% | 74.8% | 38.6% |
| negative-rotation-ccd1-3-current | 32.6% | 51.4% | 13.9% |
| negatives-ccd0 | 71.5% | 83.4% | 59.5% |

## Generation on ccd0

Source `56f04ae35ce5e98ecc596ad85dcb391e9d0fc204`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 113.28 | 218.76 | 95.46–227.85 | 3832 |
| Numeric | Modules | QRCoder | 168.55 | 337.83 | 101.44–396.44 | 5568 |
| Numeric | Modules | ZXing-C++ | 52.18 | 135.41 | 38.07–178.34 | 1696 |
| Numeric | Modules | ZXing.Net | 94.32 | 121.98 | 56.04–134.04 | 31304 |
| Numeric | Png | CodeGlyphX | 130.46 | 154.02 | 107.85–154.48 | 40340 |
| Numeric | Png | QRCoder | 219.26 | 260.57 | 179.06–295.30 | 6737 |
| Numeric | Png | ZXing-C++ | 1345.65 | 1439.03 | 1251.74–1451.96 | 848740 |
| Numeric | Png | ZXing.Net | 1303.26 | 1401.46 | 905.56–1401.81 | 877952 |
| Unicode | Modules | CodeGlyphX | 2292.22 | 5153.23 | 1959.62–5518.96 | 23872 |
| Unicode | Modules | QRCoder | 3789.43 | 9303.98 | 3008.44–11377.52 | 23220 |
| Unicode | Modules | ZXing-C++ | 1611.21 | 2422.84 | 1213.51–2827.71 | 14648 |
| Unicode | Modules | ZXing.Net | 2267.68 | 4507.11 | 1905.87–4993.55 | 606585 |
| Unicode | Png | CodeGlyphX | 2246.36 | 2499.71 | 1985.81–2581.01 | 274942 |
| Unicode | Png | QRCoder | 3622.48 | 4298.94 | 3259.32–4489.67 | 26792 |
| Unicode | Png | ZXing-C++ | 9012.46 | 9475.16 | 7975.74–9502.39 | 6207054 |
| Unicode | Png | ZXing.Net | 10508.34 | 11502.04 | 9129.95–11788.28 | 6798531 |
| Url | Modules | CodeGlyphX | 1131.01 | 1648.82 | 190.76–1783.75 | 5063 |
| Url | Modules | QRCoder | 1289.72 | 1554.43 | 792.20–1561.86 | 7417 |
| Url | Modules | ZXing-C++ | 187.82 | 239.08 | 116.32–243.01 | 2707 |
| Url | Modules | ZXing.Net | 967.76 | 1307.00 | 257.89–1574.83 | 71135 |
| Url | Png | CodeGlyphX | 258.10 | 343.42 | 220.74–355.84 | 60305 |
| Url | Png | QRCoder | 559.34 | 696.18 | 490.42–733.68 | 8375 |
| Url | Png | ZXing-C++ | 1743.27 | 2224.26 | 1549.51–2383.33 | 1309148 |
| Url | Png | ZXing.Net | 1910.75 | 3172.71 | 1573.19–3828.63 | 1377700 |

## Generation on ccd1

Source `56f04ae35ce5e98ecc596ad85dcb391e9d0fc204`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 112.16 | 175.96 | 70.43–209.62 | 3832 |
| Numeric | Modules | QRCoder | 126.05 | 172.88 | 79.03–191.31 | 5568 |
| Numeric | Modules | ZXing-C++ | 53.16 | 156.06 | 35.74–243.71 | 1696 |
| Numeric | Modules | ZXing.Net | 83.18 | 148.76 | 42.21–170.77 | 31304 |
| Numeric | Png | CodeGlyphX | 176.02 | 265.99 | 132.59–335.92 | 40340 |
| Numeric | Png | QRCoder | 267.17 | 401.89 | 219.28–449.50 | 6737 |
| Numeric | Png | ZXing-C++ | 1312.86 | 1524.71 | 935.25–1548.27 | 848463 |
| Numeric | Png | ZXing.Net | 1271.79 | 1512.08 | 1051.33–1567.43 | 877787 |
| Unicode | Modules | CodeGlyphX | 2227.24 | 2759.04 | 1740.68–3061.24 | 23872 |
| Unicode | Modules | QRCoder | 3454.13 | 4256.98 | 2646.80–4728.73 | 23220 |
| Unicode | Modules | ZXing-C++ | 1412.66 | 1659.29 | 1173.03–1713.51 | 14648 |
| Unicode | Modules | ZXing.Net | 2166.10 | 3023.69 | 1654.18–3268.66 | 606585 |
| Unicode | Png | CodeGlyphX | 1824.98 | 2129.37 | 1684.15–2270.57 | 274952 |
| Unicode | Png | QRCoder | 2990.87 | 3280.35 | 2823.99–3372.87 | 26397 |
| Unicode | Png | ZXing-C++ | 7247.04 | 8155.93 | 6798.52–8505.88 | 6207205 |
| Unicode | Png | ZXing.Net | 7768.21 | 9417.90 | 7120.81–9605.72 | 6798621 |
| Url | Modules | CodeGlyphX | 846.13 | 1091.44 | 578.38–1255.78 | 5057 |
| Url | Modules | QRCoder | 1053.08 | 1301.58 | 839.09–1327.43 | 7417 |
| Url | Modules | ZXing-C++ | 129.77 | 227.95 | 100.74–250.23 | 2711 |
| Url | Modules | ZXing.Net | 830.83 | 1011.69 | 661.40–1065.11 | 71146 |
| Url | Png | CodeGlyphX | 170.50 | 327.42 | 146.30–394.58 | 60324 |
| Url | Png | QRCoder | 444.57 | 559.50 | 393.62–577.50 | 8375 |
| Url | Png | ZXing-C++ | 1446.45 | 1705.54 | 1227.24–1724.08 | 1309198 |
| Url | Png | ZXing.Net | 1438.29 | 1685.83 | 1278.44–1776.94 | 1377669 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1535.58 | 1624.83 | 1449.07–1625.22 | 79.57 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 27.20 | 31.25 | 24.39–31.28 | 2.21 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 202.56 | 236.11 | 177.09–250.99 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1482.21 | 1560.06 | 1405.47–1562.48 | 80.21 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 25.24 | 35.70 | 20.32–43.97 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 184.23 | 235.64 | 166.71–252.39 | 12.99 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 2.8% higher on CCD0, 1.1% higher on CCD1.

Current unfinished image searches occur in 2/24 measured batches, versus 0/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2210.73 | 2353.89 | 2047.28–2426.74 | 81.04 | 19–19 | 0–0 | 0–0 | 1.028 |
| ccd0 | Candidate | 2273.33 | 2706.63 | 2078.47–2889.01 | 79.01 | 18–19 | 0–0 | 0–1 | 1.028 |
| ccd1 | Baseline | 1939.63 | 2087.35 | 1811.05–2168.94 | 82.37 | 19–19 | 0–0 | 0–0 | 1.011 |
| ccd1 | Candidate | 1960.61 | 2162.88 | 1899.75–2331.60 | 81.79 | 19–19 | 0–0 | 0–0 | 1.011 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
