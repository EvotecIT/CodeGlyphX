# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 225/267 | 0 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
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

This decoding follow-up records 225/267 correct positive images, compared with 225/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

| Domain | Previous median ms | Current median ms | Current/previous | Previous/current mean managed bytes | Previous/current mean correct requests | Previous/current mean inconclusive requests |
|---|---:|---:|---:|---:|---:|---:|
| CCD0 | 2939.511 | 2709.288 | 0.922 | 177,688,295 / 177,560,335 | 2.667 / 2.750 | 1.333 / 1.250 |
| CCD1 | 2146.916 | 2179.852 | 1.015 | 187,482,001 / 185,419,214 | 3.000 / 3.000 | 1.000 / 1.000 |

Private cache checks compare every run against independently sampled pixels. Instrumented cache counts use two existing negatives, two PDF417-only passes with a 60,000 ms diagnostic allowance to complete all work; their timings are unranked. The actual baseline/current cache source is retained in the archive. Cache byte accounting is an estimate and excludes other decoder working memory; no peak-memory claim is made. The paired controls and whole-image costs preserve the 1,500 ms allowance, preloaded pixels, hints and empty oracles. Shared-host load limits portable latency and completion claims.

[Cache checks, counts and paired costs](evidence/pdf417-cache-cost-proof.json). Whole-image comparison results remain the practical measure of scanner cost and completion.

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
| ccd0 | 252.74 | 249.75 | 0.99x | 21.36 | 21.35 | 1.00x | 9/9 → 9/9 |
| ccd1 | 240.93 | 236.04 | 0.98x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 225/267 correct positive images versus 225/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [27, 29, 29, 29]/31 currently and [29, 29, 29, 29]/31 previously. The current change retains more PDF417 sampled lines using compact exact run widths within the same estimated cache byte allowance. Original hints, payload oracles, search bounds, variant order and per-image deadlines remain fixed. Both binaries include MaxiCode image recognition. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 and MaxiCode images have informed permanent fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 5.6%, 8.3%, 7.1%. Before the supplementary negative pass: 28.5%, 25.3%, 73.6%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 52.2% |
| ccd0 | 2 | current | 27/31 | 4 | 0 | 71.1% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 96.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 68.9% |
| ccd1 | 1 | current | 29/31 | 2 | 0 | 20.0% |
| ccd1 | 2 | previous | 29/31 | 2 | 0 | 80.2% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 60.6% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 44.0% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 7.0% | 9.7% | 4.7% |
| decoding-ccd0 | 26.9% | 45.7% | 8.0% |
| decoding-ccd1 | 25.8% | 44.1% | 7.4% |
| diagnostic-final-retail-negative-current | 16.0% | 27.6% | 4.3% |
| diagnostic-final-retail-negative-previous | 23.4% | 37.7% | 9.2% |
| diagnostic-final-retail-positive-current | 31.3% | 47.2% | 15.5% |
| diagnostic-final-retail-positive-previous | 48.7% | 66.3% | 31.1% |
| generation-ccd0 | 42.5% | 59.7% | 25.4% |
| generation-ccd1 | 47.0% | 63.0% | 31.1% |
| holdout-current | 55.8% | 68.2% | 43.4% |
| negative-rotation-ccd0-0-previous | 52.2% | 64.9% | 39.4% |
| negative-rotation-ccd0-1-current | 71.1% | 78.9% | 62.6% |
| negative-rotation-ccd0-2-current | 96.4% | 98.1% | 94.7% |
| negative-rotation-ccd0-3-previous | 68.9% | 78.2% | 59.6% |
| negative-rotation-ccd1-0-current | 20.0% | 36.4% | 3.7% |
| negative-rotation-ccd1-1-previous | 80.2% | 88.7% | 71.6% |
| negative-rotation-ccd1-2-previous | 60.6% | 69.1% | 52.2% |
| negative-rotation-ccd1-3-current | 44.0% | 60.0% | 28.0% |
| negatives-ccd0 | 42.4% | 57.9% | 27.1% |

## Generation on ccd0

Source `34bfc0ab2216f02a6d84acbed997b3b9f9513f90`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 104.61 | 172.27 | 91.18–198.12 | 3832 |
| Numeric | Modules | QRCoder | 139.32 | 194.32 | 122.51–218.77 | 5568 |
| Numeric | Modules | ZXing-C++ | 53.44 | 81.05 | 42.49–84.25 | 1696 |
| Numeric | Modules | ZXing.Net | 78.90 | 155.51 | 64.23–160.34 | 31304 |
| Numeric | Png | CodeGlyphX | 122.93 | 162.16 | 115.12–166.50 | 40312 |
| Numeric | Png | QRCoder | 229.08 | 328.93 | 208.34–342.42 | 6568 |
| Numeric | Png | ZXing-C++ | 953.98 | 999.62 | 916.01–1001.87 | 848325 |
| Numeric | Png | ZXing.Net | 995.15 | 1180.45 | 940.81–1239.81 | 877608 |
| Unicode | Modules | CodeGlyphX | 2253.51 | 3548.60 | 1945.14–4539.35 | 23872 |
| Unicode | Modules | QRCoder | 4265.18 | 6492.35 | 3088.29–6989.77 | 23217 |
| Unicode | Modules | ZXing-C++ | 1725.12 | 2298.03 | 1320.60–2499.76 | 14648 |
| Unicode | Modules | ZXing.Net | 2329.25 | 3672.04 | 1974.47–4760.81 | 606584 |
| Unicode | Png | CodeGlyphX | 2053.81 | 2256.68 | 1957.96–2292.91 | 274832 |
| Unicode | Png | QRCoder | 3379.79 | 3656.12 | 2898.42–3710.06 | 26168 |
| Unicode | Png | ZXing-C++ | 8586.79 | 10198.34 | 7963.10–10479.31 | 6206731 |
| Unicode | Png | ZXing.Net | 9724.17 | 11562.48 | 8309.16–11781.70 | 6798304 |
| Url | Modules | CodeGlyphX | 1021.00 | 1820.60 | 254.53–2633.76 | 5057 |
| Url | Modules | QRCoder | 1396.20 | 1802.94 | 886.03–1919.67 | 7409 |
| Url | Modules | ZXing-C++ | 159.80 | 352.23 | 98.94–404.12 | 2707 |
| Url | Modules | ZXing.Net | 955.26 | 1676.79 | 229.57–2027.20 | 71136 |
| Url | Png | CodeGlyphX | 211.00 | 339.06 | 178.13–374.21 | 60278 |
| Url | Png | QRCoder | 505.19 | 601.66 | 383.10–628.02 | 8200 |
| Url | Png | ZXing-C++ | 1674.53 | 1893.77 | 1536.45–1995.93 | 1309021 |
| Url | Png | ZXing.Net | 1760.06 | 2029.45 | 1460.84–2184.39 | 1377466 |

