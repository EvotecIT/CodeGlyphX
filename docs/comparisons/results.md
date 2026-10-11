# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images. Every positive format has a CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 225/267 | 0 | 3 | 0 | 7 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 5 | 10 | 12 |
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

This decoding follow-up records 225/267 correct positive images, compared with 219/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png, falsepositives/03.png, falsepositives/04.png, falsepositives-2/14.png, falsepositives-2/17.png, falsepositives-2/06.png

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

The same six MaxiCode images now decode correctly (6/6), versus 0/6 previously. All six previously lacked image-scanning support. Their bytes, format hints and payload oracles are unchanged.

The scanner verifies circular finder bands, samples the staggered module grid and uses the existing primary and secondary error correction. It accepts planar rotation and inverted polarity, and can recover small damage to fixed orientation modules. The existing result carries secondary text and bytes, typed carrier metadata and the searched source region.

Secondary text and bytes remain exact library results. Postal, country and service fields remain typed metadata; the comparison adapter presents them losslessly in the corpus carrier string.

Six unmodified independently authored images and original expected payloads, selected before outcomes. They now also inform permanent regression fixtures and are not an unseen holdout.

One symbol per image; multiple MaxiCode instances, symbol geometry, mirrored lattices and arbitrary perspective are unproved. Planar rotation, inverse polarity and label clutter have focused regression coverage. Frozen corpus recognition and cost do not establish general or equal-accuracy superiority.

The [MaxiCode proof](evidence/maxicode-image-proof.json) records source hashes and search bounds. The capability catalog advertises one-symbol image scanning without geometry. Independent fixture tests do not establish coverage of every numeric MaxiCode mode; fixture filenames are not encoding-mode evidence.

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
| ccd0 | 287.96 | 297.49 | 1.03x | 21.36 | 21.35 | 1.00x | 9/9 → 9/9 |
| ccd1 | 352.85 | 340.90 | 0.97x | 21.37 | 21.36 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

The current pass records 225/267 correct positive images versus 219/267 previously; gained: maxicode-1/with-errors.png, maxicode-1/1.png, maxicode-1/MODE6.png, maxicode-1/MODE3.png, maxicode-1/mode4-mixed-sets.png, maxicode-1/MODE5.png; previously correct positive images lost: none. Paired negative completion is [29, 29, 30, 30]/31 currently and [22, 29, 30, 30]/31 previously. MaxiCode image scanning and its lossless carrier comparison presentation address the six formerly unsupported inputs. Original hints, payload oracles and per-image deadlines remain fixed. Default-family allowance distribution includes the added image family. Paired nineteen-image and unchanged retail costs retain every sample, failure and allocation loss. Supplemental PDF417 images and the six MaxiCode images have informed permanent regression fixtures; they are not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Multi-instance MaxiCode localization, mirrored MaxiCode lattices, stacked DataBar localization and arbitrary perspective remain unproved. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 63.3%, 50.8%, 60.0%. Before the supplementary negative pass: 36.9%, 26.8%, 28.0%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 20/25 | 0 | 5 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 20/25 supplementary negatives versus 23/25. Previously completed images now unfinished: falsepositives-2/06.png, falsepositives-2/12.png, falsepositives-2/18.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 22/31 | 9 | 0 | 99.8% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 36.3% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 75.3% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 40.1% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 82.5% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 20.3% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 61.6% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 20.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 58.1% | 70.8% | 45.1% |
| decoding-ccd0 | 37.3% | 50.0% | 24.7% |
| decoding-ccd1 | 24.1% | 39.2% | 9.0% |
| diagnostic-final-retail-negative-current | 24.4% | 40.9% | 7.9% |
| diagnostic-final-retail-negative-previous | 48.3% | 64.2% | 32.4% |
| diagnostic-final-retail-positive-current | 25.7% | 42.8% | 8.6% |
| diagnostic-final-retail-positive-previous | 30.6% | 51.4% | 9.8% |
| generation-ccd0 | 31.2% | 46.1% | 16.4% |
| generation-ccd1 | 19.3% | 35.4% | 3.2% |
| holdout-current | 31.7% | 50.3% | 13.2% |
| negative-rotation-ccd0-0-previous | 99.8% | 100.0% | 99.7% |
| negative-rotation-ccd0-1-current | 36.3% | 46.8% | 25.9% |
| negative-rotation-ccd0-2-current | 75.3% | 82.5% | 68.1% |
| negative-rotation-ccd0-3-previous | 40.1% | 51.4% | 28.6% |
| negative-rotation-ccd1-0-current | 82.5% | 88.4% | 76.6% |
| negative-rotation-ccd1-1-previous | 20.3% | 34.5% | 6.1% |
| negative-rotation-ccd1-2-previous | 61.6% | 73.7% | 49.5% |
| negative-rotation-ccd1-3-current | 20.8% | 37.9% | 3.9% |
| negatives-ccd0 | 30.6% | 49.4% | 11.8% |

