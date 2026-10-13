# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

Host CPU sampled before the current corpus and timing passes ranged from 13.5% to 73.3%. These initial readings do not describe load throughout each operation. Shared-host contention limits timing comparisons and can change completion under the fixed deadline.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 242/267 | 0 | 3 | 0 | 1 |
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
| Ean | 30 | 4 | 22 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 9 | 8 | 8 |
| Itf | 12 | 10 | 12 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 27 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 242/267 correct positive images, compared with 242/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All 38 PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The selection was fixed before the first supplemental outcomes; its image bytes, oracles and format hints remain unchanged.

This unchanged 38-image set comes from the same upstream collection as the ranking. Five images informed permanent regression fixtures across the remediation rounds; it is supplemental regression evidence, not an unseen holdout. Outcomes are rerun on both exact binaries in this qualification.

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

## Historical GS1 DataBar tilted photographs

These retained recovery and paired costs belong to source 01832f81c81289094b11c32bb3434df302f3efd9; current whole-image qualification appears separately.

GS1 DataBar-14 edge guards use their adjacent character pitch and reject malformed guards before allocating character widths. Stacked separator validation first samples the existing flat path, then tries four bounded slopes inside the verified data-row gap. Finder, character, checksum, geometry and separator error limits remain unchanged.

Twelve unchanged RSS-14 family images from the main corpus. One tilted photograph informed this repair and is regression evidence. Four right-angle rotations pass on every supported framework; the same four tests fail with the exact previous core.

| Engine | Previous correct / 12 | Current correct / 12 | Unexpected images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 8 | 9 | 0 |
| ZXing.Net | 8 | 8 | 0 |
| ZXing-C++ | 8 | 8 | 0 |

Three warmups and twelve grouped rotated measurements per binary and affinity domain. Twelve fixed positive requests and thirty-one fixed negative requests are preloaded as RGBA32, using unchanged hints and 1500 ms robust allowance. Input loading, hashes and scoring are excluded; every sample, allocation and inconclusive outcome is retained. Shared-host timings are local evidence.

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current mean managed MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Positive: 12 | CCD0 | 146.29 → 149.44 | 1.022 | 8 → 9 | 0 → 0 | 15.68 → 15.60 |
| Positive: 12 | CCD1 | 167.15 → 161.06 | 0.964 | 8 → 9 | 0 → 0 | 15.63 → 15.54 |
| Negative: 31 | CCD0 | 547.26 → 573.68 | 1.048 | 31 → 31 | 0 → 0 | 107.75 → 108.07 |
| Negative: 31 | CCD1 | 422.77 → 425.83 | 1.007 | 31 → 31 | 0 → 0 | 107.73 → 108.04 |

The remaining three main-corpus DataBar misses and inverted versions of the recovered photograph remain open. This photograph informed the fix; it does not establish unseen accuracy or general superiority. Wall-clock deadlines and shared-host contention can affect completion.

[Recovery and regression proof](evidence/databar-tilted-separator-proof.json) · [Positive inputs](evidence/databar14-positive-manifest.json) · [Negative inputs](evidence/databar14-negative-manifest.json) · [CCD0 positive samples](evidence/databar-tilted-positive-ccd0-result.json) · [CCD1 positive samples](evidence/databar-tilted-positive-ccd1-result.json) · [CCD0 negative samples](evidence/databar-tilted-negative-ccd0-result.json) · [CCD1 negative samples](evidence/databar-tilted-negative-ccd1-result.json).

## DPM local-window means

Direct-part-mark preprocessing stores clipped-window means as bytes and advances bounded column/window sums once per row. Dark and light retries share those means instead of retaining an image-sized Int64 summed-area table. Retry pixels, order, profiles, attempt limits and recognition allowances are unchanged; partial means are discarded on cancellation or deadline expiry.

Nine local-window cases and six DPM scanner cases pass on each of .NET 10, .NET 8 and .NET Framework 4.7.2. Window cases compare both adaptive polarities with output derived from independent clipped-neighborhood enumeration, including narrow images, radii larger than the image, clipped edges and a full-resolution boundary. All 2,025 retries across 675 diagnostic cases preserve the preceding binary’s normalized pixels and order.

The unchanged fabric negative falsepositives/15.png and texture negative falsepositives/19.png each receive the same unrestricted scan and 1500 ms allowance. Exact preceding and tested binaries use three warmups and twelve rotated measurements per fixed cache domain. Every completed negative, inconclusive request, unexpected output, latency and managed allocation remains visible. The full nineteen-image workload and retail controls are measured separately.

