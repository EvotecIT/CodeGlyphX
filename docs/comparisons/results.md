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
| ccd0 | 314.09 | 260.79 | 0.83x | 52.16 | 21.35 | 0.41x | 9/9 → 9/9 |
| ccd1 | 341.12 | 291.10 | 0.85x | 52.14 | 21.34 | 0.41x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [30, 30, 30, 30]/31 currently and [30, 30, 30, 30]/31 previously. Retail search costs are measured separately and remain visible. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 38.4%, 25.9%, 59.0%. Before the supplementary negative pass: 17.6%, 20.4%, 22.2%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 25.3% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 19.0% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 30.4% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 33.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 34.0% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 22.6% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 27.4% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 25.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 41.1% | 54.4% | 27.8% |
| decoding-ccd0 | 56.1% | 69.8% | 42.3% |
| decoding-ccd1 | 88.1% | 92.3% | 83.9% |
| diagnostic-final-retail-negative | 53.2% | 65.5% | 41.0% |
| diagnostic-final-retail-positive | 26.6% | 44.0% | 9.1% |
| generation-ccd0 | 47.0% | 59.9% | 34.1% |
| generation-ccd1 | 44.3% | 58.1% | 30.6% |
| holdout-current | 51.3% | 66.1% | 36.4% |
| negative-rotation-ccd0-0-previous | 25.3% | 41.7% | 8.8% |
| negative-rotation-ccd0-1-current | 19.0% | 33.6% | 4.7% |
| negative-rotation-ccd0-2-current | 30.4% | 45.2% | 15.6% |
| negative-rotation-ccd0-3-previous | 33.4% | 47.3% | 19.5% |
| negative-rotation-ccd1-0-current | 34.0% | 47.7% | 20.2% |
| negative-rotation-ccd1-1-previous | 22.6% | 37.5% | 7.8% |
| negative-rotation-ccd1-2-previous | 27.4% | 44.3% | 10.5% |
| negative-rotation-ccd1-3-current | 25.5% | 43.5% | 7.7% |
| negatives-ccd0 | 20.1% | 34.4% | 5.7% |

## Generation on ccd0

Source `3227d9c58f4991e5cee3fe588daf9e9d9b048b80`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 64.88 | 112.25 | 59.95–125.91 | 3832 |
| Numeric | Modules | QRCoder | 92.87 | 139.18 | 70.73–173.38 | 5568 |
| Numeric | Modules | ZXing-C++ | 32.15 | 57.39 | 28.52–58.67 | 1696 |
| Numeric | Modules | ZXing.Net | 52.82 | 89.80 | 37.80–90.49 | 31304 |
| Numeric | Png | CodeGlyphX | 114.62 | 164.74 | 77.34–173.73 | 40340 |
| Numeric | Png | QRCoder | 157.84 | 171.27 | 147.00–176.39 | 6737 |
| Numeric | Png | ZXing-C++ | 852.62 | 1057.94 | 680.24–1072.85 | 848461 |
| Numeric | Png | ZXing.Net | 936.02 | 1222.62 | 720.60–1248.46 | 877795 |
| Unicode | Modules | CodeGlyphX | 1844.59 | 2240.54 | 1549.80–2356.16 | 23872 |
| Unicode | Modules | QRCoder | 3067.34 | 4223.39 | 2522.01–5084.12 | 23216 |
| Unicode | Modules | ZXing-C++ | 1117.26 | 2081.56 | 1020.61–2968.03 | 14648 |
| Unicode | Modules | ZXing.Net | 1728.75 | 3335.42 | 1559.81–4426.91 | 606585 |
| Unicode | Png | CodeGlyphX | 1733.39 | 1966.03 | 1609.42–1993.27 | 274832 |
| Unicode | Png | QRCoder | 2815.74 | 3354.03 | 2691.42–3372.84 | 26112 |
| Unicode | Png | ZXing-C++ | 6378.27 | 7765.21 | 6049.71–8002.75 | 6206810 |
| Unicode | Png | ZXing.Net | 7815.86 | 9193.95 | 7159.74–9382.01 | 6798225 |
| Url | Modules | CodeGlyphX | 998.18 | 1251.33 | 146.17–1261.36 | 5057 |
| Url | Modules | QRCoder | 1151.39 | 1378.67 | 571.24–1388.66 | 7414 |
| Url | Modules | ZXing-C++ | 165.27 | 235.42 | 59.00–245.45 | 2707 |
| Url | Modules | ZXing.Net | 924.25 | 1177.08 | 187.41–1293.57 | 71140 |
| Url | Png | CodeGlyphX | 143.14 | 248.65 | 129.98–316.31 | 60305 |
| Url | Png | QRCoder | 392.63 | 444.49 | 369.25–447.05 | 8381 |
| Url | Png | ZXing-C++ | 1339.98 | 1750.05 | 1191.94–1761.15 | 1309305 |
| Url | Png | ZXing.Net | 1403.24 | 1747.43 | 1332.36–1812.82 | 1377667 |

