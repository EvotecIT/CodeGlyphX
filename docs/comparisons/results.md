# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 198/267 | 6 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 5 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 10 | 13 | 13 |
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
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 198/267 correct positive images, compared with 195/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 198/267 correct positive images versus 195/267 in the previous pass. Previously correct images lost: none; gained: rss14-1/5.png, rss14-1/2.png, rss14-2/24.png. Paired negative completion is [30, 29, 30, 30]/31 with the current binary and [30, 29, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Stacked DataBar adds one shared default scanner attempt for its two variants within the existing total allowance. RSS-14 hint scope changes the work requested in that family, so timing changes do not isolate decoder implementation cost. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 40.4%, 33.9%, 41.4%. Before the supplementary negative pass: 23.8%, 20.0%, 19.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 25/25. Previously completed images now unfinished: falsepositives-2/09.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 42.5% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 16.5% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 23.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 38.0% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 44.6% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 44.8% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 65.7% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 52.9% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 38.6% | 58.3% | 18.8% |
| decoding-ccd0 | 98.2% | 99.1% | 97.3% |
| decoding-ccd1 | 96.3% | 97.6% | 95.0% |
| diagnostic-final-All-32-final | 42.8% | 61.0% | 24.5% |
| diagnostic-final-All-32-qualified | 34.1% | 58.2% | 10.3% |
| diagnostic-final-QrCode-1-final | 43.5% | 60.0% | 27.1% |
| diagnostic-final-QrCode-1-qualified | 44.3% | 64.3% | 24.4% |
| diagnostic-final-QrCode-32-final | 33.5% | 55.7% | 11.3% |
| diagnostic-final-QrCode-32-qualified | 48.3% | 63.4% | 33.1% |
| generation-ccd0 | 27.5% | 46.0% | 9.0% |
| generation-ccd1 | 23.1% | 40.0% | 6.2% |
| holdout-current | 46.2% | 65.7% | 26.8% |
| negative-rotation-ccd0-0-previous | 42.5% | 59.1% | 25.9% |
| negative-rotation-ccd0-1-current | 16.5% | 30.1% | 3.1% |
| negative-rotation-ccd0-2-current | 23.4% | 40.9% | 6.0% |
| negative-rotation-ccd0-3-previous | 38.0% | 56.3% | 19.8% |
| negative-rotation-ccd1-0-current | 44.6% | 62.4% | 26.9% |
| negative-rotation-ccd1-1-previous | 44.8% | 62.6% | 27.0% |
| negative-rotation-ccd1-2-previous | 65.7% | 75.7% | 55.8% |
| negative-rotation-ccd1-3-current | 52.9% | 67.8% | 38.0% |
| negatives-ccd0 | 21.0% | 37.1% | 4.9% |

## Generation on ccd0

Source `7a37f2d278ec2d239406145cb85902a8e61dc1df`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 116.55 | 194.26 | 84.18–199.62 | 3832 |
| Numeric | Modules | QRCoder | 146.12 | 271.94 | 118.01–352.50 | 5568 |
| Numeric | Modules | ZXing-C++ | 48.97 | 92.41 | 29.74–102.62 | 1696 |
| Numeric | Modules | ZXing.Net | 87.15 | 124.79 | 41.54–132.11 | 31304 |
| Numeric | Png | CodeGlyphX | 123.76 | 159.05 | 78.24–169.51 | 40312 |
| Numeric | Png | QRCoder | 250.68 | 309.59 | 199.91–321.94 | 6568 |
| Numeric | Png | ZXing-C++ | 1008.54 | 1089.93 | 927.84–1090.31 | 848274 |
| Numeric | Png | ZXing.Net | 1066.37 | 1249.01 | 943.02–1368.36 | 877551 |
| Unicode | Modules | CodeGlyphX | 1890.25 | 2060.76 | 1829.25–2119.85 | 23872 |
| Unicode | Modules | QRCoder | 2968.49 | 3238.93 | 2747.60–3295.34 | 23216 |
| Unicode | Modules | ZXing-C++ | 1236.45 | 1383.04 | 1166.46–1473.52 | 14648 |
| Unicode | Modules | ZXing.Net | 1828.95 | 1960.34 | 1705.90–1967.33 | 606584 |
| Unicode | Png | CodeGlyphX | 2008.26 | 2164.16 | 1836.40–2174.08 | 274832 |
| Unicode | Png | QRCoder | 3384.54 | 3603.26 | 3177.98–3655.97 | 26112 |
| Unicode | Png | ZXing-C++ | 8490.37 | 8943.01 | 8110.37–9045.49 | 6206619 |
| Unicode | Png | ZXing.Net | 10143.12 | 10840.83 | 9198.14–10964.08 | 6798018 |
| Url | Modules | CodeGlyphX | 203.10 | 1060.47 | 102.75–1080.11 | 5057 |
| Url | Modules | QRCoder | 420.08 | 1215.00 | 286.64–1338.62 | 7389 |
| Url | Modules | ZXing-C++ | 136.05 | 183.83 | 84.06–184.58 | 2697 |
| Url | Modules | ZXing.Net | 231.64 | 1006.60 | 168.65–1042.07 | 71108 |
| Url | Png | CodeGlyphX | 258.26 | 352.05 | 167.16–378.83 | 60283 |
| Url | Png | QRCoder | 520.18 | 829.98 | 434.86–907.06 | 8200 |
| Url | Png | ZXing-C++ | 1517.19 | 2538.57 | 1346.21–2874.52 | 1308994 |
| Url | Png | ZXing.Net | 1867.42 | 3500.81 | 1577.77–3897.39 | 1377461 |

## Generation on ccd1

Source `7a37f2d278ec2d239406145cb85902a8e61dc1df`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 86.13 | 141.37 | 59.69–147.43 | 3832 |
| Numeric | Modules | QRCoder | 98.92 | 122.00 | 72.79–122.49 | 5568 |
| Numeric | Modules | ZXing-C++ | 30.18 | 79.67 | 28.10–96.05 | 1696 |
| Numeric | Modules | ZXing.Net | 55.65 | 92.10 | 35.90–95.24 | 31304 |
| Numeric | Png | CodeGlyphX | 102.23 | 121.25 | 76.19–125.53 | 40312 |
| Numeric | Png | QRCoder | 144.13 | 176.65 | 141.56–176.98 | 6568 |
| Numeric | Png | ZXing-C++ | 752.85 | 828.80 | 691.71–849.65 | 848318 |
| Numeric | Png | ZXing.Net | 780.78 | 877.63 | 721.85–878.63 | 877610 |
| Unicode | Modules | CodeGlyphX | 1604.94 | 1686.27 | 1507.65–1708.98 | 23872 |
| Unicode | Modules | QRCoder | 2450.53 | 2742.25 | 2358.96–2768.25 | 23216 |
| Unicode | Modules | ZXing-C++ | 1040.61 | 1249.32 | 1001.29–1253.13 | 14648 |
| Unicode | Modules | ZXing.Net | 1619.70 | 1794.66 | 1460.96–1796.15 | 606586 |
| Unicode | Png | CodeGlyphX | 1811.06 | 1973.18 | 1661.40–2044.04 | 274832 |
| Unicode | Png | QRCoder | 2961.10 | 3651.81 | 2834.93–3911.48 | 26112 |
| Unicode | Png | ZXing-C++ | 7297.41 | 8763.50 | 6856.23–8791.50 | 6206642 |
| Unicode | Png | ZXing.Net | 8656.81 | 10492.14 | 7480.53–11083.43 | 6798190 |
| Url | Modules | CodeGlyphX | 128.65 | 173.96 | 98.99–201.33 | 5056 |
| Url | Modules | QRCoder | 290.05 | 327.29 | 277.70–339.46 | 7377 |
| Url | Modules | ZXing-C++ | 63.14 | 86.00 | 57.76–87.33 | 2696 |
| Url | Modules | ZXing.Net | 195.71 | 216.29 | 165.53–219.56 | 71064 |
| Url | Png | CodeGlyphX | 147.31 | 171.09 | 125.97–175.18 | 60272 |
| Url | Png | QRCoder | 402.51 | 420.46 | 374.77–423.07 | 8200 |
| Url | Png | ZXing-C++ | 1224.53 | 1317.25 | 1182.82–1351.37 | 1309114 |
| Url | Png | ZXing.Net | 1340.04 | 1532.78 | 1265.12–1576.13 | 1377494 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1995.90 | 2390.74 | 1605.62–2405.22 | 87.59 | 17–18 | 0–0 |
| ccd0 | ZXing-C++ | 43.79 | 98.68 | 27.18–113.66 | 2.58 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 296.44 | 461.81 | 194.17–478.01 | 13.39 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1786.93 | 2065.14 | 1582.03–2132.80 | 91.69 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 29.63 | 49.71 | 21.73–53.57 | 2.58 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 230.62 | 296.24 | 180.81–302.47 | 13.38 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
