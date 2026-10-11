# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 219/267 | 6 | 3 | 0 | 1 |
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
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 32 | 34 | 34 |
| UpcA | 36 | 0 | 26 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 219/267 correct positive images, compared with 219/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

## Data Matrix deadline lookup

Bounding-box search captures its fixed enclosing deadline once per synchronous call. It still checks caller cancellation and the monotonic clock at every original row and pixel position. Pixel arithmetic, polarity and bounds are unchanged, as are recognition allowances, format hints, payload oracles and scoring.

The actual previous and current private methods agree on 1,024 cases, with zero mismatches. The diagnostic covers RGBA and BGRA pixels, transparency, padded strides, inversion, blank, solid and random images, live and expired ambient clocks, and cancelled tokens.

This decision diagnostic does not prove mid-loop timing. Full scanner deadlines, recognition outcomes and whole-image cost are qualified separately. Shared-host load and bounded search affect results. No public API or production dependency changes.

The [decision diagnostic](evidence/datamatrix-box-equivalence.json) records the exact baseline and candidate core hashes. Current whole-image timings and completion counts are reported in the paired decoding comparison below.

Actual private bounding-box methods on the same seeded 1024x768 RGBA image and live 60000ms ambient clock. Strongly typed delegates; identical reflection scope setup and result boxing included. Three warmups, twelve grouped rotated measurements per engine on each cache domain; all samples retained. Input creation and validation excluded. Managed allocation includes the shared benchmark wrapper and is not a bare-kernel allocation measurement. This is a kernel diagnostic, not whole-scanner cost or recognition accuracy.

| Domain | Previous kernel ms | Current kernel ms | Current / previous | Previous managed bytes | Current managed bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 31.491 | 21.900 | 0.695 | 2,071,387 | 2,066,703 |
| CCD1 | 27.872 | 20.083 | 0.721 | 2,068,174 | 2,070,263 |

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
| ccd0 | 438.36 | 345.23 | 0.79x | 21.37 | 21.39 | 1.00x | 9/9 → 9/9 |
| ccd1 | 402.92 | 382.93 | 0.95x | 21.35 | 21.34 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [29, 30, 30, 29]/31 currently and [29, 30, 30, 30]/31 previously. Data Matrix bounding-box search avoids repeated ambient deadline lookup while keeping every original token and clock check. Available search work inside the original allowance can change bounded recognition and completion; the private kernel decision diagnostic does not establish unbounded accuracy gains. Paired nineteen-image costs and nine-request retail costs remain separate, with every sample and recognition failure retained. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by this remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 46.0%, 45.7%, 58.1%. Before the supplementary negative pass: 82.3%, 83.6%, 85.6%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 23/25 | 0 | 2 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 23/25 supplementary negatives versus 25/25. Previously completed images now unfinished: falsepositives-2/01.png, falsepositives-2/09.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 93.3% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 42.2% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 27.7% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 38.6% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 41.9% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 31.3% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 59.6% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 32.3% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 49.9% | 64.8% | 35.0% |
| decoding-ccd0 | 65.4% | 76.4% | 54.4% |
| decoding-ccd1 | 84.8% | 89.8% | 79.8% |
| diagnostic-final-retail-negative-current | 66.2% | 74.7% | 57.7% |
| diagnostic-final-retail-negative-previous | 96.1% | 97.1% | 95.1% |
| diagnostic-final-retail-positive-current | 61.9% | 71.2% | 52.8% |
| diagnostic-final-retail-positive-previous | 77.7% | 82.9% | 72.5% |
| generation-ccd0 | 49.5% | 62.0% | 37.1% |
| generation-ccd1 | 32.8% | 47.6% | 18.0% |
| holdout-current | 95.4% | 96.9% | 93.9% |
| negative-rotation-ccd0-0-previous | 93.3% | 96.0% | 90.6% |
| negative-rotation-ccd0-1-current | 42.2% | 56.3% | 27.9% |
| negative-rotation-ccd0-2-current | 27.7% | 41.7% | 13.7% |
| negative-rotation-ccd0-3-previous | 38.6% | 54.4% | 22.8% |
| negative-rotation-ccd1-0-current | 41.9% | 55.4% | 28.3% |
| negative-rotation-ccd1-1-previous | 31.3% | 45.8% | 17.3% |
| negative-rotation-ccd1-2-previous | 59.6% | 68.8% | 50.3% |
| negative-rotation-ccd1-3-current | 32.3% | 44.9% | 19.7% |
| negatives-ccd0 | 83.8% | 89.1% | 78.5% |

