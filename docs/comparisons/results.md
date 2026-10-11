# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 219/267 | 6 | 3 | 0 | 3 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 9 | 10 | 12 |
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

This decoding follow-up records 219/267 correct positive images, compared with 208/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The 38-image selection was fixed before the first supplemental outcomes. One revealed regression informed the final correction and permanent fixture; the final set is supplemental regression evidence, not an unseen holdout.

Supplemental images from the same upstream collection, outside the 18 frozen comparison images. The initial check exposed a sampling-priority regression that was repaired and added to permanent tests. This is regression evidence used during remediation, not an unseen holdout for the final candidate, a replacement ranking, or proof on an independent dataset. Wall-clock budgets also limit attribution of changed outcomes.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 34/38 | 34/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: none. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

## Retail recovery and search cost

Supplementary contract diagnostics, not a representative accuracy or negative-rate survey. Thirty-one fixed negative IDs are each requested with three explicit retail-family hints (93 requests).

The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.

| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 53/87 | 64/87 | 0 |
| ZXing.Net | 55/87 | 55/87 | 2 |
| ZXing-C++ | 54/87 | 54/87 | 1 |

Each of 31 fixed negative images is scanned with EAN, UPC-A and UPC-E hints: 93 requests, rather than 93 independent images.

| Reader | Previous correct rejection | Current correct rejection | Current unexpected output | Current inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 92/93 | 93/93 | 0 | 0 |
| ZXing.Net | 89/93 | 89/93 | 4 | 0 |
| ZXing-C++ | 93/93 | 93/93 | 0 | 0 |

Nine prespecified requests: noise 03, stripes 14 and texture 19, each with EAN, UPC-A and UPC-E hints. PowerForge owns timing: 3 warmups, 12 grouped rotated measurements, no outlier removal, fixed Normal priority/affinity. Baseline and candidate use separate assembly contexts with identical preloaded RGBA32 and options. Loading and validation are excluded. Extra search still costs time and allocation; all samples and losses are retained. Shared-host contention prevents portable latency claims.

| Domain | Previous median ms / nine requests | Current median ms / nine requests | Latency ratio | Previous mean managed MiB | Current mean managed MiB | Allocation ratio | Previous/current correct requests |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | 166.67 | 314.89 | 1.89x | 29.91 | 52.17 | 1.74x | 8/9 → 9/9 |
| ccd1 | 159.29 | 298.03 | 1.87x | 29.91 | 52.17 | 1.74x | 8/9 → 9/9 |

The previous binary invents a UPC-E payload on one of these requests. Correctness differs, and extra recovery work costs time and allocation. These measurements do not establish a speed gain or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 208/267 previously; gained: upca-1/8.png, upca-1/4.png, upca-2/46.png, upca-2/07.png, upca-3/06.png, upca-4/14.png, upca-4/19.png, upca-4/7.png, upca-5/33.png, upce-2/41.png, upce-2/35.png; previously correct positive images lost: none. Paired negative completion is [29, 24, 30, 30]/31 currently and [29, 28, 30, 30]/31 previously. Retail fallback costs are measured separately and remain visible. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 100.0%, 100.0%, 99.9%. Before the supplementary negative pass: 39.6%, 37.6%, 31.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 21/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 62.9% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 31.0% |
| ccd0 | 3 | current | 24/31 | 7 | 0 | 98.2% |
| ccd0 | 4 | previous | 28/31 | 3 | 0 | 92.7% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 24.8% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 36.6% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 44.1% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 68.3% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 100.0% | 100.0% | 100.0% |
| decoding-ccd0 | 54.9% | 75.6% | 34.2% |
| decoding-ccd1 | 42.8% | 58.8% | 26.8% |
| diagnostic-final-retail-negative | 50.2% | 60.7% | 39.7% |
| diagnostic-final-retail-positive | 68.4% | 77.7% | 59.1% |
| generation-ccd0 | 61.6% | 72.7% | 50.5% |
| generation-ccd1 | 81.6% | 87.8% | 75.2% |
| holdout-current | 51.8% | 63.2% | 40.3% |
| negative-rotation-ccd0-0-previous | 62.9% | 73.4% | 52.4% |
| negative-rotation-ccd0-1-current | 31.0% | 46.2% | 15.8% |
| negative-rotation-ccd0-2-current | 98.2% | 99.1% | 97.2% |
| negative-rotation-ccd0-3-previous | 92.7% | 95.9% | 89.5% |
| negative-rotation-ccd1-0-current | 24.8% | 41.3% | 8.4% |
| negative-rotation-ccd1-1-previous | 36.6% | 51.1% | 22.1% |
| negative-rotation-ccd1-2-previous | 44.1% | 61.2% | 27.4% |
| negative-rotation-ccd1-3-current | 68.3% | 76.8% | 59.8% |
| negatives-ccd0 | 36.3% | 53.8% | 18.8% |

## Generation on ccd0