| Image | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current completed negatives / 1 | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | CCD0 | 1247.51 → 1237.99 | 0.992 | 61.42 → 59.19 | 3.6% | 0 → 0 | 1 → 1 | 0 → 0 |
| fabric | CCD1 | 1223.24 → 1222.70 | 1.000 | 70.77 → 67.41 | 4.7% | 0 → 0 | 1 → 1 | 0 → 0 |
| texture | CCD0 | 1021.94 → 1058.46 | 1.036 | 51.17 → 46.86 | 8.4% | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | CCD1 | 979.70 → 961.05 | 0.981 | 51.17 → 46.86 | 8.4% | 1 → 1 | 0 → 0 | 0 → 0 |

Retail controls retain their original requests, hints and oracles:

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1069.66 → 1038.25 | 0.971 | 91.28 → 91.27 | 70 → 70 | 0 → 0 | 0 → 0 |
| Positive: 87 | CCD1 | 956.56 → 947.30 | 0.990 | 91.26 → 91.25 | 70 → 70 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD0 | 737.19 → 730.64 | 0.991 | 32.57 → 32.57 | 9 → 9 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD1 | 630.61 → 627.98 | 0.996 | 32.56 → 32.56 | 9 → 9 | 0 → 0 | 0 → 0 |

Lower managed allocation does not establish faster or reliably completed negative scans, lower CPU use, throughput or memory capacity. Managed allocation excludes native allocations. Shared-host contention can change completion under fixed deadlines. Slower medians and lost completions remain included. These finite workloads do not establish overall superiority.

[Local-window proof](evidence/dpm-window-means-proof.json) · [Pixel equivalence](evidence/dpm-window-means-mask-equivalence.json) · [Fabric CCD0 samples](evidence/dpm-window-means-fabric-ccd0-result.json) · [Fabric CCD1 samples](evidence/dpm-window-means-fabric-ccd1-result.json) · [Texture CCD0 samples](evidence/dpm-window-means-texture-ccd0-result.json) · [Texture CCD1 samples](evidence/dpm-window-means-texture-ccd1-result.json).

## Historical Data Matrix grayscale retries

These grayscale retry measurements belong to source 6fb1250566eae8ad74fb464354c47795175f28bd; current costs appear separately.

Data Matrix reads Gray8 pixels directly through its existing pixel APIs. Direct-part-mark retries keep packed luminance buffers instead of expanding them to RGBA. Adaptive masks keep their original representation for morphology; retry pixels, order, attempts and recognition allowances are unchanged. Both axis-aligned and perspective sampling reject malformed layouts before accessing pixels.

On that core, on each of .NET 10, .NET 8 and .NET Framework 4.7.2, ten raw-pixel cases cover padded Gray8 rows, rotations, mirroring, inversion, GS1 metadata, cancellation and malformed layouts. Three Gray8 perspective cases and six DPM scanner cases also pass. All 675 retries across 225 diagnostic cases preserve the preceding binary’s normalized pixels and order.

One prespecified texture negative, falsepositives/19.png, from the unchanged nineteen-image workload receives the same unrestricted scan and 1500 ms allowance. Exact preceding and tested binaries are compared with three warmups and twelve rotated measurements on each fixed cache domain. Completed negatives, inconclusive requests, unexpected output, latency and managed allocation are retained together. The full nineteen-image workload and retail controls are measured separately.

| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current completed negatives / 1 | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 1019.49 → 1004.41 | 0.985 | 65.29 → 53.42 | 18.2% | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| CCD1 | 989.29 → 971.12 | 0.982 | 65.43 → 53.78 | 17.8% | 1 → 1 | 0 → 0 | 0 → 0 |

Retail controls retain their original requests, hints and oracles:

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1000.04 → 985.08 | 0.985 | 91.31 → 91.29 | 70 → 70 | 0 → 0 | 0 → 0 |
| Positive: 87 | CCD1 | 958.71 → 962.85 | 1.004 | 91.32 → 91.31 | 70 → 70 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD0 | 660.29 → 655.76 | 0.993 | 32.58 → 32.57 | 9 → 9 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD1 | 660.19 → 659.61 | 0.999 | 32.59 → 32.58 | 9 → 9 | 0 → 0 | 0 → 0 |