## Generation on ccd1

Source `34bfc0ab2216f02a6d84acbed997b3b9f9513f90`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 92.45 | 194.18 | 61.34–251.35 | 3832 |
| Numeric | Modules | QRCoder | 107.46 | 266.07 | 74.15–287.95 | 5568 |
| Numeric | Modules | ZXing-C++ | 48.81 | 95.09 | 28.09–106.72 | 1696 |
| Numeric | Modules | ZXing.Net | 66.78 | 159.85 | 40.13–171.03 | 31304 |
| Numeric | Png | CodeGlyphX | 112.08 | 149.63 | 81.50–161.58 | 40312 |
| Numeric | Png | QRCoder | 175.47 | 232.21 | 140.78–268.06 | 6568 |
| Numeric | Png | ZXing-C++ | 802.83 | 990.64 | 726.26–1100.80 | 848315 |
| Numeric | Png | ZXing.Net | 862.60 | 1057.75 | 763.67–1091.18 | 877627 |
| Unicode | Modules | CodeGlyphX | 1591.65 | 1769.32 | 1509.21–1791.37 | 23872 |
| Unicode | Modules | QRCoder | 2422.76 | 2890.59 | 2285.41–3027.86 | 23216 |
| Unicode | Modules | ZXing-C++ | 1087.94 | 1201.39 | 1000.41–1208.28 | 14648 |
| Unicode | Modules | ZXing.Net | 1581.17 | 1755.66 | 1463.91–1760.85 | 606584 |
| Unicode | Png | CodeGlyphX | 1869.09 | 2146.69 | 1612.05–2190.65 | 274832 |
| Unicode | Png | QRCoder | 3232.95 | 3739.77 | 2827.44–3901.81 | 26278 |
| Unicode | Png | ZXing-C++ | 7666.74 | 8728.17 | 6748.87–8808.00 | 6206979 |
| Unicode | Png | ZXing.Net | 8463.74 | 9696.53 | 7388.43–9957.88 | 6798563 |
| Url | Modules | CodeGlyphX | 738.95 | 966.22 | 630.91–1106.15 | 5057 |
| Url | Modules | QRCoder | 933.34 | 1087.18 | 790.96–1099.75 | 7415 |
| Url | Modules | ZXing-C++ | 132.43 | 187.09 | 101.89–195.73 | 2709 |
| Url | Modules | ZXing.Net | 681.48 | 892.38 | 617.40–947.66 | 71146 |
| Url | Png | CodeGlyphX | 157.80 | 267.36 | 132.88–309.20 | 60294 |
| Url | Png | QRCoder | 409.19 | 540.17 | 379.50–551.07 | 8200 |
| Url | Png | ZXing-C++ | 1319.31 | 1784.27 | 1177.96–1915.91 | 1309055 |
| Url | Png | ZXing.Net | 1438.38 | 1616.15 | 1267.59–1665.80 | 1377528 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1594.84 | 1667.60 | 1526.93–1668.19 | 82.47 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 25.03 | 28.45 | 20.68–28.47 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 188.78 | 196.98 | 166.71–198.02 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1568.15 | 1613.27 | 1470.67–1615.57 | 82.69 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 22.78 | 26.50 | 19.50–27.16 | 2.23 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 172.11 | 206.43 | 155.04–222.33 | 12.99 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 0.3% higher on CCD0, 1.4% higher on CCD1.

Current unfinished image searches occur in 0/24 measured batches, versus 0/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2279.69 | 2500.58 | 2098.40–2543.47 | 82.64 | 19–19 | 0–0 | 0–0 | 1.003 |
| ccd0 | Candidate | 2287.44 | 2360.14 | 2112.60–2406.38 | 81.61 | 19–19 | 0–0 | 0–0 | 1.003 |
| ccd1 | Baseline | 2004.14 | 2067.01 | 1902.26–2076.12 | 83.94 | 19–19 | 0–0 | 0–0 | 1.014 |
| ccd1 | Candidate | 2031.78 | 2138.94 | 1893.49–2199.15 | 82.79 | 19–19 | 0–0 | 0–0 | 1.014 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