## Generation on ccd0

Source `160f13a0b90e97b32b6ad363dd8553d079a62009`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 108.19 | 135.45 | 100.81–140.12 | 3832 |
| Numeric | Modules | QRCoder | 124.15 | 158.62 | 88.75–182.12 | 5568 |
| Numeric | Modules | ZXing-C++ | 51.27 | 80.25 | 33.23–89.94 | 1696 |
| Numeric | Modules | ZXing.Net | 66.54 | 110.67 | 54.88–110.97 | 31304 |
| Numeric | Png | CodeGlyphX | 137.68 | 167.60 | 75.18–174.65 | 40312 |
| Numeric | Png | QRCoder | 204.91 | 223.13 | 141.83–237.83 | 6568 |
| Numeric | Png | ZXing-C++ | 927.26 | 1017.79 | 777.98–1031.33 | 848276 |
| Numeric | Png | ZXing.Net | 945.79 | 1025.46 | 751.61–1030.87 | 877605 |
| Unicode | Modules | CodeGlyphX | 1925.79 | 2237.70 | 1614.51–2324.14 | 23872 |
| Unicode | Modules | QRCoder | 3008.85 | 3187.16 | 2733.72–3281.71 | 23216 |
| Unicode | Modules | ZXing-C++ | 1262.19 | 1415.84 | 1137.72–1483.59 | 14648 |
| Unicode | Modules | ZXing.Net | 1954.60 | 2123.88 | 1524.58–2137.17 | 606584 |
| Unicode | Png | CodeGlyphX | 2112.73 | 2162.67 | 1998.95–2165.89 | 274832 |
| Unicode | Png | QRCoder | 3454.18 | 3575.81 | 3290.43–3588.07 | 26112 |
| Unicode | Png | ZXing-C++ | 8541.09 | 9539.70 | 7803.81–9674.88 | 6206726 |
| Unicode | Png | ZXing.Net | 9622.90 | 11479.63 | 8425.11–12275.92 | 6798201 |
| Url | Modules | CodeGlyphX | 896.31 | 1108.25 | 185.74–1203.70 | 5057 |
| Url | Modules | QRCoder | 961.84 | 1162.55 | 554.83–1184.02 | 7408 |
| Url | Modules | ZXing-C++ | 159.73 | 221.66 | 86.24–232.33 | 2709 |
| Url | Modules | ZXing.Net | 791.09 | 1062.40 | 219.69–1089.86 | 71145 |
| Url | Png | CodeGlyphX | 194.18 | 264.11 | 125.21–309.39 | 60278 |
| Url | Png | QRCoder | 479.01 | 532.54 | 408.99–534.73 | 8200 |
| Url | Png | ZXing-C++ | 1430.50 | 1537.33 | 1214.13–1552.92 | 1309067 |
| Url | Png | ZXing.Net | 1506.07 | 1574.85 | 1363.27–1600.18 | 1377538 |

## Generation on ccd1