Smaller retry buffers do not establish reliable negative completion, lower CPU use, throughput or memory capacity on unseen workloads. Managed allocation excludes native allocations. Shared-host contention can change timing and completion under fixed deadlines. All losses and slower controls are retained; these finite workloads do not establish overall superiority.

[Grayscale retry proof](evidence/dpm-gray8-proof.json) · [Pixel equivalence](evidence/dpm-gray8-mask-equivalence.json) · [CCD0 samples](evidence/dpm-gray8-texture-ccd0-result.json) · [CCD1 samples](evidence/dpm-gray8-texture-ccd1-result.json).

## Historical multi-QR finder grouping

These finder-group measurements belong to source 6d50ae7c4ea1ff380cabbd0bbb94938eab5fff48; current costs appear separately.

Ordinary multi-QR decoding ranks geometrically consistent finder groups from up to 24 candidates and avoids mixing finders already used by a decoded symbol in the same pass. The permissive and acceptance-predicate searches retain their existing traversal. Payload validation, polarity passes, caller deadlines and recognition allowances are unchanged.

On that core, four generated-image cases pass on .NET 8 and .NET 10. They cover four versions, quarter-turn rotations, repeated payloads and mixed polarity with tile scanning disabled. Repeated-payload assertions require all four decoded versions.

The same frozen four-symbol QR image is decoded by the exact preceding and current qualified cores. Each fixed cache domain uses three warmups and twelve rotated measured iterations. Complete four-payload sets, unexpected output, inconclusive requests, latency and managed allocation remain visible. The complete nineteen-image batch is measured separately.

| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current complete images / 1 | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 1042.67 → 407.72 | 0.391 | 6.13 → 2.49 | 1 → 1 | 0 → 0 | 0 → 0 |
| CCD1 | 1048.91 → 411.50 | 0.392 | 6.23 → 2.49 | 1 → 1 | 0 → 0 | 0 → 0 |

The retail control batches keep their original hints and oracles:

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1058.25 → 1077.68 | 1.018 | 91.27 → 91.26 | 70 → 70 | 0 → 0 | 0 → 0 |
| Positive: 87 | CCD1 | 978.73 → 961.18 | 0.982 | 91.28 → 91.27 | 70 → 70 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD0 | 718.10 → 730.59 | 1.017 | 32.57 → 32.57 | 9 → 9 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD1 | 627.15 → 622.44 | 0.992 | 32.54 → 32.54 | 9 → 9 | 0 → 0 | 0 → 0 |

This optimization groups spatially independent symbols. It does not establish reliable recovery of deliberately overlapping codes that reuse finder patterns. The permissive search remains available. These finite image workloads do not establish overall superiority, portable latency, lower CPU use or lower memory capacity.

[Finder-group proof](evidence/qr-finder-groups-proof.json) · [CCD0 samples](evidence/qr-finder-groups-ccd0-result.json) · [CCD1 samples](evidence/qr-finder-groups-ccd1-result.json).

## Historical recognition deadline polling

These deadline measurements and regression contracts belong to source 023a9382680bc029515093c1de1bc6ed50accc27; current decoding costs appear separately.

Synchronous retail row searches reuse their active deadline, check caller cancellation at every candidate, poll the clock every 32 bounded candidates and check it before returning. Adaptive thresholds and scanline selection reuse their active deadline without reducing checks. PDF417 removes duplicate clock reads already performed at row-reader entry. Search order, hypotheses, fit tolerances and recognition allowances are unchanged.

On that core, two deadline-snapshot tests, all 58 retained scanner-margin contracts and four caller-ownership tests pass on each supported test framework. Captured deadlines still observe clock expiry and later caller cancellation.

The exact preceding qualified binary and current tested core scan the same images. Paired retail batches use 87 positive requests and nine typed negative requests, three warmups and twelve rotated measurements on each fixed cache domain. All measured allocation, latency, correctness and inconclusive samples are retained. Successful warmups are not retained by PowerForge.

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1183.24 → 1079.73 | 0.913 | 91.27 → 91.26 | 70 → 70 | 0 → 0 | 0 → 0 |
| Positive: 87 | CCD1 | 1015.33 → 962.75 | 0.948 | 91.28 → 91.26 | 70 → 70 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD0 | 821.11 → 712.07 | 0.867 | 32.55 → 32.55 | 9 → 9 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD1 | 723.92 → 627.92 | 0.867 | 32.55 → 32.55 | 9 → 9 | 0 → 0 | 0 → 0 |

