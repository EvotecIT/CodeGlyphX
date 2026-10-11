# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 219/267 | 6 | 3 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
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

## QR alignment scoring

On .NET 8 and later, alignment scoring stops after four mismatches in its 25-position pattern: that candidate cannot reach the existing 22/25 acceptance threshold. Accepted scores, candidate order, selected centers and first-best ties remain unchanged. Recognition allowances, format hints, payload oracles and scoring are unchanged.

The actual previous and current private methods agree on 67,104 scoring cases, including 10,504 accepted scores, and 330 complete finder searches, including 142 successful searches. There are zero decision or selected-center mismatches. Boundary patterns, inversion, global and adaptive thresholds, fractional coordinates, transformed and edge patterns, noise and first-perfect ties are included.

Public recognition quality and time-limited search completion are measured separately on the unchanged corpus. A faster kernel does not establish portable whole-image speed or recognition accuracy. No public API or production dependency changes.

The [decision diagnostic](evidence/qr-alignment-equivalence.json) records the exact baseline and candidate core hashes. Current whole-image timings and completion counts are reported in the paired decoding comparison below.

## Historical PDF417 search cost

These retained measurements compare source `02f8cfa12c93c0f01ec889d2e0bd6ad1d9b0d272` with `d10503fa973942b31b1e79e293de71a34eba5858`. They qualify the earlier PDF417 optimization and do not measure the current QR candidate.

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
| ccd0 | 288.68 | 295.71 | 1.02x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |
| ccd1 | 272.82 | 271.69 | 1.00x | 21.35 | 21.34 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [26, 30, 28, 29]/31 currently and [25, 23, 30, 30]/31 previously. QR alignment scoring skips candidates that cannot meet the unchanged acceptance threshold. Available search work inside the original allowance can change bounded recognition and completion; the private kernel decision diagnostic does not establish unbounded accuracy gains. Paired nineteen-image costs and nine-request retail costs remain separate, with every sample and recognition failure retained. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by this remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 30.7%, 35.5%, 50.7%. Before the supplementary negative pass: 55.3%, 53.4%, 38.8%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 25/31 | 6 | 0 | 88.4% |
| ccd0 | 2 | current | 26/31 | 5 | 0 | 96.4% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 43.6% |
| ccd0 | 4 | previous | 23/31 | 8 | 0 | 34.0% |
| ccd1 | 1 | current | 28/31 | 3 | 0 | 99.8% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 70.3% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 81.6% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 28.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 38.9% | 56.3% | 21.7% |
| decoding-ccd0 | 52.7% | 62.7% | 42.7% |
| decoding-ccd1 | 37.4% | 47.1% | 27.7% |
| diagnostic-final-retail-negative-current | 44.8% | 56.8% | 32.8% |
| diagnostic-final-retail-negative-previous | 72.5% | 78.9% | 66.2% |
| diagnostic-final-retail-positive-current | 37.3% | 52.9% | 21.7% |
| diagnostic-final-retail-positive-previous | 85.0% | 89.5% | 80.5% |
| generation-ccd0 | 31.2% | 46.8% | 15.6% |
| generation-ccd1 | 89.2% | 92.2% | 86.2% |
| holdout-current | 47.9% | 59.4% | 36.3% |
| negative-rotation-ccd0-0-previous | 88.4% | 90.7% | 86.0% |
| negative-rotation-ccd0-1-current | 96.4% | 97.2% | 95.5% |
| negative-rotation-ccd0-2-current | 43.6% | 62.3% | 24.9% |
| negative-rotation-ccd0-3-previous | 34.0% | 53.6% | 14.4% |
| negative-rotation-ccd1-0-current | 99.8% | 99.9% | 99.8% |
| negative-rotation-ccd1-1-previous | 70.3% | 81.6% | 59.1% |
| negative-rotation-ccd1-2-previous | 81.6% | 90.5% | 72.7% |
| negative-rotation-ccd1-3-current | 28.7% | 45.3% | 12.1% |
| negatives-ccd0 | 49.2% | 62.6% | 35.8% |

## Generation on ccd0

Source `941f63e9b668f86a786e8206e75fd33c4f823498`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 84.53 | 102.38 | 60.30–112.30 | 3832 |
| Numeric | Modules | QRCoder | 102.06 | 134.45 | 77.17–134.90 | 5568 |
| Numeric | Modules | ZXing-C++ | 40.93 | 65.03 | 28.09–75.19 | 1696 |
| Numeric | Modules | ZXing.Net | 50.58 | 74.80 | 40.07–76.58 | 31304 |
| Numeric | Png | CodeGlyphX | 110.69 | 139.91 | 82.39–158.93 | 40312 |
| Numeric | Png | QRCoder | 186.38 | 314.35 | 149.15–378.24 | 6568 |
| Numeric | Png | ZXing-C++ | 857.46 | 966.85 | 738.26–1033.19 | 848308 |
| Numeric | Png | ZXing.Net | 913.61 | 1034.14 | 799.43–1039.60 | 877585 |
| Unicode | Modules | CodeGlyphX | 1927.42 | 2598.34 | 1717.83–2668.07 | 23872 |
| Unicode | Modules | QRCoder | 3061.05 | 3542.46 | 2869.55–3811.85 | 23216 |
| Unicode | Modules | ZXing-C++ | 1287.39 | 1530.91 | 1174.76–1734.15 | 14648 |
| Unicode | Modules | ZXing.Net | 2050.64 | 2306.06 | 1762.71–2335.82 | 606585 |
| Unicode | Png | CodeGlyphX | 2303.77 | 3778.11 | 1988.80–3992.57 | 274832 |
| Unicode | Png | QRCoder | 3458.69 | 5752.74 | 3147.04–8034.12 | 26336 |
| Unicode | Png | ZXing-C++ | 9308.68 | 21132.07 | 7675.35–23561.69 | 6206887 |
| Unicode | Png | ZXing.Net | 11285.83 | 24580.25 | 9481.95–24754.89 | 6798289 |
| Url | Modules | CodeGlyphX | 762.15 | 1004.89 | 659.92–1047.02 | 5057 |
| Url | Modules | QRCoder | 928.57 | 1090.25 | 818.53–1117.71 | 7409 |
| Url | Modules | ZXing-C++ | 129.95 | 187.17 | 70.68–188.17 | 2711 |
| Url | Modules | ZXing.Net | 776.93 | 1002.75 | 650.81–1050.90 | 71146 |
| Url | Png | CodeGlyphX | 154.30 | 251.85 | 125.11–279.46 | 60278 |
| Url | Png | QRCoder | 419.66 | 464.12 | 386.64–480.04 | 8200 |
| Url | Png | ZXing-C++ | 1315.45 | 1426.21 | 1213.29–1460.01 | 1308998 |
| Url | Png | ZXing.Net | 1354.54 | 1498.12 | 1268.53–1559.50 | 1377465 |