## Generation on ccd0

Source `b006759fea1017ffb9940b9251a2d201b29b0615`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 80.07 | 103.42 | 64.01–107.06 | 3832 |
| Numeric | Modules | QRCoder | 89.81 | 127.56 | 72.11–132.37 | 5568 |
| Numeric | Modules | ZXing-C++ | 42.56 | 71.25 | 28.47–74.61 | 1696 |
| Numeric | Modules | ZXing.Net | 54.79 | 86.37 | 36.86–90.25 | 31304 |
| Numeric | Png | CodeGlyphX | 104.63 | 158.16 | 84.12–159.12 | 40312 |
| Numeric | Png | QRCoder | 186.63 | 258.62 | 155.65–300.43 | 6568 |
| Numeric | Png | ZXing-C++ | 864.28 | 970.54 | 794.67–1005.91 | 848319 |
| Numeric | Png | ZXing.Net | 897.88 | 996.56 | 787.50–1024.65 | 877591 |
| Unicode | Modules | CodeGlyphX | 1889.06 | 2443.68 | 1760.93–2606.42 | 23872 |
| Unicode | Modules | QRCoder | 3225.43 | 4179.28 | 2729.41–4350.10 | 23216 |
| Unicode | Modules | ZXing-C++ | 1253.36 | 2173.04 | 1092.42–3017.98 | 14648 |
| Unicode | Modules | ZXing.Net | 1967.36 | 2432.65 | 1763.30–2651.32 | 606584 |
| Unicode | Png | CodeGlyphX | 2101.48 | 2405.73 | 1921.93–2505.57 | 274942 |
| Unicode | Png | QRCoder | 3498.84 | 4030.77 | 3173.99–4042.62 | 26792 |
| Unicode | Png | ZXing-C++ | 9013.73 | 10820.13 | 7633.31–12280.05 | 6207130 |
| Unicode | Png | ZXing.Net | 10787.50 | 16388.36 | 9545.85–17445.88 | 6798607 |
| Url | Modules | CodeGlyphX | 894.97 | 1142.91 | 148.92–1146.43 | 5057 |
| Url | Modules | QRCoder | 1140.19 | 1276.96 | 353.72–1296.55 | 7409 |
| Url | Modules | ZXing-C++ | 181.00 | 316.31 | 89.73–330.44 | 2709 |
| Url | Modules | ZXing.Net | 896.55 | 1091.73 | 229.68–1125.97 | 71141 |
| Url | Png | CodeGlyphX | 169.77 | 307.42 | 146.76–354.25 | 60278 |
| Url | Png | QRCoder | 482.02 | 531.55 | 389.50–533.68 | 8200 |
| Url | Png | ZXing-C++ | 1482.26 | 1624.00 | 1311.80–1637.55 | 1309022 |
| Url | Png | ZXing.Net | 1558.04 | 1678.68 | 1319.30–1686.07 | 1377469 |

## Generation on ccd1