Fewer clock reads do not establish universal speed, lower CPU use or reliable completion on unseen images. Clock polling remains bounded within synchronous candidate work; the configured allowances are unchanged. Deadline-sensitive requests can still be inconclusive and every completion loss remains visible. These finite workloads do not establish overall superiority.

Allocation and stack profiles use three diagnostic repetitions of the public corpus adapter. Tracing overhead and JIT warmup affect completion. Sampled inclusive stacks overlap and are not CPU utilization or ranked benchmark timings.

[Deadline and regression proof](evidence/decode-deadline-proof.json) · [Positive CCD0 samples](evidence/decode-deadline-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/decode-deadline-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/decode-deadline-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/decode-deadline-negative-ccd1-result.json).

## Historical linear decoder allocations

These allocation measurements and regression contracts belong to source bfd639a91c1603c302e6204e60bdc7f5383951ba; current decoding costs appear separately.

Linear decoding reuses already trimmed module arrays and builds run lengths in one exactly sized array. Format order, image sampling, polarity handling, recognition allowances and payload scoring are unchanged.

On that core, all 58 retained scanner-margin contracts and four caller-ownership tests pass on each supported test framework. Caller modules stay unchanged through padding, inversion, reversal and repeated decoding.

The exact preceding qualified binary and current tested core scan the same images. Paired retail batches use 87 positive requests and nine typed negative requests, three warmups and twelve rotated measurements on each fixed cache domain. All measured allocation, latency, correctness and inconclusive samples are retained. Successful warmups are not retained by PowerForge.

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1093.18 → 1077.98 | 0.986 | 96.20 → 91.27 | 5.1% | 70 → 70 | 0 → 0 | 0 → 0 |
| Positive: 87 | CCD1 | 1013.86 → 1018.98 | 1.005 | 96.20 → 91.27 | 5.1% | 70 → 70 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD0 | 782.44 → 773.64 | 0.989 | 34.97 → 32.55 | 6.9% | 9 → 9 | 0 → 0 | 0 → 0 |
| Negative: 9 | CCD1 | 732.72 → 739.19 | 1.009 | 34.98 → 32.55 | 6.9% | 9 → 9 | 0 → 0 | 0 → 0 |

Managed allocation reduction does not establish lower native memory use, lower CPU use, or universal latency improvement. Deadline-sensitive requests can still be inconclusive; every completion loss remains visible. These images and controls do not establish unseen accuracy or overall superiority.

Allocation and stack profiles use three diagnostic repetitions of the public corpus adapter. Tracing overhead and JIT warmup affect completion. Sampled inclusive stacks overlap and are not CPU utilization or ranked benchmark timings.

[Allocation and regression proof](evidence/decode-allocation-proof.json) · [Positive CCD0 samples](evidence/decode-allocation-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/decode-allocation-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/decode-allocation-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/decode-allocation-negative-ccd1-result.json).

The scanner-repair contracts and costs below belong to source `f42250892ab5234464dbf2a7c926a903f9946ab5`. Current allocation costs and regression coverage appear separately.

## Historical linear barcode margins and neighboring symbols

Cropped and inverted Pharmacodes preserve their complete payloads, including independently rounded and wider bars. Closely spaced Pharmacodes return the two original payloads without a combined extra result. Pixel multi-result APIs and the scanner share margin checks, including transparent margins. Default-format scans also recover inverted single-digit Industrial 2 of 5 symbols.

Fifty-eight targeted contracts use the exact previous qualified binary and current committed core. They cover both axes and polarities, alpha 0/128 margins, compact neighbors with ordinary and wider bars, independently rounded bar widths, and crops with zero or one quiet margin. The current full framework suites include these contracts.

| Targeted contract | Previous passed | Current passed | Cases |
| --- | ---: | ---: | ---: |
| Industrial single-digit symbols | 6 | 8 | 8 |
| Neighboring symbols and transparent margins | 8 | 24 | 24 |
| Pixel-rounded Pharmacode bars | 2 | 4 | 4 |
| Pharmacode bar-width ratios | 3 | 6 | 6 |
| Pharmacode tight and one-sided crops | 8 | 16 | 16 |

