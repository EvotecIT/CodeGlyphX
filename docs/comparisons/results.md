# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 240/267 | 0 | 3 | 0 | 1 |
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
| Gs1DataBarOmnidirectional | 12 | 4 | 8 | 8 | 8 |
| Itf | 12 | 10 | 12 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 27 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 240/267 correct positive images, compared with 240/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

## Historical retail recovery and search cost

The diagnostics and paired costs in this section belong to source 5f41d326a8c332853096d888059504677cada46e; they retain their original binary identities.

Retail contrast recovery is unchanged in this pass. Current-core diagnostics and paired costs preserve all hypotheses, independent neighboring-row confirmation and prior image gains.

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
| ccd0 | 655.10 | 638.41 | 0.97x | 33.90 | 33.90 | 1.00x | 9/9 → 9/9 |
| ccd1 | 546.64 | 534.12 | 0.98x | 33.91 | 33.92 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

Same 87 preloaded retail positives with identical hints, payload validation and 1500 ms allowance. Separate contexts, 3 warmups and 12 rotated measurements per binary/domain, Normal priority and fixed affinity; no outlier removal. Loading and evaluation are excluded. Correctness is recorded with cost; successful decode counts must be read alongside the timing. Managed allocation excludes native costs. All measured samples and failed warmups are retained; successful warmup samples are not retained by PowerForge.

| Domain | Previous → current median ms / 87 positives | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 87 |
| --- | ---: | ---: | ---: | ---: |
| CCD0 | 1013.177 → 985.130 | 0.972 | 90.62 → 90.61 | 69.000 → 69.000 |
| CCD1 | 822.772 → 814.014 | 0.989 | 90.63 → 90.61 | 69.000 → 69.000 |

[Retail recovery and cost proof](evidence/retail-row-recovery-proof.json) · [CCD0 positive cost samples](evidence/retail-positive-cost-ccd0-result.json) · [CCD1 positive cost samples](evidence/retail-positive-cost-ccd1-result.json).

Retail thresholding reuses sliding minima and maxima across adjacent cutoffs. It preserves all eight binary hypotheses and their order. The actual previous and current binarizers agree on 39,820 masks and 23,593,100 pixel decisions across 3,982 fixed-image and seeded diagnostic rows. Independent direct extrema checks cover 270,776 positions. Precancelled and expired-budget calls abort. These deterministic checks establish output equivalence on their inputs; they are not an accuracy survey.

[Extrema equivalence proof](evidence/retail-extrema-equivalence.json). Earlier photograph recovery and its exact preceding-binary failures remain in the [historical row-recovery proof](evidence/historical-retail-row-recovery-proof.json); those gains are not attributed to the current optimization.

## Historical GS1 DataBar-14 character pitch recovery

The diagnostics and paired costs in this section belong to source 5f41d326a8c332853096d888059504677cada46e; current whole-image qualification appears separately.

GS1 DataBar-14 image recognition fits both finders and four characters independently, validates the complete symbol checksum, and preserves measured enclosed guard gaps before normalizing. The same guard invariant is enforced in stacked bottom rows. Existing APIs, payload oracles, family hints and recognition allowances are unchanged.

Twelve fixed RSS-14 family images from the main corpus, with the same full-family hint for every engine. They inform the repair and are regression evidence, not an unseen holdout or an additional accuracy survey.

| Reader | Previous correct / 12 | Current correct / 12 | Current unexpected output |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 7 | 8 | 0 |
| ZXing.Net | 8 | 8 | 0 |
| ZXing-C++ | 8 | 8 | 0 |

All 4 new photograph cases fail against the exact preceding binary and pass on the current core. They cover both axes and polarities through single-result pixels, multi-result pixels and the scanner. The 31 family-hinted negative requests remain separate from the main ranking.

Same twelve preloaded DataBar-14 images and 1500 ms allowance, separate binary contexts, three warmups and twelve rotated measurements per binary/domain. Normal priority, fixed affinity and no outlier removal. Loading and evaluation are excluded. Recognition is recorded with time: additional correct results change completed work. Shared-host contention prevents portable latency claims; managed allocation excludes native memory.

