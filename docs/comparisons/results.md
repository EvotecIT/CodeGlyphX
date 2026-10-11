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

## Negative-image decoding work

Direct-mark retries reuse one local-intensity table across dark and light adaptive masks. PDF417 scanlines reject guard ratios that cannot satisfy the existing fit. Search bounds, variant order, cancellation checks, recognition allowances and payload scoring are unchanged.

The actual previous and current private methods produce identical bytes for 36 preprocessing outputs across 12 configurations. All 400,000 guard decisions match, including 14,877 accepted fits.

Deterministic private-method diagnostics protect original arithmetic and outputs; they are not a representative accuracy or speed survey. Instrumented baseline stacks identify expensive searches but do not establish exclusive CPU cycles. Isolated-family timings have different allowance distribution from all-format searches and are not ranked benchmarks. Allocation savings do not establish a latency win or completion of every negative. Whole-image paired results retain every sample, recognition difference and cost loss.

The [decision and allocation diagnostic](evidence/negative-search-cost-proof.json) records source hashes, output checks and allocation observations. Paired whole-image costs and negative completion below determine the measured outcome.

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
| ccd0 | 225.37 | 243.02 | 1.08x | 21.36 | 21.36 | 1.00x | 9/9 → 9/9 |
| ccd1 | 263.20 | 270.93 | 1.03x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 225/267 correct positive images versus 225/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [29, 30, 29, 30]/31 currently and [29, 29, 30, 30]/31 previously. The current change reuses direct-mark preprocessing data and rejects mathematically impossible PDF417 guards. Original hints, payload oracles, search bounds, variant order and per-image deadlines remain fixed. Both binaries include MaxiCode image recognition. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 and MaxiCode images have informed permanent fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 33.8%, 31.9%, 18.6%. Before the supplementary negative pass: 17.2%, 13.9%, 23.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 20/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 78.6% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 39.3% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 62.6% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 30.6% |
| ccd1 | 1 | current | 29/31 | 2 | 0 | 36.2% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 44.7% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 47.2% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 26.3% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 28.1% | 43.5% | 12.8% |
| decoding-ccd0 | 44.7% | 59.1% | 30.3% |
| decoding-ccd1 | 86.3% | 91.1% | 81.5% |
| diagnostic-final-retail-negative-current | 36.6% | 51.2% | 22.0% |
| diagnostic-final-retail-negative-previous | 43.7% | 58.5% | 29.1% |
| diagnostic-final-retail-positive-current | 27.7% | 42.1% | 13.3% |
| diagnostic-final-retail-positive-previous | 62.5% | 73.8% | 51.3% |
| generation-ccd0 | 37.9% | 58.0% | 18.2% |
| generation-ccd1 | 42.6% | 57.7% | 27.6% |
| holdout-current | 63.3% | 73.4% | 53.2% |
| negative-rotation-ccd0-0-previous | 78.6% | 84.4% | 72.7% |
| negative-rotation-ccd0-1-current | 39.3% | 53.9% | 24.6% |
| negative-rotation-ccd0-2-current | 62.6% | 72.2% | 53.1% |
| negative-rotation-ccd0-3-previous | 30.6% | 46.4% | 14.8% |
| negative-rotation-ccd1-0-current | 36.2% | 52.3% | 20.1% |
| negative-rotation-ccd1-1-previous | 44.7% | 58.4% | 31.0% |
| negative-rotation-ccd1-2-previous | 47.2% | 59.4% | 34.9% |
| negative-rotation-ccd1-3-current | 26.3% | 43.6% | 9.1% |
| negatives-ccd0 | 18.1% | 33.5% | 3.2% |

## Generation on ccd0

Source `c7f1eb7ea85e698efc709df31b2b34504ea5ca44`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 105.18 | 181.64 | 62.12–245.06 | 3832 |
| Numeric | Modules | QRCoder | 133.36 | 178.55 | 97.42–181.44 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.78 | 92.34 | 37.80–108.98 | 1696 |
| Numeric | Modules | ZXing.Net | 87.27 | 125.37 | 42.60–140.78 | 31304 |
| Numeric | Png | CodeGlyphX | 146.15 | 240.89 | 113.05–304.91 | 40312 |
| Numeric | Png | QRCoder | 258.57 | 384.62 | 201.83–407.22 | 6568 |
| Numeric | Png | ZXing-C++ | 1175.27 | 1616.12 | 964.25–2013.72 | 848316 |
| Numeric | Png | ZXing.Net | 1234.10 | 1621.90 | 988.94–1846.75 | 877604 |
| Unicode | Modules | CodeGlyphX | 1961.51 | 2157.07 | 1806.31–2209.12 | 23872 |
| Unicode | Modules | QRCoder | 3081.35 | 3423.34 | 2841.60–3667.80 | 23216 |
| Unicode | Modules | ZXing-C++ | 1269.46 | 1338.63 | 1236.43–1364.04 | 14648 |
| Unicode | Modules | ZXing.Net | 2053.14 | 2300.07 | 1820.22–2321.77 | 606584 |
| Unicode | Png | CodeGlyphX | 2685.08 | 3960.90 | 2093.87–5172.79 | 274832 |
| Unicode | Png | QRCoder | 4179.20 | 6061.29 | 3411.51–6193.37 | 26112 |
| Unicode | Png | ZXing-C++ | 12695.15 | 22928.21 | 8929.16–24699.19 | 6206773 |
| Unicode | Png | ZXing.Net | 13487.55 | 26867.29 | 10372.47–27452.06 | 6798205 |
| Url | Modules | CodeGlyphX | 824.59 | 1471.76 | 139.72–1544.59 | 5057 |
| Url | Modules | QRCoder | 928.99 | 1540.24 | 439.41–2041.03 | 7407 |
| Url | Modules | ZXing-C++ | 161.38 | 315.70 | 79.62–344.32 | 2709 |
| Url | Modules | ZXing.Net | 962.13 | 1363.27 | 210.37–1423.47 | 71142 |
| Url | Png | CodeGlyphX | 266.96 | 365.36 | 180.00–372.61 | 60278 |
| Url | Png | QRCoder | 559.09 | 977.08 | 466.32–1089.72 | 8200 |
| Url | Png | ZXing-C++ | 1672.43 | 3683.49 | 1506.02–3721.54 | 1309049 |
| Url | Png | ZXing.Net | 1788.17 | 4119.15 | 1652.41–5019.04 | 1377465 |