Shared scanline sampling also affects retail inputs. The same 87 positive requests and nine typed negative requests are compared with the exact preceding binary, using three warmups and twelve rotated measurements on each fixed cache domain. Every latency, allocation, correctness and inconclusive sample is retained.

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range | Previous → current mean managed MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 1093.05 → 1084.10 | 0.992 | 70 → 70 | 0 → 0 | 0 → 0 | 91.64 → 96.19 |
| Positive: 87 | CCD1 | 1014.52 → 1010.83 | 0.996 | 70 → 70 | 0 → 0 | 0 → 0 | 91.66 → 96.20 |
| Negative: 9 | CCD0 | 878.35 → 841.46 | 0.958 | 9 → 9 | 0 → 0 | 0 → 0 | 34.54 → 34.98 |
| Negative: 9 | CCD1 | 745.40 → 731.96 | 0.982 | 9 → 9 | 0 → 0 | 0 → 0 | 34.54 → 34.98 |

These generated and independent-raster regression controls informed the fixes; they are separate from the frozen photograph ranking. Pharmacode has no guards or mandatory checksum and remains an explicitly requested format. The controls do not establish unseen accuracy or universal rejection of barcode-like textures.

[Regression and binary proof](evidence/scanner-boundary-proof.json) · [Exact-core diagnostics](evidence/scanner-final-core-probes.json) · [Typed retail negatives from this scanner-repair pass](evidence/scanner-retail-negative-diagnostic.json) · [Positive CCD0 samples](evidence/scanner-boundary-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/scanner-boundary-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/scanner-boundary-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/scanner-boundary-negative-ccd1-result.json).

## Historical retail photographs and tile boundary safety

These retained diagnostics and costs belong to source 63677a2c11e9561677736cc001a8bf748ca28670; the current scanner contracts and costs appear separately. The current scanner repair above covers the inverted single-digit and joined-Pharmacode gaps described in this historical packet.

Retail recovery adds two complementary 9/16 local-contrast masks after the original masks establish complete retail guard geometry. The added masks admit only full-length EAN-13/UPC-A geometry; EAN-8 and UPC-E retain their original masks. It reuses the completed middle window without adding buffers. The original eight masks, digit fitting, parity, checksums and nearby-row confirmation remain unchanged.

Eighty-seven unchanged EAN/UPC positive requests from the main corpus. The tilted EAN photograph ean13-4/13.png informed the repair and is regression evidence. Four right-angle rotations in both polarities pass on all supported test frameworks; all eight cases fail with the exact preceding qualified core.

| Engine | Previous correct / 87 | Current correct / 87 | Unexpected images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 69 | 70 | 0 |
| ZXing.Net | 55 | 55 | 2 |
| ZXing-C++ | 54 | 54 | 1 |

Three warmups and twelve grouped rotated measurements per binary and affinity domain. Eighty-seven fixed positive EAN/UPC requests and nine prespecified negative requests (three images with EAN, UPC-A and UPC-E hints) are preloaded as RGBA32 with the unchanged 1500 ms robust allowance. Input loading, hashes and scoring are excluded. Every sample, managed allocation, unexpected payload and inconclusive outcome is retained. Shared-host timings are local evidence.

| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range | Previous → current mean managed MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Positive: 87 | CCD0 | 878.08 → 996.53 | 1.135 | 69 → 70 | 0 → 0 | 0 → 0 | 90.69 → 91.64 |
| Positive: 87 | CCD1 | 786.94 → 848.27 | 1.078 | 69 → 70 | 0 → 0 | 0 → 0 | 90.69 → 91.64 |
| Negative: 9 | CCD0 | 765.76 → 783.86 | 1.024 | 9 → 9 | 0 → 0 | 0 → 0 | 33.99 → 34.54 |
| Negative: 9 | CCD1 | 641.97 → 684.81 | 1.067 | 9 → 9 | 0 → 0 | 0 → 0 | 33.99 → 34.54 |

The 31 distinct negative images are also scanned separately with each retail hint: 93 requests. This diagnostic does not replace all-format negative completion.

| Engine | Completed / 93 | Inconclusive | Unexpected requests |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 93 | 0 | 0 |
| ZXing.Net | 89 | 0 | 4 |
| ZXing-C++ | 93 | 0 | 0 |

Eight default all-format tile-scanner cases prohibit retail payloads on the perforated negative fixture in four rotations and both polarities. The four non-inverted cases return no symbol. Each case also exhausts typed EAN/UPC tile decoding. An unrelated format can reach its recognition limit on a legacy runtime, so safety and completion remain separate. All eight fail with the exact initial candidate before the full-length restriction.