| Domain | Previous → current median ms / 12 images | Current / previous | Previous → current correct / 12 | Previous → current managed MiB |
| --- | ---: | ---: | ---: | ---: |
| CCD0 | 164.15 → 164.01 | 0.999 | 7 → 8 | 16.15 → 15.63 |
| CCD1 | 145.31 → 147.16 | 1.013 | 7 → 8 | 16.14 → 15.63 |

[DataBar-14 recovery proof](evidence/databar14-run-recovery-proof.json) · [Positive requests](evidence/databar14-positive-manifest.json) · [Negative requests](evidence/databar14-negative-manifest.json) · [CCD0 samples](evidence/databar14-cost-ccd0-result.json) · [CCD1 samples](evidence/databar14-cost-ccd1-result.json).

## QR image cleanup and negative-search cost

QR closing, opening and edge filters use moving column counts instead of rescanning each square. Clipped borders, thresholds and output pixels remain unchanged.

Four unchanged negative images that reached deadlines in the preceding qualification. Kernel measurements apply closing and opening at radii 1, 2 and 3 to hybrid-threshold images. Whole-image measurements use the same RGBA32, format hints and 1500 ms robust allowance. Three warmups, twelve rotated measurements per binary/domain, Normal priority, no outlier removal. Setup, hashes and scoring are outside measurement.

Every output pixel matches the preceding binary on 24 closing/opening filters over four corpus images. Eight direct-neighborhood cases pass on each modern framework, covering solid and mixed images, clipped borders, narrow dimensions and adaptive thresholds.

| Domain | Previous → current median ms / 24 filters | Current / previous | Previous → current mean managed MiB |
| --- | ---: | ---: | ---: |
| CCD0 | 140.40 → 59.78 | 0.426 | 2.18 → 2.12 |
| CCD1 | 118.36 → 50.29 | 0.425 | 2.10 → 2.11 |

The same four difficult negatives are also measured through the complete scanner. Completed-image ranges retain every measured batch; an unfinished empty search remains inconclusive.

| Domain | Previous → current median ms / 4 images | Current / previous | Previous → current completed / 4 range (mean) | Previous → current inconclusive range (mean) | Previous → current mean managed MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 2801.25 → 2991.44 | 1.068 | 2–4 (3.67) → 3–4 (3.42) | 0–2 (0.33) → 0–1 (0.58) | 352.54 → 352.19 |
| CCD1 | 2776.71 → 2748.80 | 0.990 | 3–4 (3.67) → 3–4 (3.92) | 0–1 (0.33) → 0–1 (0.08) | 349.91 → 351.16 |

Kernel speed does not establish equal whole-image improvement or eliminate timeouts. Every inconclusive result remains visible. These four images informed this optimization and are regression evidence; shared-host timing is not a portable ranking.

[Proof and output hashes](evidence/qr-morphology-cost-proof.json) · [Unchanged images and oracles](evidence/qr-morphology-negative-manifest.json) · [CCD0 kernel samples](evidence/qr-morphology-cost-ccd0-result.json) · [CCD1 kernel samples](evidence/qr-morphology-cost-ccd1-result.json) · [CCD0 whole-image samples](evidence/qr-negative-cost-ccd0-result.json) · [CCD1 whole-image samples](evidence/qr-negative-cost-ccd1-result.json).