Source `b006759fea1017ffb9940b9251a2d201b29b0615`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 103.78 | 122.84 | 63.66–134.50 | 3832 |
| Numeric | Modules | QRCoder | 123.08 | 144.82 | 86.04–152.06 | 5569 |
| Numeric | Modules | ZXing-C++ | 44.75 | 63.90 | 30.17–65.78 | 1696 |
| Numeric | Modules | ZXing.Net | 74.69 | 97.58 | 48.40–97.95 | 31304 |
| Numeric | Png | CodeGlyphX | 146.53 | 174.10 | 111.38–182.91 | 40326 |
| Numeric | Png | QRCoder | 205.03 | 232.21 | 193.18–236.81 | 6653 |
| Numeric | Png | ZXing-C++ | 947.40 | 1019.28 | 783.41–1024.72 | 848350 |
| Numeric | Png | ZXing.Net | 936.22 | 998.42 | 828.03–1035.16 | 877635 |
| Unicode | Modules | CodeGlyphX | 2016.83 | 2316.36 | 1698.90–2328.74 | 23872 |
| Unicode | Modules | QRCoder | 3087.80 | 3638.38 | 2832.57–3660.60 | 23220 |
| Unicode | Modules | ZXing-C++ | 1272.92 | 1737.84 | 1172.66–2119.13 | 14648 |
| Unicode | Modules | ZXing.Net | 1860.75 | 2496.03 | 1724.80–2784.52 | 606586 |
| Unicode | Png | CodeGlyphX | 2148.96 | 3131.99 | 1965.17–3600.26 | 274942 |
| Unicode | Png | QRCoder | 3651.16 | 5023.99 | 3221.04–5654.62 | 26778 |
| Unicode | Png | ZXing-C++ | 9135.12 | 10272.50 | 7040.80–10407.67 | 6207211 |
| Unicode | Png | ZXing.Net | 10324.75 | 14566.56 | 7951.30–15341.59 | 6798698 |
| Url | Modules | CodeGlyphX | 1000.24 | 1209.19 | 210.53–1273.21 | 5057 |
| Url | Modules | QRCoder | 1239.48 | 1270.13 | 492.94–1274.78 | 7412 |
| Url | Modules | ZXing-C++ | 168.43 | 230.23 | 101.22–259.37 | 2709 |
| Url | Modules | ZXing.Net | 881.03 | 1064.64 | 242.18–1085.92 | 71136 |
| Url | Png | CodeGlyphX | 201.89 | 387.24 | 142.47–448.32 | 60305 |
| Url | Png | QRCoder | 504.92 | 613.98 | 425.04–617.46 | 8375 |
| Url | Png | ZXing-C++ | 1566.26 | 1757.00 | 1379.24–1768.82 | 1309234 |
| Url | Png | ZXing.Net | 1675.09 | 1836.42 | 1464.93–1860.94 | 1377686 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1669.58 | 1803.18 | 1547.42–1839.41 | 91.99 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 27.85 | 54.71 | 20.01–67.10 | 2.21 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 207.61 | 271.20 | 169.99–304.52 | 13.01 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1622.73 | 1794.28 | 1520.31–1825.66 | 92.37 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 26.82 | 28.98 | 23.11–29.05 | 2.23 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 204.84 | 229.70 | 180.68–246.14 | 13.00 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Unsupported MaxiCode, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

In this round, current median batch time is 5.5% lower on CCD0, 7.6% lower on CCD1.

The current binary has an unfinished image search in 9/24 measured batches, versus 15/24 for the previous binary. All samples and outcomes remain available; a faster unfinished search does not count as a successful rejection.

Actual Data Matrix bounding-box decisions are checked separately. Whole-image costs retain recognition failures and unfinished searches; unchanged retail costs retain shared-host variation. None establishes a portable performance change.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2801.81 | 2946.70 | 2274.80–3022.29 | 77.10 | 17–18 | 0–0 | 0–1 | 0.945 |
| ccd0 | Candidate | 2648.81 | 2920.07 | 2331.67–2953.18 | 80.60 | 17–18 | 0–0 | 0–1 | 0.945 |
| ccd1 | Baseline | 2568.61 | 2753.81 | 2239.82–2755.35 | 85.97 | 17–18 | 0–0 | 0–1 | 0.924 |
| ccd1 | Candidate | 2374.15 | 2773.27 | 1956.10–2795.34 | 86.51 | 17–18 | 0–0 | 0–1 | 0.924 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