## Generation on ccd1

Source `c7f1eb7ea85e698efc709df31b2b34504ea5ca44`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 77.64 | 104.71 | 62.04–111.31 | 3832 |
| Numeric | Modules | QRCoder | 98.22 | 124.58 | 77.67–124.80 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.05 | 65.30 | 29.12–67.44 | 1696 |
| Numeric | Modules | ZXing.Net | 48.88 | 69.61 | 38.90–70.77 | 31304 |
| Numeric | Png | CodeGlyphX | 164.39 | 256.67 | 88.32–284.59 | 40312 |
| Numeric | Png | QRCoder | 290.16 | 383.18 | 164.26–436.67 | 6568 |
| Numeric | Png | ZXing-C++ | 1011.53 | 1557.96 | 859.36–1581.62 | 848324 |
| Numeric | Png | ZXing.Net | 1217.14 | 1369.42 | 828.92–1379.29 | 877601 |
| Unicode | Modules | CodeGlyphX | 1908.60 | 2279.99 | 1680.50–2530.73 | 23872 |
| Unicode | Modules | QRCoder | 3080.19 | 3338.35 | 2714.85–3347.42 | 23216 |
| Unicode | Modules | ZXing-C++ | 1299.18 | 1542.92 | 1142.89–1658.31 | 14648 |
| Unicode | Modules | ZXing.Net | 1868.27 | 2154.08 | 1700.92–2188.34 | 606584 |
| Unicode | Png | CodeGlyphX | 1702.93 | 2440.70 | 1637.02–2971.38 | 274832 |
| Unicode | Png | QRCoder | 2880.39 | 3366.13 | 2735.02–3372.59 | 26112 |
| Unicode | Png | ZXing-C++ | 6339.55 | 7096.92 | 6096.21–7268.41 | 6206798 |
| Unicode | Png | ZXing.Net | 7888.64 | 9493.39 | 6950.81–10082.59 | 6798210 |
| Url | Modules | CodeGlyphX | 862.47 | 1973.19 | 273.77–2907.66 | 5057 |
| Url | Modules | QRCoder | 1032.41 | 1948.15 | 880.25–2659.60 | 7408 |
| Url | Modules | ZXing-C++ | 150.09 | 215.65 | 93.16–219.35 | 2709 |
| Url | Modules | ZXing.Net | 836.18 | 1078.38 | 222.22–1173.94 | 71136 |
| Url | Png | CodeGlyphX | 193.90 | 309.78 | 136.38–384.25 | 60278 |
| Url | Png | QRCoder | 489.79 | 584.68 | 422.73–624.60 | 8200 |
| Url | Png | ZXing-C++ | 1514.65 | 1735.84 | 1268.90–1821.09 | 1309016 |
| Url | Png | ZXing.Net | 1698.97 | 1987.24 | 1324.59–2145.59 | 1377513 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image that was unsupported by the previous source, and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1572.15 | 1934.92 | 1454.85–1985.61 | 82.76 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 22.49 | 63.14 | 20.76–97.86 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 183.43 | 238.70 | 158.69–242.17 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1574.08 | 1973.56 | 1474.97–2121.03 | 85.80 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 23.15 | 32.09 | 19.50–32.63 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 209.31 | 251.15 | 155.30–257.87 | 12.98 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Both binaries support MaxiCode image recognition; negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 0.3% higher on CCD0, 0.6% lower on CCD1.

Current unfinished image searches occur in 7/24 measured batches, versus 6/24 previously. Every sample and recognition outcome is retained.

Both binaries include MaxiCode image recognition. Correctness and completion remain measured alongside time; a faster failed search does not establish a speed win.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2257.35 | 2514.80 | 1895.29–2573.77 | 88.25 | 18–19 | 0–0 | 0–1 | 1.003 |
| ccd0 | Candidate | 2263.81 | 2559.22 | 1919.42–2582.85 | 83.23 | 19–19 | 0–0 | 0–0 | 1.003 |
| ccd1 | Baseline | 2641.62 | 3003.87 | 1956.36–3102.43 | 80.01 | 18–19 | 0–0 | 0–1 | 0.994 |
| ccd1 | Candidate | 2625.84 | 2990.17 | 1949.54–3009.13 | 75.52 | 18–19 | 0–0 | 0–1 | 0.994 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
