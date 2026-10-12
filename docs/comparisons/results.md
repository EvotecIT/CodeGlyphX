# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 239/267 | 0 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 6 | 6 | 6 |
| Code128 | 14 | 5 | 13 | 13 | 13 |
| Code39 | 12 | 1 | 12 | 12 | 12 |
| Code93 | 3 | 2 | 3 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 21 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 7 | 8 | 8 |
| Itf | 12 | 10 | 12 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 27 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 239/267 correct positive images, compared with 239/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

## Historical ITF complete-symbol recovery

These diagnostics and paired costs belong to source `56f04ae35ce5e98ecc596ad85dcb391e9d0fc204`. They describe the earlier ITF change; current corpus, retail and whole-image measurements are separate.

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

Retail scanline recovery reuses sliding minima and maxima across adjacent contrast cutoffs, reducing window construction from eight passes to four. All eight binary hypotheses, their order, pixel classification, cancellation checks, guards, parity, checksums and neighboring-row corroboration remain enforced. The additional pooled storage is two bytes per scanline pixel during retail recovery.

Supplementary contract diagnostics, not a representative accuracy or negative-rate survey. Thirty-one fixed negative IDs are each requested with three explicit retail-family hints (93 requests).

The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.

| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 69/87 | 69/87 | 0 |
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
| ccd0 | 638.21 | 551.55 | 0.86x | 33.92 | 33.92 | 1.00x | 9/9 → 9/9 |
| ccd1 | 630.78 | 538.81 | 0.85x | 33.89 | 33.89 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

Same 87 preloaded retail positives with identical hints, payload validation and 1500 ms allowance. Separate contexts, 3 warmups and 12 rotated measurements per binary/domain, Normal priority and fixed affinity; no outlier removal. Loading and evaluation are excluded. Correctness is recorded with cost; successful decode counts must be read alongside the timing. Managed allocation excludes native costs. All measured samples and failed warmups are retained; successful warmup samples are not retained by PowerForge.

| Domain | Previous → current median ms / 87 positives | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 87 |
| --- | ---: | ---: | ---: | ---: |
| CCD0 | 916.329 → 751.747 | 0.820 | 91.27 → 90.60 | 69.000 → 69.000 |
| CCD1 | 1140.340 → 947.650 | 0.831 | 90.62 → 90.61 | 69.000 → 69.000 |

[Retail recovery and cost proof](evidence/retail-row-recovery-proof.json) · [CCD0 positive cost samples](evidence/retail-positive-cost-ccd0-result.json) · [CCD1 positive cost samples](evidence/retail-positive-cost-ccd1-result.json).

The current optimization reuses sliding minima and maxima across adjacent cutoffs. It preserves all eight binary hypotheses and their order. The actual previous and current binarizers agree on 39,820 masks and 23,593,100 pixel decisions across 3,982 fixed-image and seeded diagnostic rows. Independent direct extrema checks cover 270,776 positions. Precancelled and expired-budget calls abort. These deterministic checks establish output equivalence on their inputs; they are not an accuracy survey.

[Extrema equivalence proof](evidence/retail-extrema-equivalence.json). Earlier photograph recovery and its exact preceding-binary failures remain in the [historical row-recovery proof](evidence/historical-retail-row-recovery-proof.json); those gains are not attributed to the current optimization.