## Generation on ccd1

Source `941f63e9b668f86a786e8206e75fd33c4f823498`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 109.92 | 193.73 | 100.74–195.05 | 3832 |
| Numeric | Modules | QRCoder | 167.13 | 253.10 | 112.75–263.40 | 5568 |
| Numeric | Modules | ZXing-C++ | 51.64 | 112.63 | 37.29–154.60 | 1696 |
| Numeric | Modules | ZXing.Net | 85.89 | 125.07 | 42.20–129.30 | 31304 |
| Numeric | Png | CodeGlyphX | 138.17 | 281.70 | 108.66–322.19 | 40312 |
| Numeric | Png | QRCoder | 233.57 | 375.76 | 186.57–390.77 | 6568 |
| Numeric | Png | ZXing-C++ | 1043.65 | 1257.31 | 969.93–1269.58 | 848310 |
| Numeric | Png | ZXing.Net | 1140.44 | 1399.22 | 903.79–1424.01 | 877592 |
| Unicode | Modules | CodeGlyphX | 1942.85 | 2235.85 | 1779.94–2272.28 | 23872 |
| Unicode | Modules | QRCoder | 3065.19 | 3685.66 | 2780.80–3830.28 | 23216 |
| Unicode | Modules | ZXing-C++ | 1251.13 | 1349.65 | 1111.70–1414.52 | 14648 |
| Unicode | Modules | ZXing.Net | 1858.46 | 2237.86 | 1671.88–2270.30 | 606585 |
| Unicode | Png | CodeGlyphX | 2174.26 | 2962.02 | 2038.77–3222.98 | 274832 |
| Unicode | Png | QRCoder | 3538.87 | 4880.71 | 3260.48–6174.75 | 26112 |
| Unicode | Png | ZXing-C++ | 8565.94 | 9875.77 | 7679.03–10165.62 | 6206542 |
| Unicode | Png | ZXing.Net | 9682.89 | 13023.90 | 8894.60–13241.05 | 6798232 |
| Url | Modules | CodeGlyphX | 1139.92 | 2174.23 | 463.89–2281.63 | 5057 |
| Url | Modules | QRCoder | 1396.01 | 2149.96 | 544.76–2219.18 | 7399 |
| Url | Modules | ZXing-C++ | 266.71 | 648.40 | 117.71–704.64 | 2707 |
| Url | Modules | ZXing.Net | 1148.22 | 1625.76 | 438.18–1770.07 | 71125 |
| Url | Png | CodeGlyphX | 350.91 | 681.23 | 217.75–769.42 | 60286 |
| Url | Png | QRCoder | 841.62 | 1987.04 | 551.75–2451.00 | 8244 |
| Url | Png | ZXing-C++ | 2570.35 | 4698.87 | 1867.60–5627.65 | 1309048 |
| Url | Png | ZXing.Net | 3003.21 | 3874.34 | 1861.04–3970.47 | 1377555 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1727.55 | 2005.21 | 1471.93–2019.67 | 86.08 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 27.95 | 39.90 | 22.61–42.77 | 2.21 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 208.94 | 270.85 | 175.02–275.55 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1669.42 | 1807.49 | 1573.06–1825.21 | 86.17 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 28.89 | 56.50 | 21.55–78.10 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 206.46 | 323.83 | 176.96–374.25 | 13.19 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Unsupported MaxiCode, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

In this round, current median batch time is 5.1% lower on CCD0, 0.4% lower on CCD1.

The current binary has an unfinished image search in 9/24 measured batches, versus 12/24 for the previous binary. All samples and outcomes remain available; a faster unfinished search does not count as a successful rejection.

Actual QR alignment decisions are checked separately. Whole-image costs retain recognition failures and unfinished searches; unchanged retail costs retain shared-host variation. None establishes a portable performance change.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2826.49 | 3050.06 | 2445.58–3189.83 | 77.02 | 17–18 | 0–0 | 0–1 | 0.949 |
| ccd0 | Candidate | 2682.35 | 2990.65 | 2382.92–3026.56 | 83.99 | 17–18 | 0–0 | 0–1 | 0.949 |
| ccd1 | Baseline | 2364.93 | 2844.89 | 2123.41–3188.42 | 84.53 | 17–18 | 0–0 | 0–1 | 0.996 |
| ccd1 | Candidate | 2356.63 | 2523.48 | 2253.45–2579.27 | 86.85 | 17–18 | 0–0 | 0–1 | 0.996 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
