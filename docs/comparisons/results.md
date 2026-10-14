# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

Host CPU sampled before the current corpus and timing passes ranged from 14.1% to 100.0%. These initial readings do not describe load throughout each operation. Shared-host contention limits timing comparisons and can change completion under the fixed deadline.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 243/267 | 0 | 3 | 0 | 1 |
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
| Gs1DataBarOmnidirectional | 12 | 4 | 10 | 8 | 8 |
| Itf | 12 | 10 | 12 | 12 | 12 |
| MaxiCode | 6 | 0 | 6 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 34 | 34 | 34 |
| UpcA | 36 | 0 | 27 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 243/267 correct positive images, compared with 243/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

## Additional Code128 and DataBar photographs

Every eligible image with a UTF-8 text oracle in upstream code128-2 and rss14-2, excluding the unchanged main corpus IDs, sorted by SHA256 of source path. Selected before decoding; all misses retained. Separate diagnostic qualification set, not an independent post-development accuracy estimate.

This externally sourced set guided development. It is a diagnostic and regression set, not a held-out accuracy estimate. Its 52 images remain separate from the 267-positive headline ranking. Original oracles, all misses, inconclusive scans and unexpected outputs are retained. Unexpected counts are positive-image payload mismatches against the locked text oracles; they do not establish a negative-image false-positive rate. QRCoder generates QR symbols and is excluded from decoding comparisons.

| Family | Reader | Previous correct | Current correct | Images | Current unexpected | Current inconclusive |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Code128 | CodeGlyphX | 34 | 34 | 34 | 0 | 0 |
| Code128 | ZXing.Net | 34 | 34 | 34 | 0 | 0 |
| Code128 | ZXing-C++ | 34 | 34 | 34 | 0 | 0 |
| DataBar | CodeGlyphX | 6 | 8 | 18 | 0 | 0 |
| DataBar | ZXing.Net | 5 | 5 | 18 | 0 | 0 |
| DataBar | ZXing-C++ | 8 | 8 | 18 | 1 | 0 |
| Combined | CodeGlyphX | 40 | 42 | 52 | 0 | 0 |
| Combined | ZXing.Net | 39 | 39 | 52 | 0 | 0 |
| Combined | ZXing-C++ | 42 | 42 | 52 | 1 | 0 |

CodeGlyphX gains 2 images, loses 0, and adds unexpected outputs on 0 images in this set.

[Inputs](evidence/databar-separator-additional-inputs.json) · [Previous outcomes](evidence/databar-separator-additional-before.json) · [Current outcomes](evidence/databar-separator-additional-current.json).

## Stacked DataBar separator geometry and complete scan costs



Stacked DataBar image decoding fits a tilted separator centerline from transitions between checksum-verified data rows. Consistent thickness, distributed support, a three-quarter centerline consensus and the complete encoded separator pattern validate the fallback. Physical layout, payload checks and the existing deadline remain enforced. No public option or runtime dependency is added.

Twenty-two separator geometry cases pass on each framework. Fifteen of these 22 cases fail against the exact preceding core. Independent stacked photographs pass in all four rotations and both polarities; synthetic simple and omnidirectional layouts preserve their payloads and reject the wrong layout and missing separators. The exact core and test assembly pass 147 native ARM64 checks, plus 20 Code128 pixel-API calls. The separate 52-photo development set is excluded from the headline 267-image ranking.

The separate traced nine-image diagnostic records 27/27 to 26/27 correct rejections after priming, with mean managed allocation 19.16 to 19.04 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.