The current pass records 239/267 correct positive images versus 239/267 previously; gained: none; previously correct positive images lost: none. The current optimization preserves every binary hypothesis and its order, guard geometry, digit-fit bounds, parity, checksums and matching neighboring physical rows. Hints, oracles, format priorities, presentation and recognition allowances are unchanged. Earlier photograph recovery, ITF, Code 39, Data Matrix, PDF417, QR and Codabar improvement measurements remain historical. Current corpus, retail and whole-image costs are rerun on the frozen core. Both binaries include MaxiCode recognition. Supplemental PDF417 and MaxiCode images have informed fixtures and are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and wall-clock budgets affect results. Multi-instance and mirrored MaxiCode, remaining stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 37.3%, 31.6%, 25.9%. Before the supplementary negative pass: 17.2%, 17.1%, 23.6%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 31.7% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 23.8% |
| ccd0 | 3 | current | 31/31 | 0 | 0 | 20.0% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 25.4% |
| ccd1 | 1 | current | 29/31 | 2 | 0 | 17.6% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 12.2% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 36.5% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 20.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 31.6% | 48.3% | 14.9% |
| decoding-ccd0 | 14.9% | 24.9% | 5.2% |
| decoding-ccd1 | 81.7% | 85.8% | 77.8% |
| diagnostic-final-codabar-diagnostic-current | 24.2% | 33.8% | 14.6% |
| diagnostic-final-codabar-diagnostic-previous | 50.0% | 58.6% | 41.5% |
| diagnostic-final-codabar-supplemental-current | 33.0% | 43.0% | 23.0% |
| diagnostic-final-codabar-supplemental-previous | 34.4% | 43.0% | 25.7% |
| diagnostic-final-code39-negative-current | 43.1% | 55.3% | 30.9% |
| diagnostic-final-code39-negative-previous | 34.4% | 47.3% | 21.2% |
| diagnostic-final-code39-positive-current | 46.2% | 57.6% | 34.7% |
| diagnostic-final-code39-positive-previous | 22.7% | 33.2% | 12.1% |
| diagnostic-final-itf-negative-current | 20.3% | 36.8% | 3.6% |
| diagnostic-final-itf-negative-previous | 44.9% | 58.6% | 30.9% |
| diagnostic-final-itf-positive-current | 32.4% | 51.5% | 13.2% |
| diagnostic-final-itf-positive-previous | 37.1% | 49.6% | 24.2% |
| diagnostic-final-retail-negative-current | 15.8% | 28.0% | 3.7% |
| diagnostic-final-retail-negative-previous | 36.0% | 47.5% | 24.6% |
| diagnostic-final-retail-positive-current | 33.7% | 46.3% | 21.1% |
| diagnostic-final-retail-positive-previous | 14.1% | 24.2% | 4.2% |
| generation-ccd0 | 14.9% | 25.9% | 4.1% |
| generation-ccd1 | 61.5% | 71.1% | 51.9% |
| holdout-current | 34.6% | 48.0% | 21.3% |
| negative-rotation-ccd0-0-previous | 31.7% | 46.9% | 16.5% |
| negative-rotation-ccd0-1-current | 23.8% | 37.8% | 9.6% |
| negative-rotation-ccd0-2-current | 20.0% | 33.6% | 6.5% |
| negative-rotation-ccd0-3-previous | 25.4% | 35.6% | 15.2% |
| negative-rotation-ccd1-0-current | 17.6% | 28.7% | 6.7% |
| negative-rotation-ccd1-1-previous | 12.2% | 20.4% | 4.2% |
| negative-rotation-ccd1-2-previous | 36.5% | 51.0% | 22.0% |
| negative-rotation-ccd1-3-current | 20.7% | 33.1% | 8.3% |
| negatives-ccd0 | 19.3% | 33.5% | 4.9% |

## Generation on ccd0

Source `5145edfb766f1ced33dfc6427b65cfdbd2aa7eb3`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 105.28 | 160.00 | 86.61–172.86 | 3832 |
| Numeric | Modules | QRCoder | 158.93 | 321.19 | 111.84–454.52 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.75 | 247.48 | 37.16–352.98 | 1696 |
| Numeric | Modules | ZXing.Net | 102.56 | 202.81 | 60.22–217.85 | 31304 |
| Numeric | Png | CodeGlyphX | 123.44 | 190.29 | 96.11–209.89 | 40312 |
| Numeric | Png | QRCoder | 212.42 | 259.77 | 187.14–288.98 | 6568 |
| Numeric | Png | ZXing-C++ | 903.77 | 1059.73 | 798.02–1085.47 | 848319 |
| Numeric | Png | ZXing.Net | 975.32 | 1128.58 | 865.51–1169.26 | 877581 |
| Unicode | Modules | CodeGlyphX | 1896.17 | 2049.90 | 1733.54–2092.13 | 23872 |
| Unicode | Modules | QRCoder | 2922.16 | 4062.54 | 2821.64–4843.76 | 23217 |
| Unicode | Modules | ZXing-C++ | 1225.74 | 1599.61 | 1115.20–1675.96 | 14648 |
| Unicode | Modules | ZXing.Net | 1879.64 | 2422.43 | 1697.23–2424.01 | 606584 |
| Unicode | Png | CodeGlyphX | 1964.45 | 2151.06 | 1853.52–2205.70 | 274832 |
| Unicode | Png | QRCoder | 3166.57 | 3380.23 | 3068.60–3454.09 | 26112 |
| Unicode | Png | ZXing-C++ | 7972.48 | 9557.51 | 7513.92–10102.69 | 6206713 |
| Unicode | Png | ZXing.Net | 9587.53 | 10321.21 | 8329.34–10538.84 | 6798205 |
| Url | Modules | CodeGlyphX | 884.23 | 1006.40 | 217.52–1010.21 | 5057 |
| Url | Modules | QRCoder | 1090.33 | 1303.47 | 951.47–1434.45 | 7409 |
| Url | Modules | ZXing-C++ | 163.70 | 228.16 | 119.05–261.58 | 2709 |
| Url | Modules | ZXing.Net | 888.91 | 1005.78 | 228.82–1033.38 | 71141 |
| Url | Png | CodeGlyphX | 449.82 | 756.27 | 235.44–837.71 | 60278 |
| Url | Png | QRCoder | 780.41 | 1338.48 | 518.89–1463.74 | 8200 |
| Url | Png | ZXing-C++ | 2968.05 | 3839.87 | 1605.62–4034.61 | 1309014 |
| Url | Png | ZXing.Net | 3210.45 | 4742.61 | 1803.90–4835.04 | 1377492 |