Current corpus, alternating negatives, generation, whole-image decoding and QR morphology costs are rerun on the frozen tested core. Earlier decoder-specific discovery and cost packets retain their original identities. Binary morphology pixels are unchanged; hints, oracles, format priorities and recognition allowances are unchanged. Wall-clock deadlines and shared-host contention still affect completion. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 9.4%, 6.3%, 6.8%. Before the supplementary negative pass: 11.0%, 13.8%, 14.6%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 24/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 32.4% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 8.8% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 31.9% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 34.9% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 35.2% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 14.4% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 15.0% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 15.4% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| before-ccd0 | 83.8% | 88.5% | 79.1% |
| before-ccd1 | 28.4% | 49.5% | 7.9% |
| corpus-ccd0 | 7.5% | 14.3% | 1.1% |
| decoding-ccd0 | 33.0% | 46.9% | 19.1% |
| decoding-ccd1 | 44.2% | 57.4% | 31.4% |
| exploratory-ccd0 | 62.0% | 71.6% | 52.3% |
| final-morphology-ccd0 | 26.3% | 42.0% | 10.7% |
| final-morphology-ccd1 | 36.9% | 50.8% | 23.0% |
| final-negative-ccd0 | 58.3% | 68.5% | 48.0% |
| final-negative-ccd1 | 39.3% | 54.0% | 24.7% |
| generation-ccd0 | 17.8% | 30.2% | 5.5% |
| generation-ccd1 | 12.3% | 22.9% | 2.1% |
| holdout-current | 30.8% | 44.8% | 17.0% |
| holdout-previous | 22.8% | 36.5% | 9.2% |
| morphology-ccd0 | 18.1% | 30.2% | 6.2% |
| negative-rotation-ccd0-0-previous | 32.4% | 44.4% | 20.5% |
| negative-rotation-ccd0-1-current | 8.8% | 16.3% | 1.5% |
| negative-rotation-ccd0-2-current | 31.9% | 46.0% | 17.8% |
| negative-rotation-ccd0-3-previous | 34.9% | 48.0% | 21.7% |
| negative-rotation-ccd1-0-current | 35.2% | 48.2% | 22.2% |
| negative-rotation-ccd1-1-previous | 14.4% | 25.5% | 3.4% |
| negative-rotation-ccd1-2-previous | 15.0% | 27.5% | 2.7% |
| negative-rotation-ccd1-3-current | 15.4% | 28.4% | 2.6% |
| negatives-ccd0 | 13.1% | 23.1% | 3.2% |
| paired-decode-cost-ccd0-12 | 42.5% | 55.4% | 29.6% |
| paired-decode-cost-ccd1-12 | 90.2% | 93.2% | 87.2% |

## Generation on ccd0

Source `418fc0728d2d5fa5e29247de4bc5bacfdafd5f70`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 92.65 | 156.09 | 75.68–179.38 | 3832 |
| Numeric | Modules | QRCoder | 120.91 | 184.41 | 98.33–200.54 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.19 | 79.94 | 34.16–83.54 | 1696 |
| Numeric | Modules | ZXing.Net | 85.69 | 165.91 | 55.38–178.06 | 31304 |
| Numeric | Png | CodeGlyphX | 130.88 | 180.89 | 98.20–193.72 | 40312 |
| Numeric | Png | QRCoder | 220.38 | 282.83 | 183.39–298.02 | 6568 |
| Numeric | Png | ZXing-C++ | 1009.32 | 1167.37 | 879.61–1240.16 | 848318 |
| Numeric | Png | ZXing.Net | 1018.63 | 1230.47 | 881.94–1287.60 | 877583 |
| Unicode | Modules | CodeGlyphX | 1820.30 | 1916.09 | 1783.29–1942.49 | 23872 |
| Unicode | Modules | QRCoder | 2804.46 | 2869.63 | 2713.51–2888.03 | 23216 |
| Unicode | Modules | ZXing-C++ | 1194.75 | 1240.95 | 1171.12–1250.36 | 14648 |
| Unicode | Modules | ZXing.Net | 1767.31 | 1822.62 | 1702.61–1832.52 | 606584 |
| Unicode | Png | CodeGlyphX | 1992.61 | 2226.85 | 1870.83–2249.69 | 274832 |
| Unicode | Png | QRCoder | 3259.90 | 3956.05 | 3082.79–4222.92 | 26112 |
| Unicode | Png | ZXing-C++ | 7967.97 | 9304.74 | 7587.94–9613.15 | 6206825 |
| Unicode | Png | ZXing.Net | 10137.58 | 11825.72 | 9190.72–12108.44 | 6798122 |
| Url | Modules | CodeGlyphX | 847.22 | 1003.86 | 206.44–1064.05 | 5057 |
| Url | Modules | QRCoder | 1044.43 | 1146.94 | 815.77–1162.61 | 7415 |
| Url | Modules | ZXing-C++ | 175.60 | 213.38 | 104.18–235.97 | 2709 |
| Url | Modules | ZXing.Net | 843.28 | 934.90 | 205.72–952.07 | 71141 |
| Url | Png | CodeGlyphX | 202.19 | 276.68 | 169.02–319.20 | 60278 |
| Url | Png | QRCoder | 477.64 | 494.97 | 444.16–495.78 | 8200 |
| Url | Png | ZXing-C++ | 1478.27 | 1534.65 | 1366.38–1542.33 | 1309013 |
| Url | Png | ZXing.Net | 1560.97 | 1712.64 | 1429.35–1732.01 | 1377510 |