- whole on CCD0: 228 to 227 correct requests out of 228 across twelve measured calls.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 927.48 → 920.49 | 0.992 | 33.64 → 34.67 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 906.71 → 921.41 | 1.016 | 33.94 → 34.07 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 547.80 → 544.98 | 0.995 | 17.99 → 17.99 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 542.68 → 546.90 | 1.008 | 17.99 → 17.99 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 645.72 → 753.65 | 1.167 | 27.90 → 27.80 | 19 → 18–19 | 0 → 0–1 | 0 → 0 |
| whole | 19 | CCD1 | 567.45 → 568.57 | 1.002 | 27.18 → 27.18 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 892.79 → 891.22 | 0.998 | 91.08 → 91.07 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 705.66 → 692.19 | 0.981 | 91.08 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 533.78 → 509.19 | 0.954 | 32.64 → 32.64 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 491.17 → 443.50 | 0.903 | 32.64 → 32.64 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 31.80 → 29.58 | 0.930 | 13.29 → 13.29 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 22.06 → 22.45 | 1.018 | 13.28 → 13.28 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 353.52 → 358.43 | 1.014 | 54.66 → 54.68 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 416.99 → 425.62 | 1.021 | 54.65 → 54.66 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-positive | 30 | CCD0 | 58.57 → 59.40 | 1.014 | 31.86 → 31.86 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-positive | 30 | CCD1 | 71.52 → 70.73 | 0.989 | 31.84 → 31.85 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-negative | 31 | CCD0 | 868.03 → 882.48 | 1.017 | 4.60 → 4.60 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-negative | 31 | CCD1 | 748.23 → 722.83 | 0.966 | 4.62 → 4.62 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Stacked DataBar separator geometry and complete scan costs proof](evidence/databar-separator-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Code128 photo recovery and complete scan costs

These measurements retain their original source 6c0c27e55b330f384e3abc278aa693e59a9854fa.

Code128 photo decoding locates complete frames in both physical directions and recovers bounded neighboring rows with complementary threshold cuts. Recovery requires three adjacent rows with the same checksum-verified payload, type and reader-initialization state. Existing pixel APIs use this internal pass without a new public option or runtime dependency.

Twenty photograph and row-recovery regression cases pass on each framework. Fifteen of these 20 cases fail against the exact preceding core. Public checks cover transformed images, three adjacent matching rows, checksum rejection and reader-initialization identity. The exact core and test assembly pass 125 native ARM64 checks. The separate 52-photo development set is reported above and is excluded from the headline 267-image ranking.

The separate traced nine-image diagnostic records 27/27 to 27/27 correct rejections after priming, with mean managed allocation 19.04 to 19.14 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 989.58 → 993.36 | 1.004 | 33.21 → 32.62 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 926.70 → 906.88 | 0.979 | 33.89 → 34.11 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 578.12 → 572.38 | 0.990 | 17.93 → 18.00 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 491.62 → 499.21 | 1.015 | 17.93 → 18.00 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 771.91 → 777.46 | 1.007 | 26.98 → 27.19 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 683.40 → 680.67 | 0.996 | 27.00 → 27.20 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 998.72 → 919.47 | 0.921 | 91.09 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 776.42 → 724.69 | 0.933 | 91.08 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 559.65 → 559.00 | 0.999 | 32.64 → 32.64 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 425.89 → 440.98 | 1.035 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 44.11 → 41.36 | 0.938 | 13.30 → 13.29 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 30.12 → 27.99 | 0.929 | 13.29 → 13.28 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 404.83 → 412.48 | 1.019 | 54.69 → 54.66 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 355.33 → 352.39 | 0.992 | 54.67 → 54.64 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-positive | 30 | CCD0 | 67.47 → 60.53 | 0.897 | 31.86 → 31.85 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-positive | 30 | CCD1 | 70.91 → 74.13 | 1.045 | 31.86 → 31.85 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-negative | 31 | CCD0 | 708.18 → 723.17 | 1.021 | 4.62 → 4.61 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-negative | 31 | CCD1 | 659.40 → 638.73 | 0.969 | 4.61 → 4.61 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Code128 photo recovery proof](evidence/code128-photo-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical DPM preprocessing storage and scan costs

These measurements retain their original source 490c16e586d21be460d4c04494c7580cc078327c.

DPM preprocessing reuses private gray, local mean, mask and morphology storage for each retry enumeration. Its returned images remain independently owned. QR and DPM share a rolling square filter with explicit clipped and zero-outside erosion policies. Retry order, thresholds, attempts, pixels, cancellation and output ownership retain their existing behavior.

All 19,240 actual square-filter cases preserve the two border policies. Across 3,414 retry cases, all 14,886 returned images match the preceding binary, including retained and mutated outputs. Seventeen cancellation, early-disposal and consumer-exception cases preserve ownership. Fifteen regression contracts pass on each framework; the exact core and test assembly pass 105 native ARM64 checks. These private preprocessing checks do not establish image recognition or comparative speed.

The separate traced nine-image diagnostic records 27/27 to 27/27 correct rejections after priming, with mean managed allocation 23.67 to 19.05 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 893.99 → 900.43 | 1.007 | 39.01 → 33.98 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 797.28 → 813.54 | 1.020 | 39.62 → 33.92 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 614.37 → 626.72 | 1.020 | 23.45 → 17.93 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 469.94 → 466.24 | 0.992 | 23.44 → 17.92 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 767.88 → 779.92 | 1.016 | 32.49 → 26.98 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 718.71 → 731.77 | 1.018 | 32.50 → 26.98 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 1060.15 → 1212.86 | 1.144 | 91.08 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 827.86 → 807.34 | 0.975 | 91.08 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 581.97 → 581.26 | 0.999 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 424.10 → 430.84 | 1.016 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 34.71 → 30.63 | 0.882 | 13.31 → 13.30 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 29.66 → 27.18 | 0.916 | 13.28 → 13.29 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 471.27 → 479.34 | 1.017 | 54.68 → 54.69 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 551.97 → 520.29 | 0.943 | 54.68 → 54.68 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-positive | 30 | CCD0 | 92.02 → 93.10 | 1.012 | 31.87 → 31.87 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-positive | 30 | CCD1 | 61.55 → 59.34 | 0.964 | 31.86 → 31.86 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-negative | 31 | CCD0 | 876.86 → 823.49 | 0.939 | 4.62 → 4.62 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-negative | 31 | CCD1 | 899.77 → 914.86 | 1.017 | 4.60 → 4.60 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[DPM workspace proof](evidence/dpm-workspace-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Shared linear run storage and scan costs

These measurements retain their original source 38c89640464483414c562f46965781a549b8ac76.

Linear module decoding computes run widths lazily once per immutable input and shares its initialized read-only span across format attempts. Pooled storage belongs to the synchronous call; reversed and nested inputs own independent workspaces. Telepen normalization preserves its separate padded copy. Format order, transforms, checksums and cancellation retain their existing behavior.

All 133,203 actual primitive run cases preserve widths and nested/repeated ownership. All 22,464 public module decisions match the preceding qualified binary across hints, transforms, scaling, truncations and checksum policies. Seven shared-workspace regression contracts pass on each framework; the exact core and test assembly pass 90 native ARM64 checks. Private primitive and public module evidence do not establish image recognition or comparative speed.

The separate traced nine-image diagnostic records 18/27 to 27/27 correct rejections after priming, with mean managed allocation 31.05 to 23.65 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.


- fabric on CCD1: 3 to 1 correct requests out of 12 across twelve measured calls.
- texture on CCD1: 12 to 11 correct requests out of 12 across twelve measured calls.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 929.83 → 935.82 | 1.006 | 49.92 → 42.75 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 993.57 → 992.39 | 0.999 | 49.26 → 38.94 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 747.21 → 703.64 | 0.942 | 27.09 → 24.24 | 0–1 → 1 | 0–1 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 697.56 → 632.55 | 0.907 | 26.53 → 23.73 | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| whole | 19 | CCD0 | 831.23 → 846.77 | 1.019 | 35.46 → 32.49 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 800.50 → 878.30 | 1.097 | 35.47 → 32.51 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 891.89 → 900.13 | 1.009 | 91.09 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 916.01 → 847.89 | 0.926 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 628.84 → 616.50 | 0.980 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 658.22 → 764.90 | 1.162 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 46.90 → 40.95 | 0.873 | 13.31 → 13.31 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 60.84 → 54.63 | 0.898 | 13.30 → 13.29 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 570.36 → 637.39 | 1.118 | 54.71 → 54.71 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 595.72 → 623.34 | 1.046 | 54.71 → 54.71 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-positive | 30 | CCD0 | 88.61 → 87.82 | 0.991 | 31.90 → 31.89 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-positive | 30 | CCD1 | 97.00 → 105.07 | 1.083 | 31.89 → 31.89 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-negative | 31 | CCD0 | 753.15 → 747.36 | 0.992 | 4.62 → 4.62 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-negative | 31 | CCD1 | 903.57 → 834.98 | 0.924 | 4.61 → 4.61 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Shared linear run proof](evidence/linear-run-workspace-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Expanded Stacked decoding workspace and scan costs

These measurements retain their original source 8c115dc8584e042586c1a941ea8acfca7a84f5be.

Expanded Stacked image scanning reuses run and start lists and a pooled offset buffer within each call. A bounded 235-width stack workspace holds candidate rows; only retained observations copy their completed widths. The shared linear/stacked character fitter uses eight stack elements and preserves fitting arithmetic and accumulated error. Finder matching consumes existing pattern spans. Candidate order, guards, row ownership, assembly and deadline polling preserve their existing rules.

All 24,616 actual character-fitting outcomes match the preceding qualified binary, including success, appended widths and exact fit bits. The image-row collector preserves all 45,113 ordered observations across 244 rotation/polarity variants of 61 frozen images and 564,768 calls per binary, including dirty pooled storage. Five public workspace contracts pass on each framework; the exact core and test assembly pass 83 native ARM64 checks.

The separate traced nine-image diagnostic records 27/27 to 25/27 correct rejections after priming, with mean managed allocation 38.27 to 33.78 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 895.44 → 935.50 | 1.045 | 59.29 → 50.38 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 1116.75 → 992.29 | 0.889 | 57.53 → 48.33 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 636.40 → 710.38 | 1.116 | 34.24 → 27.09 | 0–1 → 1 | 0–1 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 479.12 → 460.15 | 0.960 | 33.93 → 27.18 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 1049.07 → 969.24 | 0.924 | 44.49 → 36.66 | 18–19 → 18–19 | 0–1 → 0–1 | 0 → 0 |
| whole | 19 | CCD1 | 999.38 → 900.46 | 0.901 | 44.58 → 36.57 | 18–19 → 19 | 0–1 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 2438.33 → 1937.17 | 0.794 | 91.11 → 91.10 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 925.73 → 956.14 | 1.033 | 91.11 → 91.12 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 559.58 → 554.24 | 0.990 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 829.82 → 757.72 | 0.913 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 59.90 → 51.38 | 0.858 | 13.31 → 13.31 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 36.04 → 35.16 | 0.976 | 13.30 → 13.30 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 533.35 → 524.60 | 0.984 | 54.73 → 54.72 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 434.74 → 435.26 | 1.001 | 54.70 → 54.69 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-positive | 30 | CCD0 | 89.97 → 76.86 | 0.854 | 53.44 → 31.86 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-positive | 30 | CCD1 | 81.62 → 66.94 | 0.820 | 53.46 → 31.88 | 29 → 29 | 0 → 0 | 1 → 1 |
| expanded-negative | 31 | CCD0 | 1033.40 → 986.47 | 0.955 | 126.04 → 4.62 | 31 → 31 | 0 → 0 | 0 → 0 |
| expanded-negative | 31 | CCD1 | 763.84 → 739.32 | 0.968 | 126.04 → 4.62 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Expanded Stacked workspace proof](evidence/databar-expanded-workspace-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical DataBar-14 decoding workspace and scan costs

These measurements retain their original source a94fee87b496128712dd84eeee5cb6b8771d3a96.

DataBar-14 exact decoding uses bounded module runs and stack workspaces for physical widths, checksum values and stacked assembly. Stacked image scanning reuses one run list and pooled offset buffer within each call, initializes every active prefix, and keeps retained row widths independently owned. Guards, finder matching, character groups, checksums, candidate order and deadline polling preserve their existing rules.

All 36,158 public DataBar-14 module, Barcode1D, stacked BitMatrix and compatibility-alias outcomes match the preceding qualified binary. The actual image-row collector preserves all 1,833 ordered observations across 172 rotation/polarity variants of 43 frozen images and 232,818 calls per binary, including dirty pooled storage. Eighteen workspace contracts pass on each framework; the exact managed core and test assembly pass 78 native ARM64 checks.

The separate traced nine-image diagnostic records 25/27 to 15/27 correct rejections after priming, with mean managed allocation 42.70 to 36.53 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.


- fabric on CCD0: 4 to 0 correct requests out of 12 across twelve measured calls.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 1019.77 → 1056.47 | 1.036 | 62.15 → 58.34 | 0–1 → 0 | 0–1 → 1 | 0 → 0 |
| fabric | 1 | CCD1 | 933.58 → 923.48 | 0.989 | 63.26 → 59.62 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 555.87 → 553.29 | 0.995 | 35.86 → 33.66 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 622.89 → 637.99 | 1.024 | 36.25 → 34.15 | 0–1 → 1 | 0–1 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 848.52 → 787.00 | 0.928 | 47.37 → 44.93 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 736.33 → 742.00 | 1.008 | 47.17 → 44.84 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 937.33 → 913.63 | 0.975 | 91.11 → 91.10 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 904.42 → 889.37 | 0.983 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 578.14 → 565.98 | 0.979 | 32.66 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 642.56 → 615.36 | 0.958 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 41.41 → 36.51 | 0.882 | 16.16 → 13.31 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 37.16 → 43.13 | 1.160 | 16.17 → 13.32 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 542.22 → 548.98 | 1.012 | 108.36 → 54.71 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 549.65 → 500.30 | 0.910 | 108.37 → 54.71 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[DataBar-14 workspace proof](evidence/databar14-workspace-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical DataBar bounded module extraction and scan costs

These measurements retain their original source 307101715324909086610d7765637117df310c68.

DataBar Limited and Expanded read exact module runs into bounded stack buffers before validating symbol widths. Expanded reuses the collected widths for both light-guard restoration retries and chooses its unique character count without a temporary candidate list. Physical stacked rows retain their existing assembly and checksum rules.

All 16,985 public module-decoder success and text outcomes match the preceding qualified binary, including valid, damaged, trimmed, padded and malformed inputs. Nineteen contracts pass on each framework, covering every supported Expanded character count from 4 through 22, independent Limited vectors, maximum stacked symbols and initialized buffer boundaries. The exact managed binary and test assembly pass 60 native ARM64 checks, including those nineteen DataBar contracts.

The separate traced nine-image diagnostic records 17/27 to 24/27 correct rejections after priming, with mean managed allocation 48.71 to 41.96 MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 928.00 → 900.03 | 0.970 | 76.44 → 63.11 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 913.53 → 906.23 | 0.992 | 76.92 → 63.16 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 538.34 → 543.75 | 1.010 | 40.31 → 36.57 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 547.21 → 533.23 | 0.974 | 40.59 → 36.37 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 829.64 → 807.65 | 0.973 | 51.55 → 47.63 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 766.19 → 773.17 | 1.009 | 51.61 → 47.27 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 928.04 → 905.79 | 0.976 | 91.11 → 91.10 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 936.74 → 947.38 | 1.011 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 605.22 → 593.76 | 0.981 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 536.69 → 533.40 | 0.994 | 32.67 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 40.83 → 47.00 | 1.151 | 16.17 → 16.16 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 40.85 → 34.35 | 0.841 | 16.18 → 16.17 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 582.55 → 578.24 | 0.993 | 108.37 → 108.37 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 518.11 → 514.63 | 0.993 | 108.38 → 108.38 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[DataBar module-run proof](evidence/databar-module-runs-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Image component coordinates and scan costs

These measurements retain their original source 2e2915a4b218c89dba8c126a1e32dec410c4958e.

Image component searches prepare one exact integer reciprocal for the image width. QR traversal and the shared Data Matrix/Aztec visitor use multiplication and one remainder correction for row and column coordinates. The queues, four-connected neighbours, row-major seeds, component filters, result ordering, tied extrema and deadline polling keep their existing behavior.

The isolated QR component traversal median falls by 4.3% to 18.0% across the two processor domains. Complete-scan costs and deadline outcomes remain separate below. Independent coordinate and graph references preserve pixel order, bounds and area, and the exact binary passes 41 native ARM64 functional checks.

Eight exploratory public-scan pairs retain every priming and measured per-image result in a separate table, including all slower passes and incomplete searches.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Each public-scan table retains one decoder session per binary. Earlier fresh-context costs remain separately identified in the historical context inventory.


- Final texture on CCD0: 12 to 11 correct requests out of 12 across twelve measured calls.
- Final whole on CCD0: 227 to 226 correct requests out of 228 across twelve measured calls.
- Final whole on CCD1: 228 to 227 correct requests out of 228 across twelve measured calls.
- Exploratory negative15 on CCD1: 12 to 11 correct requests out of 12 across twelve measured calls.
- Exploratory texture on CCD0: 12 to 10 correct requests out of 12 across twelve measured calls.
- Exploratory timeout9 on CCD1: 108 to 107 correct requests out of 108 across twelve measured calls.

### Component collection only

Actual QR component collection only. Identical explicitly prepared grayscale inputs, two fixed thresholds and both polarities, unbounded budget. Loading, preparation, outcome evaluation and pooled-result disposal are outside timing. This is separate from complete public scans and competitive rankings.

| Domain | Input variants | Identical components | Previous → current median ms | Current / previous | Previous → current mean managed MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 16 | 163 | 19.00 → 15.57 | 0.820 | 2.15 → 2.15 |
| CCD1 | 16 | 163 | 20.89 → 19.99 | 0.957 | 2.15 → 2.15 |

### Complete scans

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 987.71 → 1027.82 | 1.041 | 74.78 → 74.61 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 819.80 → 841.15 | 1.026 | 76.48 → 76.62 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 730.57 → 766.42 | 1.049 | 40.61 → 39.68 | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| texture | 1 | CCD1 | 664.89 → 642.45 | 0.966 | 40.36 → 40.90 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 1035.70 → 890.48 | 0.860 | 51.26 → 51.83 | 18–19 → 18–19 | 0–1 → 0–1 | 0 → 0 |
| whole | 19 | CCD1 | 892.34 → 860.87 | 0.965 | 51.49 → 51.45 | 19 → 18–19 | 0 → 0–1 | 0 → 0 |
| retail-positive | 87 | CCD0 | 907.30 → 920.32 | 1.014 | 91.09 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 783.64 → 780.56 | 0.996 | 91.08 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 666.27 → 615.95 | 0.924 | 32.67 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 568.36 → 534.58 | 0.941 | 32.66 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 37.84 → 38.65 | 1.022 | 16.14 → 16.15 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 45.78 → 55.06 | 1.203 | 16.15 → 16.15 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 607.82 → 603.35 | 0.993 | 108.38 → 108.38 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 545.87 → 531.01 | 0.973 | 108.36 → 108.36 | 31 → 31 | 0 → 0 | 0 → 0 |

### Exploratory complete scans

Every exploratory public-scan pass remains separate from the final table, with all priming and measured outcomes retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| qr | 1 | CCD0 | 129.01 → 130.97 | 1.015 | 2.64 → 2.64 | 1 → 1 | 0 → 0 | 0 → 0 |
| qr | 1 | CCD1 | 131.77 → 119.59 | 0.908 | 2.63 → 2.64 | 1 → 1 | 0 → 0 | 0 → 0 |
| negative15 | 1 | CCD0 | 1009.48 → 1027.80 | 1.018 | 73.67 → 74.07 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| negative15 | 1 | CCD1 | 775.86 → 774.60 | 0.998 | 76.96 → 76.57 | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 587.55 → 574.03 | 0.977 | 40.22 → 40.45 | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| texture | 1 | CCD1 | 457.77 → 446.31 | 0.975 | 40.51 → 40.21 | 1 → 1 | 0 → 0 | 0 → 0 |
| timeout9 | 9 | CCD0 | 6766.32 → 5991.67 | 0.886 | 487.45 → 489.66 | 5–9 → 6–9 | 0–4 → 0–3 | 0 → 0 |
| timeout9 | 9 | CCD1 | 4603.39 → 4721.36 | 1.026 | 491.00 → 490.35 | 9 → 8–9 | 0 → 0–1 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Component coordinate proof](evidence/component-coordinates-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical DataBar recovery buffer allocation and costs

These measurements retain their original source 5a33118fd1ef6e6381dc4bd86007e65c1d161511.

DataBar photo recovery reuses one temporary run list per call and a lazily rented integer offset buffer. Every active offset prefix is initialized before reads; growth returns the previous lease and every exit returns the current lease. Accepted observations own their module arrays. All fifteen cutoffs, candidate order, character fits, three-row corroboration, conflict rejection and original deadlines retain their behavior.

Managed allocation falls by 28.2% to 29.6% on the unchanged DataBar workloads. Median elapsed time ranges from 0.918 to 1.355 times the preceding binary across the two processor domains; some passes are slower. The actual recovery collector matches all 732 located candidates across 344 variants, including dirty pooled storage, rotation, inversion, packed RGBA and padded BGRA. Existing DataBar regressions pass on all three frameworks, and the exact binary passes 32 native ARM64 public decoding checks.

Four exploratory DataBar pairs remain separately retained from the final table. Every priming and measured per-image outcome is included. Five traced negative batches per binary provide allocation diagnostics; their overlapping stack samples do not establish CPU use or competitive latency.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 721.76 → 710.42 | 0.984 | 78.66 → 76.87 | 1 → 1 | 0 → 0 | 0 → 0 |
| fabric | 1 | CCD1 | 685.39 → 681.18 | 0.994 | 78.75 → 76.50 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD0 | 455.92 → 447.80 | 0.982 | 41.86 → 39.95 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 432.14 → 428.32 | 0.991 | 42.41 → 40.23 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 558.08 → 562.32 | 1.008 | 53.39 → 51.24 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 621.64 → 606.16 | 0.975 | 53.86 → 51.43 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 744.94 → 796.40 | 1.069 | 91.08 → 91.06 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 726.94 → 738.48 | 1.016 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 430.35 → 432.57 | 1.005 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 474.25 → 476.39 | 1.005 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 30.52 → 41.35 | 1.355 | 22.95 → 16.17 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 29.98 → 27.52 | 0.918 | 22.94 → 16.15 | 10 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 376.98 → 385.46 | 1.023 | 151.04 → 108.39 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 419.50 → 405.63 | 0.967 | 151.05 → 108.38 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[DataBar buffer reuse proof](evidence/databar-buffers-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Corroborated DataBar photo recovery and costs

These measurements retain their original source 217a30ffb28a25aadec49a55a9ca2a0c797131e5.

Photographed DataBar-14 rows can recover an invalid independently rounded character through a bounded one-module transfer that preserves the character sum and measured residual limit. Complete guards, both finders, all character constraints and the symbol checksum must validate. The same payload and aligned physical bounds must appear in three adjacent rows. Different payloads in overlapping three-row windows reject. Ordinary decoding and format priority retain their earlier behavior.

Positive CodeGlyphX: 9 → 10/12 correct, 0 current unexpected images. Positive ZXing.Net: 8 → 8/12 correct, 0 current unexpected images. Positive ZXing-C++: 8 → 8/12 correct, 0 current unexpected images. Negative CodeGlyphX: 31 → 31/31 correct, 0 current unexpected images. Negative ZXing.Net: 31 → 31/31 correct, 0 current unexpected images. Negative ZXing-C++: 31 → 31/31 correct, 0 current unexpected images. Eleven regression cases pass on each of .NET 10, .NET 8 and .NET Framework 4.7.2, with span paths exercised on modern targets. The exact managed binary passes 32 native ARM64 public decoding checks; timing remains specific to Windows.





One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

These tables retain one decoder session per binary. Earlier fresh-context measurements remain separately recorded in the historical context inventory and cannot be compared directly with these reused sessions.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 917.72 → 937.29 | 1.021 | 75.78 → 78.32 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 874.38 → 818.55 | 0.936 | 76.58 → 78.52 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 549.57 → 567.45 | 1.033 | 40.41 → 42.03 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 565.47 → 559.22 | 0.989 | 40.10 → 42.09 | 0–1 → 1 | 0–1 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 716.73 → 725.61 | 1.012 | 51.56 → 54.08 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 692.08 → 695.03 | 1.004 | 51.41 → 53.89 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 886.64 → 858.56 | 0.968 | 91.07 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 869.90 → 863.29 | 0.992 | 91.09 → 91.10 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 598.85 → 566.16 | 0.945 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 520.38 → 504.38 | 0.969 | 32.64 → 32.64 | 9 → 9 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD0 | 33.55 → 48.80 | 1.454 | 16.07 → 22.94 | 9 → 10 | 0 → 0 | 0 → 0 |
| databar-positive | 12 | CCD1 | 28.44 → 34.85 | 1.225 | 16.09 → 22.96 | 9 → 10 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD0 | 427.60 → 546.16 | 1.277 | 107.30 → 151.05 | 31 → 31 | 0 → 0 | 0 → 0 |
| databar-negative | 31 | CCD1 | 431.75 → 561.48 | 1.300 | 107.29 → 151.03 | 31 → 31 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[DataBar photo recovery proof](evidence/databar-character-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical QR component mask and costs

These measurements retain their original source 2178357d3a5faefd626d06cb0c48a2caedd85e86.

QR component foreground classification on .NET 8 and later uses hardware vectors when available, with scalar tails and fallback. Every logical pixel receives the same zero or one value for its unsigned threshold comparison and polarity. Unused pooled storage is untouched. Row-level live budget checks, four-connected traversal, row-major seed order, component filtering and resource return paths retain their previous behavior.

The actual component mask matches independent byte selection in 57,344 cases and 72,810,496 pixel classifications per execution mode. Windows covers 256-bit vectors, 128-bit vectors and scalar fallback; the same clean binary also passes native Apple M4 ARM64 with 128-bit vectors. Checks cover every global byte threshold, varied adaptive maps, both polarities, vector boundary widths, solid/patterned/random images and unused pooled-array tails. Separately, both exact binaries match an independent four-connected reference across 288 cases, 12,279,996 pixel classifications and 133 accepted components, including bounds, areas and order. Pre-cancelled and already-expired mask calls stop before writes; row-level mid-scan polling is supported by source inspection, without a response-time guarantee.

Eight exploratory pairs on this same production binary remain separate from the final table. They include QR, texture, whole-image and fabric workloads, with every completion loss retained. Native ARM64 results prove pixel decisions only; the timing tables describe the Windows host. The Runtime field in the earlier foreground-bounds receipt and initial mask receipt contains dotnet --version output, which is the SDK version. Their selected framework runtime was not recorded. The current mask check was rerun with an explicit framework version and roll-forward disabled; the decision proof is unchanged. Original receipts remain retained.

Final fabric on CCD1: 10 → 8 correct image requests out of 12 across twelve measured calls. Final texture on CCD0: 12 → 11 correct image requests out of 12 across twelve measured calls.

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 911.50 → 891.38 | 0.978 | 76.08 → 76.06 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 876.92 → 916.04 | 1.045 | 76.22 → 76.23 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 617.78 → 611.93 | 0.991 | 40.61 → 40.22 | 1 → 0–1 | 0 → 0–1 | 0 → 0 |
| texture | 1 | CCD1 | 493.24 → 500.02 | 1.014 | 40.48 → 40.61 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 750.81 → 743.97 | 0.991 | 51.42 → 51.48 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 697.38 → 670.46 | 0.961 | 51.64 → 51.42 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 1100.73 → 1074.87 | 0.977 | 91.12 → 91.11 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 867.45 → 855.34 | 0.986 | 91.09 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 602.72 → 598.98 | 0.994 | 32.68 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 593.29 → 611.19 | 1.030 | 32.65 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[QR component mask proof](evidence/qr-component-mask-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical QR foreground bounds and costs

These measurements retain their original source 8178c13b0a12445b83cc014c9abc7f92bd4d8900.

QR pixel decoding on .NET 8 and later scans foreground bounds with hardware vectors when available and a scalar fallback. Threshold equality, adaptive maps, both polarities and cropped regions preserve the same selected pixels. Cancellation and deadline checks retain the previous global 256-pixel polling cadence. Trimming, expansion, version/phase sampling, payload decoding and diagnostics remain unchanged.

Independent selected-pixel extrema match the actual foreground scan in 286,720 cases and 144,252,928 pixel classifications per execution mode. Windows checks cover 256-bit vectors, 128-bit vectors and scalar execution. The same qualified managed binaries pass on native Apple M4 ARM64 with 128-bit vectors. Cases cover every byte threshold, global/adaptive maps, both polarities, vector boundary widths, cropped and single-row/column regions, patterned/random pixels and unused pooled-array tails. Pre-cancelled and already-expired searches stop. Mid-scan polling equivalence is supported by source inspection, without a response-time guarantee. Framework tests protect the public QR recognition paths.

Eight exploratory pairs on this same production binary remain separate from the final table. They include QR, texture, whole-image and fabric workloads, with every completion loss retained. Native ARM64 results prove pixel decisions only; the timing tables describe the Windows host.

Exploratory fabric on CCD0: 7 → 5 correct image requests out of 12 across twelve measured calls.

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 767.45 → 740.89 | 0.965 | 76.77 → 76.98 | 1 → 1 | 0 → 0 | 0 → 0 |
| fabric | 1 | CCD1 | 752.82 → 708.17 | 0.941 | 75.55 → 75.55 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD0 | 558.24 → 504.13 | 0.903 | 39.47 → 39.47 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 449.02 → 426.45 | 0.950 | 39.45 → 39.45 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 841.03 → 747.97 | 0.889 | 50.07 → 50.07 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 680.62 → 648.53 | 0.953 | 50.07 → 50.08 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 746.72 → 763.96 | 1.023 | 91.07 → 91.07 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 681.74 → 704.52 | 1.033 | 91.08 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 447.95 → 458.14 | 1.023 | 32.64 → 32.64 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 429.73 → 436.70 | 1.016 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[QR foreground bounds proof](evidence/qr-foreground-bounds-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical QR cancellation and component costs

These measurements retain their original source a83e44aad8767c51df3f7fe40890c3e814473f17.

QR recognition polls live caller cancellation and the captured enclosing deadline even when its local timeout is disabled. Component traversal reads the existing budget directly at its previous polling points. Grayscale conversion distinguishes a budget stop from invalid input. Pixel classification, component bounds and order, accepted payloads, hints and recognition allowances remain unchanged.

Actual preceding and current component searches match an independent four-connected traversal across 288 image/polarity/threshold cases and 12,279,996 pixel classifications, including 133 accepted components and their bounds, areas and order. Cancelled and expired traversal stops are checked separately. Public untimed cancellation diagnostics and a real enclosing-deadline reproduction are retained without a timing threshold. Full tests cover nested deadlines and an independently retained partial multi-symbol result after cancellation.

Six exploratory pairs from the earlier candidate remain separate from the table below. They include completion losses and a whole-image latency increase. Public cancellation response samples are diagnostic evidence, with no timing threshold or portable latency claim.

Final fabric on CCD0: 11 → 10 correct image requests out of 12 across twelve measured calls. Exploratory fabric on CCD0: 11 → 9 correct image requests out of 12 across twelve measured calls. Exploratory fabric on CCD1: 12 → 11 correct image requests out of 12 across twelve measured calls.

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 799.46 → 813.06 | 1.017 | 76.25 → 76.37 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 738.33 → 736.62 | 0.998 | 76.13 → 76.66 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 546.64 → 543.79 | 0.995 | 40.40 → 40.23 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 468.76 → 453.37 | 0.967 | 40.29 → 40.35 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 647.28 → 629.37 | 0.972 | 51.57 → 51.59 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 630.31 → 626.16 | 0.993 | 51.86 → 51.42 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 911.02 → 947.04 | 1.040 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 758.15 → 846.25 | 1.116 | 91.10 → 91.11 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 547.49 → 540.93 | 0.988 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 508.48 → 512.89 | 1.009 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[QR budget and component proof](evidence/qr-component-budget-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical Aztec probe cache costs

These measurements retain their original source 9d4d7ff462586a9beecd5e94c9069d6a6bc2035a.

Aztec stores repeated bullseye-probe decisions in compact per-pitch windows instead of one dictionary entry per offset. Each image, threshold and polarity keeps its own cache, bounded to 256 windows with at most 81 state cells each. Off-grid requests and requests after capacity use the ordinary pixel probe. Ring voting, sampled views, search order, cancellation, live deadlines and recognition allowances remain unchanged.

The actual preceding and current ring probes agree with independent 33-point pixel voting across 194,168 requests and 100 image/polarity cases, including 5,848 accepted probes and 457 accepted sampled views. Both polarities, small pitches, clamped edges, noise, the acceptance boundary, reused dimensions, off-grid origins and capacity pressure are covered. Cached accepted probes stop after later cancellation and real clock expiry; disposing the deadline scope restores ordinary acceptance. This diagnostic is separate from corpus accuracy and performance. Full validation includes sixty Aztec cases on each modern framework and twenty-four on .NET Framework.

The proof also retains six exploratory pairs from the same candidate before full qualification. Their complete priming and measured outcomes remain separate from the table below, including inconclusive results and latency losses.

Final fabric on CCD0: 6 → 5 correct image requests out of 12 across twelve measured calls. Exploratory whole on CCD0: 227 → 226 correct image requests out of 228 across twelve measured calls.

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 950.46 → 957.63 | 1.008 | 80.66 → 75.16 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 827.21 → 828.97 | 1.002 | 81.81 → 76.54 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 608.53 → 607.40 | 0.998 | 45.67 → 40.46 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 552.20 → 550.41 | 0.997 | 45.53 → 40.20 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 813.24 → 761.21 | 0.936 | 56.78 → 51.61 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 649.65 → 645.23 | 0.993 | 56.27 → 51.71 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 947.15 → 979.99 | 1.035 | 91.10 → 91.08 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 878.32 → 865.67 | 0.986 | 91.12 → 91.11 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 554.03 → 561.02 | 1.013 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 544.61 → 543.05 | 0.997 | 32.65 → 32.63 | 9 → 9 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Aztec probe cache proof](evidence/aztec-probe-cache-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical PDF417 scanline cache costs

These measurements retain their original source c3bf421307ef938d48d5fdb9f3956022282691af.

PDF417 stores sampled scanlines in bounded 128-line segments with value slots instead of one dictionary entry and allocated wrapper per line. Canonical run ordering, polarity, live expiry and cancellation checks remain unchanged. Segment metadata is charged before admission within the existing logical byte and entry allowances. Search order, checksums, payload validation and recognition allowances are unchanged.

The actual preceding and current private caches agree with independent direct pixel-run enumeration across 95,100 requests and thirty shape/pattern cases. Checks cover segment boundaries, both axes, reversal, inversion, tilted lines, ushort-width boundaries, cached-row cancellation and byte/entry pressure. Cache accounting is a logical charge, not measured allocation or peak memory. PDF417 regression cases pass on all three test frameworks.

The proof also retains six exploratory pairs for segmented storage and six for a rejected recent-segment cursor. The cursor had no consistent latency benefit and produced slower fabric/texture medians and completion losses; it is absent from this source. Exploratory and final qualification costs remain separate.



One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative. One additional CCD0 whole-batch pair investigates the original 18% median latency loss. It uses the same fixed settings and retains the original result separately; no further repeat is selected from its outcome.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 1176.87 → 1240.63 | 1.054 | 80.56 → 76.15 | 0 → 0 | 1 → 1 | 0 → 0 |
| fabric | 1 | CCD1 | 1020.64 → 1008.94 | 0.989 | 83.85 → 80.84 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 791.85 → 788.32 | 0.996 | 47.05 → 45.83 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD1 | 745.02 → 694.03 | 0.932 | 47.84 → 45.80 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 930.73 → 1099.20 | 1.181 | 58.89 → 56.52 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 844.53 → 861.85 | 1.021 | 58.45 → 56.77 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 1074.30 → 1092.90 | 1.017 | 91.12 → 91.11 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 737.61 → 737.72 | 1.000 | 91.10 → 91.09 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 802.10 → 798.66 | 0.996 | 32.67 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 649.42 → 646.47 | 0.995 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| whole followup | 19 | CCD0 | 1119.40 → 958.29 | 0.856 | 57.61 → 56.38 | 18–19 → 18–19 | 0–1 → 0–1 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[PDF417 cache proof](evidence/pdf417-segment-cache-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical retained decoder sessions and deadline costs

These measurements retain their original source f24d627591a7f30c8819b1f1a1eb9d3cf014a9b5.

PDF417 and GS1 DataBar loops capture the current recognition deadline once per synchronous method while preserving live expiry and cancellation checks. Rejected Aztec ring probes check cancellation at each offset and poll the clock after at most 32 bounded candidates; accepted sampled views retain existing checks. Search order, checksums, payload validation and recognition allowances are unchanged.







One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative.

Archived recipe inspection verifies 94 historical paired packets from nineteen source snapshots as fresh-context first-use costs: setup constructs a new decoder context for each call, so setup warmups do not warm the instances used for measurements. The inventory identifies these packets and archived recipes explicitly. Ten other historical paired packets remain unclassified. The main corpus, generation and typed decoding lanes use separate paths. Original costs and completion losses remain retained.

| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| fabric | 1 | CCD0 | 1013.18 → 1024.19 | 1.011 | 84.33 → 83.61 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| fabric | 1 | CCD1 | 883.39 → 908.61 | 1.029 | 84.60 → 84.62 | 0–1 → 0–1 | 0–1 → 0–1 | 0 → 0 |
| texture | 1 | CCD0 | 655.37 → 623.41 | 0.951 | 47.07 → 46.95 | 1 → 1 | 0 → 0 | 0 → 0 |
| texture | 1 | CCD1 | 512.63 → 501.11 | 0.978 | 46.74 → 46.99 | 1 → 1 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD0 | 854.39 → 852.45 | 0.998 | 58.36 → 58.70 | 19 → 19 | 0 → 0 | 0 → 0 |
| whole | 19 | CCD1 | 733.67 → 742.60 | 1.012 | 58.18 → 58.81 | 19 → 19 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD0 | 898.81 → 871.18 | 0.969 | 91.09 → 91.11 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-positive | 87 | CCD1 | 782.30 → 786.52 | 1.005 | 91.09 → 91.10 | 70 → 70 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD0 | 560.49 → 580.20 | 1.035 | 32.65 → 32.65 | 9 → 9 | 0 → 0 | 0 → 0 |
| retail-negative | 9 | CCD1 | 487.13 → 492.04 | 1.010 | 32.66 → 32.66 | 9 → 9 | 0 → 0 | 0 → 0 |

These retained-context costs are a separate workload from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority.

[Session and deadline proof](evidence/retained-contexts-proof.json) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.

## Historical DPM local-window means

These local-window measurements belong to source 4f3d6312bd989d03d5b475b99b22327d56a53561. Their paired decoder contexts are recreated per sample; current retained-context costs appear separately.

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

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative. These retained-context costs are separate from historical fresh-context first-use costs. Lower elapsed time or allocation does not establish reliable completion, lower CPU use, throughput or capacity. Managed allocation excludes native allocations. Shared-host contention affects timing and fixed-deadline completion. Every loss remains included; finite corpus results do not establish overall superiority. Main corpus, supplemental PDF417, alternating negative passes and matched generation/typed decoding are rerun on the exact tested core. Earlier decoder-specific proofs retain original identities. Hints, oracles and recognition allowances are unchanged. Invalid GS1 and undeclared QR encoding policies remain unresolved.

Host CPU samples before the main corpus pass: 59.1%, 94.6%, 73.0%. Before the supplementary negative pass: 67.5%, 66.6%, 63.0%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 31.6% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 35.6% |
| ccd0 | 3 | current | 27/31 | 4 | 0 | 96.4% |
| ccd0 | 4 | previous | 27/31 | 4 | 0 | 50.9% |
| ccd1 | 1 | current | 25/31 | 6 | 0 | 100.0% |
| ccd1 | 2 | previous | 28/31 | 3 | 0 | 69.2% |
| ccd1 | 3 | previous | 27/31 | 4 | 0 | 91.6% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 56.2% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 75.6% | 83.6% | 67.5% |
| decoding-ccd0 | 53.1% | 61.9% | 44.3% |
| decoding-ccd1 | 70.8% | 78.1% | 63.4% |
| generation-ccd0 | 88.6% | 90.0% | 86.9% |
| generation-ccd1 | 96.2% | 97.5% | 94.8% |
| negative-rotation-ccd0-0-previous | 31.6% | 42.5% | 20.6% |
| negative-rotation-ccd0-1-current | 35.6% | 47.0% | 24.3% |
| negative-rotation-ccd0-2-current | 96.4% | 97.0% | 95.8% |
| negative-rotation-ccd0-3-previous | 50.9% | 61.0% | 40.7% |
| negative-rotation-ccd1-0-current | 100.0% | 100.0% | 100.0% |
| negative-rotation-ccd1-1-previous | 69.2% | 74.1% | 64.2% |
| negative-rotation-ccd1-2-previous | 91.6% | 93.3% | 89.7% |
| negative-rotation-ccd1-3-current | 56.2% | 63.6% | 49.0% |
| negatives-ccd0 | 65.7% | 78.1% | 53.3% |
| pdf417-supplemental-current | 28.4% | 40.7% | 16.2% |
| pdf417-supplemental-previous | 68.3% | 76.5% | 60.1% |
| scanner-qr-exact-grid-recheck | 24.7% | 34.9% | 14.6% |
| scanner-retail-negative-diagnostic | 20.2% | 26.8% | 13.7% |
| scanner-retail-positive-diagnostic | 22.3% | 32.4% | 12.3% |

## Generation on ccd0

Source `e32694849a97ac05932f6bf8c9b9c3b232684368`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 106.48 | 139.97 | 70.13–147.17 | 3832 |
| Numeric | Modules | QRCoder | 46.01 | 76.65 | 30.92–77.17 | 3760 |
| Numeric | Modules | ZXing-C++ | 48.23 | 81.12 | 34.02–82.02 | 1696 |
| Numeric | Modules | ZXing.Net | 70.53 | 148.14 | 59.97–184.82 | 31304 |
| Numeric | Png | CodeGlyphX | 138.69 | 193.10 | 108.26–198.09 | 40312 |
| Numeric | Png | QRCoder | 138.57 | 169.24 | 120.76–188.40 | 4817 |
| Numeric | Png | ZXing-C++ | 1006.29 | 1214.00 | 908.47–1335.38 | 848327 |
| Numeric | Png | ZXing.Net | 1033.91 | 1252.29 | 931.60–1320.98 | 877611 |
| Unicode | Modules | CodeGlyphX | 1947.16 | 2012.67 | 1880.24–2023.47 | 23872 |
| Unicode | Modules | QRCoder | 1395.83 | 1457.35 | 1296.88–1459.98 | 16731 |
| Unicode | Modules | ZXing-C++ | 1260.08 | 1338.29 | 1188.92–1373.26 | 14648 |
| Unicode | Modules | ZXing.Net | 1936.56 | 2048.24 | 1845.19–2063.84 | 606584 |
| Unicode | Png | CodeGlyphX | 2892.73 | 5986.17 | 2173.40–6932.27 | 274996 |
| Unicode | Png | QRCoder | 2425.09 | 3976.49 | 1853.72–4237.64 | 20322 |
| Unicode | Png | ZXing-C++ | 15637.50 | 22940.92 | 12079.37–28459.68 | 6207256 |
| Unicode | Png | ZXing.Net | 18427.20 | 25780.68 | 13290.38–26252.65 | 6798876 |
| Url | Modules | CodeGlyphX | 1019.99 | 1137.43 | 866.49–1166.95 | 5057 |
| Url | Modules | QRCoder | 730.74 | 799.92 | 616.70–806.38 | 5026 |
| Url | Modules | ZXing-C++ | 175.63 | 225.55 | 120.71–228.38 | 2711 |
| Url | Modules | ZXing.Net | 954.91 | 1023.80 | 796.85–1041.41 | 71129 |
| Url | Png | CodeGlyphX | 215.75 | 315.86 | 157.92–344.54 | 60310 |
| Url | Png | QRCoder | 230.29 | 322.85 | 193.40–357.53 | 5979 |
| Url | Png | ZXing-C++ | 1498.69 | 1998.91 | 1393.13–2445.93 | 1309091 |
| Url | Png | ZXing.Net | 1672.85 | 1894.47 | 1527.58–1961.28 | 1377574 |

## Generation on ccd1

Source `e32694849a97ac05932f6bf8c9b9c3b232684368`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 122.99 | 164.86 | 99.59–187.65 | 3832 |
| Numeric | Modules | QRCoder | 62.43 | 95.04 | 42.97–106.07 | 3760 |
| Numeric | Modules | ZXing-C++ | 65.63 | 106.56 | 44.65–106.58 | 1696 |
| Numeric | Modules | ZXing.Net | 120.82 | 195.21 | 80.32–212.29 | 31304 |
| Numeric | Png | CodeGlyphX | 135.00 | 168.59 | 110.99–178.68 | 40340 |
| Numeric | Png | QRCoder | 142.38 | 162.75 | 123.26–166.10 | 4931 |
| Numeric | Png | ZXing-C++ | 1042.21 | 1112.13 | 980.57–1128.64 | 848417 |
| Numeric | Png | ZXing.Net | 1035.58 | 1190.00 | 978.40–1195.92 | 877735 |
| Unicode | Modules | CodeGlyphX | 2149.78 | 2644.05 | 1887.62–3111.00 | 23872 |
| Unicode | Modules | QRCoder | 1544.05 | 2219.66 | 1396.87–2758.74 | 16735 |
| Unicode | Modules | ZXing-C++ | 1438.76 | 1933.27 | 1252.84–2116.17 | 14648 |
| Unicode | Modules | ZXing.Net | 2187.88 | 2530.18 | 1937.27–2542.66 | 606585 |
| Unicode | Png | CodeGlyphX | 2113.56 | 2343.41 | 1968.15–2414.61 | 274832 |
| Unicode | Png | QRCoder | 1841.69 | 2283.48 | 1754.85–2669.21 | 19624 |
| Unicode | Png | ZXing-C++ | 8628.30 | 9866.20 | 8045.52–10088.19 | 6206576 |
| Unicode | Png | ZXing.Net | 10157.24 | 11664.53 | 9188.13–12350.65 | 6798001 |
| Url | Modules | CodeGlyphX | 1465.43 | 4073.73 | 273.86–6645.91 | 5057 |
| Url | Modules | QRCoder | 1255.81 | 5390.52 | 224.17–6087.83 | 5022 |
| Url | Modules | ZXing-C++ | 388.44 | 1278.90 | 164.82–1824.05 | 2707 |
| Url | Modules | ZXing.Net | 1630.15 | 7112.20 | 461.42–10656.17 | 71140 |
| Url | Png | CodeGlyphX | 450.57 | 2324.40 | 250.70–2877.14 | 60305 |
| Url | Png | QRCoder | 350.31 | 829.87 | 263.62–942.28 | 5987 |
| Url | Png | ZXing-C++ | 6811.18 | 12787.06 | 2617.74–12924.79 | 1309542 |
| Url | Png | ZXing.Net | 5273.88 | 9775.60 | 2573.43–9977.17 | 1377973 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 823.29 | 1110.57 | 683.65–1345.77 | 27.10 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 28.10 | 38.05 | 24.70–47.69 | 2.23 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 220.77 | 289.72 | 184.78–300.76 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 799.26 | 1129.87 | 697.23–1217.25 | 27.37 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 33.07 | 119.89 | 24.11–201.41 | 2.20 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 223.94 | 381.33 | 193.26–394.54 | 13.03 | 19–19 | 0–0 |

## Paired whole-image decoding cost

One separately owned decoder context per binary is reused across three priming calls and twelve grouped rotated measurements per cache domain. The unchanged 1500 ms robust allowance applies to the same preloaded RGBA32 requests. Loading, outcome evaluation, persistence and disposal are outside operation timing. All thirty calls per workload retain per-image outcomes, including every inconclusive negative. The nineteen-image table retains every priming and measured outcome.

CCD0: the current binary has 0.0 to 1.0 inconclusive images per measured batch, compared with 0.0 to 0.0 for the previous binary. All twelve samples remain in the table and raw packet; the latency difference does not establish better completion.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 645.72 | 990.24 | 574.46–1004.19 | 27.90 | 19–19 | 0–0 | 0–0 | 1.167 |
| ccd0 | Candidate | 753.65 | 1263.44 | 587.51–1342.85 | 27.80 | 18–19 | 0–0 | 0–1 | 1.167 |
| ccd1 | Baseline | 567.45 | 671.81 | 539.64–673.19 | 27.18 | 19–19 | 0–0 | 0–0 | 1.002 |
| ccd1 | Candidate | 568.57 | 663.83 | 540.29–692.57 | 27.18 | 19–19 | 0–0 | 0–0 | 1.002 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
