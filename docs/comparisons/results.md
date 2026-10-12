# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 239/267 | 0 | 3 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
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

This decoding follow-up records 239/267 correct positive images, compared with 234/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

Retail recovery refines guarded anchors by at most three rows in either direction and uses four additional bounded contrast hypotheses for fine bars and glare. Inverted fine bars are normalized before low-contrast padding is classified. Full framing, digit-fit bounds, parity, checksums and independent neighboring-row confirmation remain enforced.

Supplementary contract diagnostics, not a representative accuracy or negative-rate survey. Thirty-one fixed negative IDs are each requested with three explicit retail-family hints (93 requests).

The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.

| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 64/87 | 69/87 | 0 |
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
| ccd0 | 244.99 | 763.64 | 3.12x | 21.34 | 33.91 | 1.59x | 9/9 → 9/9 |
| ccd1 | 224.18 | 681.31 | 3.04x | 21.34 | 33.92 | 1.59x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

Same 87 preloaded retail positives with identical hints, payload validation and 1500 ms allowance. Separate contexts, 3 warmups and 12 rotated measurements per binary/domain, Normal priority and fixed affinity; no outlier removal. Loading and evaluation are excluded. More successful decodes change completed work. Managed allocation excludes native costs. All measured samples and failed warmups are retained; successful warmup samples are not retained by PowerForge.

| Domain | Previous → current median ms / 87 positives | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 87 |
| --- | ---: | ---: | ---: | ---: |
| CCD0 | 447.025 → 1144.541 | 2.560 | 72.75 → 90.61 | 64.000 → 69.000 |
| CCD1 | 362.376 → 921.001 | 2.542 | 72.76 → 90.61 | 64.000 → 69.000 |

Three independent EAN photographs pass in both orientations and polarities. 10 of 16 new cases fail against the exact preceding binary; all pass on the current core. Lone-row and invalid-checksum inputs remain rejected.

[Retail row-recovery proof](evidence/retail-row-recovery-proof.json) · [CCD0 positive cost samples](evidence/retail-positive-cost-ccd0-result.json) · [CCD1 positive cost samples](evidence/retail-positive-cost-ccd1-result.json).

The current pass records 239/267 correct positive images versus 234/267 previously; gained: ean13-2/12.png, ean13-4/04.png, ean13-4/07.png, ean13-4/19.png, upca-5/27.png; previously correct positive images lost: none. Retail recovery preserves guard geometry, digit-fit bounds, parity, checksums and matching neighboring physical rows. Hints, oracles, format priorities, presentation and recognition allowances are unchanged. Earlier ITF, Code 39, Data Matrix, PDF417, QR and Codabar improvement measurements remain historical. Current corpus, retail and whole-image costs are rerun on the frozen core. Both binaries include MaxiCode recognition. Supplemental PDF417 and MaxiCode images have informed fixtures and are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and wall-clock budgets affect results. Multi-instance and mirrored MaxiCode, remaining stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 15.2%, 19.9%, 32.7%. Before the supplementary negative pass: 7.6%, 11.1%, 26.6%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 13.9% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 84.3% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 79.0% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 23.6% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 78.2% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 94.5% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 22.8% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 32.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 22.6% | 33.7% | 11.4% |
| decoding-ccd0 | 94.3% | 95.5% | 93.1% |
| decoding-ccd1 | 56.2% | 70.8% | 41.2% |
| diagnostic-final-codabar-diagnostic-current | 23.8% | 37.7% | 10.2% |
| diagnostic-final-codabar-diagnostic-previous | 15.3% | 24.8% | 5.8% |
| diagnostic-final-codabar-supplemental-current | 37.7% | 49.1% | 26.3% |
| diagnostic-final-codabar-supplemental-previous | 11.7% | 21.6% | 2.2% |
| diagnostic-final-code39-negative-current | 13.8% | 24.8% | 2.8% |
| diagnostic-final-code39-negative-previous | 18.5% | 26.3% | 10.7% |
| diagnostic-final-code39-positive-current | 16.5% | 25.2% | 8.0% |
| diagnostic-final-code39-positive-previous | 30.4% | 40.6% | 20.1% |
| diagnostic-final-itf-negative-current | 42.3% | 58.1% | 26.5% |
| diagnostic-final-itf-negative-previous | 33.3% | 56.9% | 9.8% |
| diagnostic-final-itf-positive-current | 38.9% | 54.3% | 23.5% |
| diagnostic-final-itf-positive-previous | 66.7% | 74.8% | 58.7% |
| diagnostic-final-retail-negative-current | 12.8% | 24.0% | 1.9% |
| diagnostic-final-retail-negative-previous | 30.4% | 46.6% | 14.2% |
| diagnostic-final-retail-positive-current | 13.7% | 24.7% | 2.8% |
| diagnostic-final-retail-positive-previous | 31.7% | 48.0% | 15.3% |
| generation-ccd0 | 40.7% | 54.9% | 26.4% |
| generation-ccd1 | 21.7% | 35.9% | 7.8% |
| holdout-current | 15.0% | 28.3% | 2.2% |
| negative-rotation-ccd0-0-previous | 13.9% | 23.4% | 4.5% |
| negative-rotation-ccd0-1-current | 84.3% | 90.8% | 77.9% |
| negative-rotation-ccd0-2-current | 79.0% | 86.0% | 71.9% |
| negative-rotation-ccd0-3-previous | 23.6% | 35.2% | 12.0% |
| negative-rotation-ccd1-0-current | 78.2% | 83.6% | 72.8% |
| negative-rotation-ccd1-1-previous | 94.5% | 95.3% | 93.8% |
| negative-rotation-ccd1-2-previous | 22.8% | 41.1% | 4.5% |
| negative-rotation-ccd1-3-current | 32.5% | 48.9% | 16.1% |
| negatives-ccd0 | 15.1% | 24.3% | 6.3% |