## Generation on ccd1

Source `418fc0728d2d5fa5e29247de4bc5bacfdafd5f70`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 83.44 | 108.87 | 60.74–122.91 | 3832 |
| Numeric | Modules | QRCoder | 97.08 | 134.83 | 75.76–145.14 | 5568 |
| Numeric | Modules | ZXing-C++ | 41.36 | 65.92 | 30.28–69.31 | 1696 |
| Numeric | Modules | ZXing.Net | 55.18 | 93.47 | 44.52–94.93 | 31304 |
| Numeric | Png | CodeGlyphX | 89.44 | 119.49 | 79.53–123.29 | 40312 |
| Numeric | Png | QRCoder | 155.85 | 179.87 | 149.41–181.33 | 6568 |
| Numeric | Png | ZXing-C++ | 752.67 | 814.33 | 702.22–819.03 | 848317 |
| Numeric | Png | ZXing.Net | 784.93 | 862.83 | 709.47–913.60 | 877616 |
| Unicode | Modules | CodeGlyphX | 1632.21 | 1700.20 | 1501.32–1710.33 | 23872 |
| Unicode | Modules | QRCoder | 2481.60 | 2635.00 | 2313.40–2684.40 | 23216 |
| Unicode | Modules | ZXing-C++ | 1058.03 | 1160.04 | 1007.12–1165.40 | 14648 |
| Unicode | Modules | ZXing.Net | 1611.08 | 1736.66 | 1499.16–1789.40 | 606585 |
| Unicode | Png | CodeGlyphX | 1788.80 | 1942.14 | 1710.26–2020.24 | 274832 |
| Unicode | Png | QRCoder | 2846.75 | 3131.92 | 2753.99–3194.33 | 26112 |
| Unicode | Png | ZXing-C++ | 7044.71 | 7700.87 | 6692.74–7878.73 | 6206907 |
| Unicode | Png | ZXing.Net | 7821.03 | 9417.53 | 7228.67–9802.31 | 6798420 |
| Url | Modules | CodeGlyphX | 713.41 | 830.59 | 627.22–844.97 | 5057 |
| Url | Modules | QRCoder | 883.86 | 1086.56 | 786.73–1116.98 | 7415 |
| Url | Modules | ZXing-C++ | 125.27 | 176.83 | 97.80–186.76 | 2709 |
| Url | Modules | ZXing.Net | 736.88 | 973.69 | 641.40–1002.57 | 71146 |
| Url | Png | CodeGlyphX | 189.87 | 297.76 | 146.93–349.48 | 60280 |
| Url | Png | QRCoder | 429.04 | 540.36 | 402.94–553.37 | 8200 |
| Url | Png | ZXing-C++ | 1416.97 | 1683.48 | 1260.50–1796.72 | 1309049 |
| Url | Png | ZXing.Net | 1418.63 | 1731.03 | 1322.41–1739.92 | 1377464 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1504.08 | 1560.29 | 1470.41–1570.02 | 85.59 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 27.15 | 42.78 | 23.68–56.73 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 196.55 | 217.07 | 189.12–219.83 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1477.52 | 1552.46 | 1384.62–1556.99 | 86.41 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 23.12 | 27.18 | 19.72–27.47 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 176.53 | 197.58 | 166.96–205.80 | 12.99 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 1982.80 | 2187.42 | 1911.97–2262.90 | 87.04 | 19–19 | 0–0 | 0–0 | 1.005 |
| ccd0 | Candidate | 1991.82 | 2174.88 | 1845.85–2237.67 | 87.36 | 19–19 | 0–0 | 0–0 | 1.005 |
| ccd1 | Baseline | 2083.70 | 2369.30 | 1936.82–2438.75 | 89.24 | 19–19 | 0–0 | 0–0 | 0.960 |
| ccd1 | Candidate | 1999.53 | 2536.67 | 1899.27–2596.59 | 87.95 | 18–19 | 0–0 | 0–1 | 0.960 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