Source `344a78dcc2f20946f0ba87890d8607767a5f3f83`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 106.16 | 155.48 | 86.04–178.95 | 3832 |
| Numeric | Modules | QRCoder | 150.55 | 221.59 | 97.62–250.23 | 5568 |
| Numeric | Modules | ZXing-C++ | 42.91 | 83.81 | 28.90–84.38 | 1696 |
| Numeric | Modules | ZXing.Net | 68.03 | 121.53 | 44.37–145.15 | 31304 |
| Numeric | Png | CodeGlyphX | 117.45 | 188.32 | 82.29–193.64 | 40312 |
| Numeric | Png | QRCoder | 245.20 | 353.82 | 177.70–445.17 | 6568 |
| Numeric | Png | ZXing-C++ | 923.48 | 1440.18 | 789.32–1825.73 | 848314 |
| Numeric | Png | ZXing.Net | 951.18 | 1626.74 | 869.54–2263.05 | 877600 |
| Unicode | Modules | CodeGlyphX | 1951.02 | 2847.37 | 1794.66–3548.71 | 23872 |
| Unicode | Modules | QRCoder | 2988.44 | 3346.25 | 2742.10–3398.58 | 23216 |
| Unicode | Modules | ZXing-C++ | 1287.71 | 2433.30 | 1166.92–3179.45 | 14648 |
| Unicode | Modules | ZXing.Net | 2082.50 | 2826.36 | 1758.63–2912.37 | 606584 |
| Unicode | Png | CodeGlyphX | 2052.21 | 2305.82 | 1860.95–2501.04 | 274832 |
| Unicode | Png | QRCoder | 3398.23 | 4537.25 | 3035.65–5514.82 | 26112 |
| Unicode | Png | ZXing-C++ | 8725.02 | 9609.87 | 7699.72–10002.92 | 6206879 |
| Unicode | Png | ZXing.Net | 10243.25 | 11944.73 | 9075.81–12693.11 | 6798182 |
| Url | Modules | CodeGlyphX | 911.23 | 1021.20 | 135.65–1028.68 | 5057 |
| Url | Modules | QRCoder | 1175.80 | 1254.78 | 508.17–1260.26 | 7409 |
| Url | Modules | ZXing-C++ | 166.60 | 249.61 | 87.31–254.39 | 2707 |
| Url | Modules | ZXing.Net | 918.36 | 1208.15 | 278.85–1237.88 | 71142 |
| Url | Png | CodeGlyphX | 188.93 | 330.52 | 153.50–378.43 | 60278 |
| Url | Png | QRCoder | 455.93 | 522.67 | 405.95–564.00 | 8200 |
| Url | Png | ZXing-C++ | 1460.46 | 1738.96 | 1372.83–1768.57 | 1309033 |
| Url | Png | ZXing.Net | 1577.57 | 1770.39 | 1422.30–1805.96 | 1377457 |

## Generation on ccd1

Source `344a78dcc2f20946f0ba87890d8607767a5f3f83`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 72.58 | 99.51 | 59.61–99.78 | 3832 |
| Numeric | Modules | QRCoder | 100.11 | 142.66 | 80.52–170.25 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.30 | 80.17 | 29.89–90.17 | 1696 |
| Numeric | Modules | ZXing.Net | 56.14 | 90.71 | 40.88–97.72 | 31304 |
| Numeric | Png | CodeGlyphX | 104.96 | 133.45 | 80.96–143.82 | 40312 |
| Numeric | Png | QRCoder | 160.96 | 183.04 | 147.87–191.04 | 6568 |
| Numeric | Png | ZXing-C++ | 800.54 | 867.19 | 735.28–892.45 | 848303 |
| Numeric | Png | ZXing.Net | 809.05 | 869.97 | 728.40–872.69 | 877629 |
| Unicode | Modules | CodeGlyphX | 1636.95 | 1867.08 | 1604.73–1926.39 | 23872 |
| Unicode | Modules | QRCoder | 2562.21 | 3056.64 | 2446.90–3114.67 | 23216 |
| Unicode | Modules | ZXing-C++ | 1095.92 | 1263.01 | 1053.07–1337.49 | 14648 |
| Unicode | Modules | ZXing.Net | 1671.61 | 1773.12 | 1581.25–1802.92 | 606584 |
| Unicode | Png | CodeGlyphX | 2505.96 | 3776.26 | 1792.20–4327.28 | 274832 |
| Unicode | Png | QRCoder | 3895.33 | 4982.93 | 2961.96–5308.56 | 26112 |
| Unicode | Png | ZXing-C++ | 10439.22 | 12639.87 | 7412.60–12941.64 | 6206762 |
| Unicode | Png | ZXing.Net | 12109.56 | 17104.37 | 9344.44–19283.00 | 6798230 |
| Url | Modules | CodeGlyphX | 925.03 | 1052.69 | 144.76–1093.53 | 5057 |
| Url | Modules | QRCoder | 990.24 | 1362.46 | 676.75–1706.61 | 7412 |
| Url | Modules | ZXing-C++ | 143.21 | 287.73 | 72.16–296.31 | 2707 |
| Url | Modules | ZXing.Net | 796.80 | 936.29 | 175.12–1017.67 | 71141 |
| Url | Png | CodeGlyphX | 207.40 | 293.42 | 136.45–306.82 | 60278 |
| Url | Png | QRCoder | 437.81 | 696.83 | 402.58–977.38 | 8200 |
| Url | Png | ZXing-C++ | 1374.77 | 1977.67 | 1249.88–2266.78 | 1309049 |
| Url | Png | ZXing.Net | 1448.99 | 2150.71 | 1318.22–2352.49 | 1377469 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1651.05 | 1741.32 | 1604.67–1754.01 | 92.54 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 28.05 | 30.66 | 24.59–31.87 | 2.20 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 198.35 | 221.71 | 188.77–221.86 | 12.96 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1558.39 | 1904.43 | 1492.21–1991.39 | 97.83 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 25.91 | 36.82 | 22.29–37.17 | 2.23 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 182.78 | 274.93 | 156.25–306.08 | 13.00 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