## Generation on ccd0

Source `7c907b2c9f64b622fac80fde24d9a2f421035b39`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 107.91 | 171.11 | 90.99–183.92 | 3832 |
| Numeric | Modules | QRCoder | 157.49 | 223.20 | 105.97–245.95 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.49 | 92.45 | 41.94–113.78 | 1696 |
| Numeric | Modules | ZXing.Net | 70.03 | 95.72 | 39.85–95.73 | 31304 |
| Numeric | Png | CodeGlyphX | 121.06 | 164.06 | 94.08–181.95 | 40312 |
| Numeric | Png | QRCoder | 212.92 | 273.13 | 157.68–281.86 | 6568 |
| Numeric | Png | ZXing-C++ | 923.94 | 1000.06 | 773.61–1010.58 | 848308 |
| Numeric | Png | ZXing.Net | 941.61 | 1073.10 | 832.99–1090.63 | 877599 |
| Unicode | Modules | CodeGlyphX | 1849.67 | 2161.57 | 1723.93–2238.32 | 23872 |
| Unicode | Modules | QRCoder | 2914.58 | 3037.01 | 2621.54–3072.51 | 23216 |
| Unicode | Modules | ZXing-C++ | 1207.36 | 1342.01 | 1121.34–1407.83 | 14648 |
| Unicode | Modules | ZXing.Net | 1881.84 | 1983.90 | 1705.98–2010.20 | 606585 |
| Unicode | Png | CodeGlyphX | 2126.69 | 2420.45 | 1891.77–2520.83 | 274832 |
| Unicode | Png | QRCoder | 3407.19 | 3810.89 | 3110.29–3849.08 | 26112 |
| Unicode | Png | ZXing-C++ | 8470.57 | 9592.94 | 7866.28–9791.73 | 6206710 |
| Unicode | Png | ZXing.Net | 9531.61 | 10189.07 | 8545.55–10288.10 | 6798179 |
| Url | Modules | CodeGlyphX | 948.45 | 1119.04 | 277.69–1149.26 | 5057 |
| Url | Modules | QRCoder | 1145.22 | 1361.97 | 977.16–1368.29 | 7415 |
| Url | Modules | ZXing-C++ | 173.69 | 267.18 | 74.81–282.47 | 2709 |
| Url | Modules | ZXing.Net | 869.59 | 1082.65 | 300.08–1147.51 | 71141 |
| Url | Png | CodeGlyphX | 204.23 | 283.32 | 151.09–291.59 | 60278 |
| Url | Png | QRCoder | 490.53 | 607.10 | 410.51–610.24 | 8200 |
| Url | Png | ZXing-C++ | 1531.40 | 1675.26 | 1462.28–1701.97 | 1309022 |
| Url | Png | ZXing.Net | 1626.86 | 1710.93 | 1520.64–1724.56 | 1377485 |