Eight rotations and polarities of falsepositives-2/13.png, outside the unchanged ranking. New retail false acceptance from the initial candidate is removed. The existing tile-boundary check now also rejects Industrial 2 of 5 fragments, removing the pre-existing one-digit outputs exposed by the inverted variants. The current core returns no payload in these eight informed regressions. Full raw old/current outcomes retain completion, format and text; these cases do not establish general false-positive rates.

[Additional negative variants before](evidence/retail-fine-negative-variants-baseline.json) · [Additional negative variants current](evidence/retail-fine-negative-variants-current.json).

This photograph informed the repair and does not establish unseen accuracy or general superiority. Blur, undeclared encoding policies and other remaining misses are retained. Wall-clock deadlines and shared-host contention affect completion.

[Recovery and regression proof](evidence/retail-fine-contrast-proof.json) · [Positive requests](evidence/retail-fine-positive-manifest.json) · [Negative cost requests](evidence/retail-fine-negative-manifest.json) · [Hinted negative outcomes](evidence/retail-fine-negative-diagnostic.json) · [CCD0 positive samples](evidence/retail-fine-positive-ccd0-result.json) · [CCD1 positive samples](evidence/retail-fine-positive-ccd1-result.json) · [CCD0 negative samples](evidence/retail-fine-negative-ccd0-result.json) · [CCD1 negative samples](evidence/retail-fine-negative-ccd1-result.json).

### Complete margins for Industrial 2 of 5 tiles

The unified image scanner extends its existing unchecked-barcode tile-boundary filter to Industrial 2 of 5, for default and explicit-format scans. Both quiet margins must have the background polarity and sufficient width; inverted symbols use their dark background. Full-frame and module decoding retain their behavior, and no minimum payload length is introduced. Tile-margin luminance now uses the same alpha-to-white compositing as decoding, preserving visible quiet zones on transparent backgrounds for Industrial 2 of 5, ITF and Pharmacode.

Four inverted orientations of the perforated texture are scanned with default formats and an Industrial 2 of 5 hint. Before correction, six of the eight checks fail on returned fragments. Six positive contracts preserve both single-digit payloads: typed scans cover both axes and polarities, and default scans cover normal polarity in both axes. Twelve typed transparent-margin cases cover both image axes and alpha 0/128 for these three formats. Both original payloads are restored in every case; Industrial and ITF return exactly those two.

These images informed the repair. They establish these regressions and valid short-symbol behavior, not a general false-positive rate. Unfinished format searches remain inconclusive in the unchanged corpus. Default-format scans still miss the inverted single-digit pair, while explicit Industrial hints recover both payloads; this pre-existing recognition gap remains open. The separate full-frame Pharmacode ambiguity remains open: a neighboring 91/123 pair also produces 2079, before and after the repair.

[Tile boundary and single-digit regression proof](evidence/industrial-tile-boundary-proof.json) · [Tile geometry before correction](evidence/industrial-tile-boundary-diagnosis.json) · [Safety contracts before correction](evidence/industrial-tile-before-proof.json) · [Current short-symbol diagnostic](evidence/industrial-tile-boundary-current-diagnostic.json) · [Transparent-margin contracts before correction](evidence/industrial-tile-alpha-before-proof.json) · [Current transparent-margin diagnostic](evidence/industrial-tile-alpha-current-diagnostic.json).

## Historical QR image cleanup and negative-search cost

These retained filter and whole-image costs belong to source 418fc0728d2d5fa5e29247de4bc5bacfdafd5f70; current whole-image qualification appears separately.

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