## Generation on ccd1

Source `3227d9c58f4991e5cee3fe588daf9e9d9b048b80`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 60.28 | 87.90 | 58.83–87.99 | 3832 |
| Numeric | Modules | QRCoder | 89.41 | 144.41 | 77.08–180.31 | 5568 |
| Numeric | Modules | ZXing-C++ | 39.57 | 65.22 | 28.39–79.76 | 1696 |
| Numeric | Modules | ZXing.Net | 45.26 | 108.10 | 36.97–126.97 | 31304 |
| Numeric | Png | CodeGlyphX | 76.93 | 104.80 | 76.11–111.13 | 40312 |
| Numeric | Png | QRCoder | 146.14 | 150.61 | 144.31–151.34 | 6568 |
| Numeric | Png | ZXing-C++ | 635.02 | 656.58 | 617.04–659.78 | 848295 |
| Numeric | Png | ZXing.Net | 670.54 | 699.73 | 627.68–703.27 | 877604 |
| Unicode | Modules | CodeGlyphX | 1470.76 | 1511.49 | 1450.81–1527.76 | 23872 |
| Unicode | Modules | QRCoder | 2383.01 | 2482.23 | 2326.89–2515.77 | 23216 |
| Unicode | Modules | ZXing-C++ | 983.92 | 1008.71 | 970.33–1013.95 | 14648 |
| Unicode | Modules | ZXing.Net | 1440.66 | 1492.17 | 1408.60–1492.64 | 606584 |
| Unicode | Png | CodeGlyphX | 2120.40 | 3274.21 | 1613.85–3737.91 | 274832 |
| Unicode | Png | QRCoder | 3414.66 | 3849.87 | 2734.04–3938.40 | 26112 |
| Unicode | Png | ZXing-C++ | 8559.70 | 11256.54 | 6741.73–12963.14 | 6206859 |
| Unicode | Png | ZXing.Net | 10049.28 | 12711.27 | 7072.11–13007.95 | 6798180 |
| Url | Modules | CodeGlyphX | 753.63 | 1268.30 | 166.29–1368.28 | 5057 |
| Url | Modules | QRCoder | 1065.16 | 1427.86 | 915.74–1538.19 | 7409 |
| Url | Modules | ZXing-C++ | 164.06 | 263.77 | 101.83–284.51 | 2709 |
| Url | Modules | ZXing.Net | 848.45 | 977.63 | 653.75–982.88 | 71146 |
| Url | Png | CodeGlyphX | 166.44 | 234.30 | 124.42–238.36 | 60278 |
| Url | Png | QRCoder | 430.89 | 739.73 | 376.98–766.22 | 8200 |
| Url | Png | ZXing-C++ | 1194.79 | 1530.62 | 1120.49–1628.59 | 1309024 |
| Url | Png | ZXing.Net | 1230.68 | 1801.45 | 1144.30–1866.99 | 1377494 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 2076.67 | 2428.68 | 1703.77–2564.88 | 83.11 | 17–18 | 0–0 |
| ccd0 | ZXing-C++ | 49.46 | 99.53 | 24.83–114.41 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 270.47 | 588.43 | 204.27–652.15 | 13.00 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1813.45 | 2147.59 | 1491.74–2189.95 | 85.08 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 31.68 | 39.50 | 26.70–43.73 | 2.21 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 239.21 | 317.57 | 176.02–344.38 | 12.97 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