## Generation on ccd1

Source `5145edfb766f1ced33dfc6427b65cfdbd2aa7eb3`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 82.37 | 148.99 | 63.73–164.58 | 3832 |
| Numeric | Modules | QRCoder | 95.36 | 171.19 | 71.26–221.56 | 5568 |
| Numeric | Modules | ZXing-C++ | 32.88 | 59.93 | 27.82–63.51 | 1696 |
| Numeric | Modules | ZXing.Net | 48.79 | 73.99 | 39.29–82.88 | 31304 |
| Numeric | Png | CodeGlyphX | 153.99 | 260.89 | 98.62–268.82 | 40312 |
| Numeric | Png | QRCoder | 266.68 | 388.44 | 171.24–415.99 | 6568 |
| Numeric | Png | ZXing-C++ | 1157.16 | 1473.37 | 959.73–1485.00 | 848302 |
| Numeric | Png | ZXing.Net | 1079.48 | 1561.38 | 787.89–1718.66 | 877600 |
| Unicode | Modules | CodeGlyphX | 1829.91 | 1916.63 | 1628.69–1964.58 | 23872 |
| Unicode | Modules | QRCoder | 2778.79 | 2945.86 | 2526.58–2994.19 | 23219 |
| Unicode | Modules | ZXing-C++ | 1168.63 | 1339.28 | 1076.11–1357.73 | 14648 |
| Unicode | Modules | ZXing.Net | 1757.21 | 1870.70 | 1606.88–1887.97 | 606585 |
| Unicode | Png | CodeGlyphX | 1811.13 | 2498.98 | 1664.12–2572.35 | 274942 |
| Unicode | Png | QRCoder | 3081.54 | 3789.85 | 2663.26–3803.97 | 26445 |
| Unicode | Png | ZXing-C++ | 6993.06 | 11154.64 | 6165.22–12195.15 | 6206982 |
| Unicode | Png | ZXing.Net | 8548.45 | 11921.93 | 7549.90–12769.51 | 6798342 |
| Url | Modules | CodeGlyphX | 1090.57 | 1392.04 | 218.29–1496.70 | 5057 |
| Url | Modules | QRCoder | 1367.80 | 1858.25 | 914.95–1989.80 | 7412 |
| Url | Modules | ZXing-C++ | 206.97 | 282.15 | 116.97–321.66 | 2709 |
| Url | Modules | ZXing.Net | 1162.81 | 1993.72 | 278.67–2343.96 | 71137 |
| Url | Png | CodeGlyphX | 186.35 | 381.04 | 140.07–403.07 | 60291 |
| Url | Png | QRCoder | 418.94 | 727.16 | 375.74–833.80 | 8266 |
| Url | Png | ZXing-C++ | 1341.93 | 1591.95 | 1249.53–1733.80 | 1309122 |
| Url | Png | ZXing.Net | 1553.06 | 2265.74 | 1328.74–2653.64 | 1377583 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1508.00 | 1827.62 | 1383.39–1989.94 | 85.57 | 18–19 | 0–0 |
| ccd0 | ZXing-C++ | 24.22 | 30.05 | 19.41–32.27 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 179.34 | 237.28 | 151.76–241.62 | 13.02 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1446.90 | 1537.50 | 1376.09–1560.28 | 87.64 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 19.79 | 20.82 | 18.12–21.57 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 147.63 | 170.20 | 145.24–171.68 | 12.98 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 1.8% lower on CCD0, 0.4% lower on CCD1.

Current unfinished image searches occur in 1/24 measured batches, versus 3/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2007.52 | 2315.75 | 1880.61–2330.23 | 86.64 | 19–19 | 0–0 | 0–0 | 0.982 |
| ccd0 | Candidate | 1971.33 | 2231.51 | 1832.65–2248.84 | 86.74 | 19–19 | 0–0 | 0–0 | 0.982 |
| ccd1 | Baseline | 1970.68 | 2636.84 | 1898.79–2726.18 | 85.09 | 18–19 | 0–0 | 0–1 | 0.996 |
| ccd1 | Candidate | 1963.76 | 2568.83 | 1812.63–2613.99 | 85.95 | 18–19 | 0–0 | 0–1 | 0.996 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