## Generation on ccd1

Source `7c907b2c9f64b622fac80fde24d9a2f421035b39`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 143.55 | 291.53 | 63.55–440.34 | 3832 |
| Numeric | Modules | QRCoder | 135.39 | 304.24 | 119.23–415.10 | 5568 |
| Numeric | Modules | ZXing-C++ | 55.95 | 140.71 | 42.47–169.12 | 1696 |
| Numeric | Modules | ZXing.Net | 69.89 | 103.90 | 57.31–111.77 | 31304 |
| Numeric | Png | CodeGlyphX | 92.92 | 108.78 | 80.92–112.70 | 40312 |
| Numeric | Png | QRCoder | 167.57 | 240.36 | 141.12–277.77 | 6568 |
| Numeric | Png | ZXing-C++ | 746.04 | 873.21 | 693.10–895.80 | 848307 |
| Numeric | Png | ZXing.Net | 777.54 | 922.55 | 711.61–964.78 | 877622 |
| Unicode | Modules | CodeGlyphX | 1846.84 | 2148.08 | 1612.52–2215.90 | 23872 |
| Unicode | Modules | QRCoder | 2749.93 | 3523.47 | 2511.78–3837.22 | 23216 |
| Unicode | Modules | ZXing-C++ | 1187.56 | 1421.39 | 1021.63–1518.05 | 14648 |
| Unicode | Modules | ZXing.Net | 1758.39 | 2799.49 | 1546.28–3089.15 | 606585 |
| Unicode | Png | CodeGlyphX | 1918.98 | 2710.77 | 1745.27–3159.41 | 274832 |
| Unicode | Png | QRCoder | 3248.87 | 3707.30 | 2880.53–3802.92 | 26112 |
| Unicode | Png | ZXing-C++ | 8375.65 | 11179.35 | 7419.08–12753.39 | 6206768 |
| Unicode | Png | ZXing.Net | 9553.11 | 12173.49 | 7682.91–12241.73 | 6798262 |
| Url | Modules | CodeGlyphX | 852.33 | 1312.61 | 606.83–1356.66 | 5057 |
| Url | Modules | QRCoder | 988.12 | 1517.23 | 805.00–1726.73 | 7415 |
| Url | Modules | ZXing-C++ | 147.11 | 252.50 | 97.28–270.08 | 2709 |
| Url | Modules | ZXing.Net | 881.55 | 1089.22 | 434.87–1156.03 | 71152 |
| Url | Png | CodeGlyphX | 192.49 | 387.77 | 127.42–498.28 | 60319 |
| Url | Png | QRCoder | 404.78 | 605.64 | 373.50–691.60 | 8375 |
| Url | Png | ZXing-C++ | 1662.86 | 3050.46 | 1201.90–3374.49 | 1309567 |
| Url | Png | ZXing.Net | 1716.88 | 2970.76 | 1245.60–3166.15 | 1377962 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1718.34 | 1972.93 | 1551.36–2011.80 | 84.69 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 31.26 | 40.47 | 25.36–43.27 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 255.50 | 305.43 | 196.20–341.82 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1688.42 | 1851.53 | 1470.43–1881.21 | 85.22 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 26.16 | 41.13 | 21.18–49.56 | 2.19 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 212.73 | 309.85 | 167.14–360.67 | 12.96 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 3.4% higher on CCD0, 3.3% higher on CCD1.

Current unfinished image searches occur in 1/24 measured batches, versus 0/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2106.65 | 2463.94 | 2000.86–2549.76 | 80.17 | 19–19 | 0–0 | 0–0 | 1.034 |
| ccd0 | Candidate | 2179.01 | 2620.91 | 2027.85–2681.45 | 84.51 | 18–19 | 0–0 | 0–1 | 1.034 |
| ccd1 | Baseline | 1919.15 | 2082.50 | 1847.49–2178.54 | 81.07 | 19–19 | 0–0 | 0–0 | 1.033 |
| ccd1 | Candidate | 1983.01 | 2352.01 | 1909.54–2501.24 | 86.48 | 19–19 | 0–0 | 0–0 | 1.033 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