Current corpus, alternating negatives, generation, whole-image decoding and paired retail costs are rerun on the frozen tested core. Packed local means preserve preceding DPM retry pixels and order; clipped-window, full-resolution, DPM metadata and cancellation contracts pass on all supported test frameworks. Every measured recognition and completion change is retained. Earlier decoder-specific discovery and cost packets retain their original identities. Hints, oracles and recognition allowances are unchanged. Shared-host contention affects completion and timing. The invalid GS1 oracle and undeclared QR encoding policies remain unchanged. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 50.3%, 39.3%, 35.5%. Before the supplementary negative pass: 33.7%, 21.7%, 16.3%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 17.5% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 20.0% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 22.1% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 22.8% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 56.5% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 23.5% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 19.6% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 18.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 41.7% | 53.6% | 29.8% |
| decoding-ccd0 | 35.8% | 49.5% | 22.1% |
| decoding-ccd1 | 25.0% | 42.6% | 7.3% |
| generation-ccd0 | 29.8% | 39.3% | 20.3% |
| generation-ccd1 | 34.3% | 48.1% | 20.5% |
| negative-rotation-ccd0-0-previous | 17.5% | 32.3% | 3.0% |
| negative-rotation-ccd0-1-current | 20.0% | 34.9% | 5.5% |
| negative-rotation-ccd0-2-current | 22.1% | 36.8% | 7.5% |
| negative-rotation-ccd0-3-previous | 22.8% | 37.6% | 8.1% |
| negative-rotation-ccd1-0-current | 56.5% | 67.6% | 45.5% |
| negative-rotation-ccd1-1-previous | 23.5% | 37.1% | 10.1% |
| negative-rotation-ccd1-2-previous | 19.6% | 34.3% | 4.7% |
| negative-rotation-ccd1-3-current | 18.5% | 32.8% | 4.3% |
| negatives-ccd0 | 23.9% | 37.7% | 10.1% |
| pdf417-supplemental-current | 39.1% | 51.8% | 26.3% |
| pdf417-supplemental-previous | 45.0% | 59.1% | 30.8% |
| scanner-qr-exact-grid-recheck | 16.0% | 30.3% | 2.0% |
| scanner-retail-negative-diagnostic | 26.4% | 38.8% | 14.2% |
| scanner-retail-positive-diagnostic | 34.0% | 46.6% | 21.5% |

## Generation on ccd0

Source `4f3d6312bd989d03d5b475b99b22327d56a53561`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 94.47 | 124.07 | 61.99–128.10 | 3832 |
| Numeric | Modules | QRCoder | 41.55 | 75.83 | 30.46–78.18 | 3760 |
| Numeric | Modules | ZXing-C++ | 44.65 | 78.42 | 37.66–95.19 | 1696 |
| Numeric | Modules | ZXing.Net | 78.69 | 114.42 | 60.81–131.00 | 31304 |
| Numeric | Png | CodeGlyphX | 124.04 | 153.18 | 93.20–163.89 | 40312 |
| Numeric | Png | QRCoder | 126.35 | 146.94 | 109.60–154.59 | 4760 |
| Numeric | Png | ZXing-C++ | 848.12 | 899.01 | 799.09–922.74 | 848310 |
| Numeric | Png | ZXing.Net | 882.44 | 915.12 | 857.30–921.17 | 877614 |
| Unicode | Modules | CodeGlyphX | 1942.61 | 2155.41 | 1794.45–2163.35 | 23872 |
| Unicode | Modules | QRCoder | 1405.71 | 1558.75 | 1310.27–1582.61 | 16728 |
| Unicode | Modules | ZXing-C++ | 1237.56 | 1397.94 | 1108.82–1428.69 | 14648 |
| Unicode | Modules | ZXing.Net | 1915.83 | 2072.71 | 1753.43–2085.81 | 606586 |
| Unicode | Png | CodeGlyphX | 2047.68 | 2176.58 | 1973.33–2250.39 | 274832 |
| Unicode | Png | QRCoder | 1739.58 | 1856.79 | 1676.11–1941.37 | 19624 |
| Unicode | Png | ZXing-C++ | 7854.79 | 8925.24 | 7640.06–9411.98 | 6206718 |
| Unicode | Png | ZXing.Net | 9862.24 | 10915.31 | 9298.00–11099.31 | 6798157 |
| Url | Modules | CodeGlyphX | 968.43 | 1139.06 | 299.57–1157.31 | 5057 |
| Url | Modules | QRCoder | 683.82 | 801.76 | 553.09–810.16 | 5025 |
| Url | Modules | ZXing-C++ | 194.98 | 271.97 | 118.35–281.26 | 2711 |
| Url | Modules | ZXing.Net | 934.62 | 1049.12 | 416.02–1089.83 | 71147 |
| Url | Png | CodeGlyphX | 207.96 | 304.59 | 167.74–345.34 | 60278 |
| Url | Png | QRCoder | 234.09 | 312.88 | 162.10–320.26 | 5816 |
| Url | Png | ZXing-C++ | 1482.19 | 1713.32 | 1374.47–1774.06 | 1309000 |
| Url | Png | ZXing.Net | 1518.77 | 1748.67 | 1475.39–1751.32 | 1377472 |

## Generation on ccd1