Source `160f13a0b90e97b32b6ad363dd8553d079a62009`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 73.57 | 92.86 | 59.86–99.52 | 3832 |
| Numeric | Modules | QRCoder | 83.54 | 110.45 | 70.59–120.88 | 5568 |
| Numeric | Modules | ZXing-C++ | 33.85 | 54.32 | 28.45–56.05 | 1696 |
| Numeric | Modules | ZXing.Net | 48.58 | 66.62 | 36.71–67.54 | 31304 |
| Numeric | Png | CodeGlyphX | 94.29 | 123.04 | 77.69–125.12 | 40312 |
| Numeric | Png | QRCoder | 167.72 | 196.33 | 143.32–199.01 | 6568 |
| Numeric | Png | ZXing-C++ | 731.69 | 825.67 | 666.81–834.39 | 848300 |
| Numeric | Png | ZXing.Net | 796.99 | 912.97 | 700.40–939.10 | 877629 |
| Unicode | Modules | CodeGlyphX | 1894.19 | 2700.90 | 1601.46–2896.70 | 23872 |
| Unicode | Modules | QRCoder | 2819.85 | 3792.11 | 2555.80–4061.78 | 23216 |
| Unicode | Modules | ZXing-C++ | 1180.46 | 1585.51 | 1070.05–1710.68 | 14648 |
| Unicode | Modules | ZXing.Net | 1865.30 | 2021.83 | 1616.59–2059.82 | 606584 |
| Unicode | Png | CodeGlyphX | 1854.65 | 1991.37 | 1675.26–2072.50 | 274832 |
| Unicode | Png | QRCoder | 2950.96 | 3239.25 | 2720.63–3250.91 | 26112 |
| Unicode | Png | ZXing-C++ | 7111.68 | 7675.78 | 6724.19–7690.76 | 6206980 |
| Unicode | Png | ZXing.Net | 7956.24 | 8750.99 | 7493.35–9020.64 | 6798484 |
| Url | Modules | CodeGlyphX | 771.93 | 1021.51 | 162.04–1103.61 | 5057 |
| Url | Modules | QRCoder | 983.87 | 1135.90 | 823.37–1161.99 | 7410 |
| Url | Modules | ZXing-C++ | 130.15 | 243.13 | 86.16–312.55 | 2707 |
| Url | Modules | ZXing.Net | 722.41 | 1004.19 | 218.42–1010.77 | 71141 |
| Url | Png | CodeGlyphX | 162.87 | 225.81 | 131.81–231.10 | 60279 |
| Url | Png | QRCoder | 401.84 | 455.67 | 372.39–463.59 | 8200 |
| Url | Png | ZXing-C++ | 1277.21 | 1384.16 | 1154.59–1398.32 | 1309123 |
| Url | Png | ZXing.Net | 1382.56 | 1507.67 | 1245.28–1526.51 | 1377545 |

## Decode latency

The nineteen-image subset includes a negative, a MaxiCode image that was unsupported by the previous source, and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1600.39 | 1691.17 | 1522.32–1710.47 | 87.40 | 19–19 | 0–0 |
| ccd0 | ZXing-C++ | 27.83 | 32.13 | 25.57–32.38 | 2.23 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 194.06 | 234.78 | 176.72–236.66 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1563.34 | 1651.44 | 1439.91–1711.58 | 88.26 | 19–19 | 0–0 |
| ccd1 | ZXing-C++ | 23.73 | 30.64 | 21.62–30.75 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 172.69 | 213.12 | 159.13–229.10 | 12.97 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. The formerly unsupported MaxiCode input, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Current median batch time is 1.2% lower on CCD0, 8.5% higher on CCD1.

Current unfinished image searches occur in 10/24 measured batches, versus 8/24 previously. Every sample and recognition outcome is retained.

The same nineteen-image batch retains a formerly unsupported MaxiCode input, now recognized. Correctness changes alongside measured time; neither a faster failed search nor a slower successful search establishes an equal-accuracy speed ranking.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2460.74 | 2977.10 | 2259.34–3004.63 | 84.58 | 17–18 | 0–0 | 0–1 | 0.988 |
| ccd0 | Candidate | 2431.04 | 3053.14 | 2246.47–3127.10 | 87.05 | 18–19 | 0–0 | 0–1 | 0.988 |
| ccd1 | Baseline | 2244.89 | 3042.06 | 1985.99–3075.53 | 80.23 | 17–18 | 0–0 | 0–1 | 1.085 |
| ccd1 | Candidate | 2436.61 | 3133.41 | 2023.12–3150.95 | 76.06 | 18–19 | 0–0 | 0–1 | 1.085 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