Source `4f3d6312bd989d03d5b475b99b22327d56a53561`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 79.14 | 112.07 | 66.96–120.74 | 3832 |
| Numeric | Modules | QRCoder | 43.15 | 73.95 | 29.44–74.03 | 3760 |
| Numeric | Modules | ZXing-C++ | 43.89 | 63.04 | 28.98–67.37 | 1696 |
| Numeric | Modules | ZXing.Net | 59.80 | 92.70 | 38.88–93.83 | 31304 |
| Numeric | Png | CodeGlyphX | 106.99 | 135.32 | 91.03–138.81 | 40312 |
| Numeric | Png | QRCoder | 107.54 | 122.51 | 97.28–122.96 | 4760 |
| Numeric | Png | ZXing-C++ | 819.75 | 902.01 | 744.87–920.33 | 848300 |
| Numeric | Png | ZXing.Net | 837.17 | 895.73 | 791.26–907.88 | 877635 |
| Unicode | Modules | CodeGlyphX | 1768.01 | 1860.64 | 1691.16–1873.34 | 23872 |
| Unicode | Modules | QRCoder | 1322.57 | 1345.42 | 1278.63–1345.51 | 16729 |
| Unicode | Modules | ZXing-C++ | 1160.96 | 1196.30 | 1121.68–1198.77 | 14648 |
| Unicode | Modules | ZXing.Net | 1762.63 | 1850.34 | 1707.94–1870.69 | 606586 |
| Unicode | Png | CodeGlyphX | 1949.33 | 2036.43 | 1839.71–2045.03 | 274832 |
| Unicode | Png | QRCoder | 1714.62 | 1808.77 | 1648.26–1833.27 | 19624 |
| Unicode | Png | ZXing-C++ | 7934.36 | 8240.04 | 7750.98–8508.56 | 6206786 |
| Unicode | Png | ZXing.Net | 8548.69 | 9242.43 | 8172.12–9650.51 | 6798332 |
| Url | Modules | CodeGlyphX | 880.47 | 986.98 | 832.14–998.11 | 5057 |
| Url | Modules | QRCoder | 641.74 | 715.15 | 495.57–741.21 | 5025 |
| Url | Modules | ZXing-C++ | 148.90 | 190.17 | 125.52–197.05 | 2711 |
| Url | Modules | ZXing.Net | 866.37 | 978.56 | 794.72–1005.62 | 71152 |
| Url | Png | CodeGlyphX | 211.70 | 257.70 | 146.79–286.35 | 60294 |
| Url | Png | QRCoder | 186.54 | 239.83 | 161.99–265.82 | 5816 |
| Url | Png | ZXing-C++ | 1426.40 | 1463.16 | 1349.53–1492.59 | 1309105 |
| Url | Png | ZXing.Net | 1507.49 | 1571.79 | 1404.30–1609.93 | 1377543 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 768.56 | 823.97 | 746.55–857.07 | 56.79 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 25.56 | 27.96 | 20.72–29.31 | 2.20 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 191.97 | 199.98 | 178.90–203.15 | 12.97 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 770.34 | 827.75 | 740.69–843.45 | 56.78 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 24.55 | 27.47 | 20.47–28.71 | 2.19 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 187.71 | 520.43 | 179.11–638.44 | 13.04 | 19–19 | 0–0 |

## Paired whole-image decoding cost

The same nineteen preloaded RGBA32 images, options and 1500 ms allowance compare exact preceding and current binaries in separate assembly contexts. Each verified affinity domain uses three warmups and twelve grouped rotated measurements per engine. All measured samples and failed warmups are retained; successful warmups are not retained by PowerForge. Input loading and outcome evaluation are excluded. Shared-host contention limits portable latency and completion claims.

CCD0: the current binary has 0.0–1.0 inconclusive images per measured batch, compared with 0.0–0.0 for the previous binary. All twelve samples remain in the table and raw packet; the latency difference does not establish better completion.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 1452.88 | 1573.86 | 1398.45–1585.12 | 62.06 | 19–19 | 0–0 | 0–0 | 0.998 |
| ccd0 | Candidate | 1450.51 | 1568.04 | 1413.48–1576.88 | 57.74 | 18–19 | 0–0 | 0–1 | 0.998 |
| ccd1 | Baseline | 1395.35 | 1446.64 | 1235.08–1470.65 | 62.04 | 19–19 | 0–0 | 0–0 | 0.989 |
| ccd1 | Candidate | 1380.60 | 1428.14 | 1354.99–1451.89 | 57.73 | 19–19 | 0–0 | 0–0 | 0.989 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
