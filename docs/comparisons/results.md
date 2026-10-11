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

This decoding follow-up records 198/267 correct positive images, compared with 198/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 198/267 correct positive images versus 198/267 in the previous pass. Previously correct images lost: none; gained: none. Paired negative completion is [30, 29, 30, 29]/31 with the current binary and [30, 29, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. QR extraction and deadline work reduce implementation cost without changing the total or family recognition allowances. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 27.3%, 24.8%, 24.5%. Before the supplementary negative pass: 94.2%, 99.6%, 97.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 19/25 | 0 | 6 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 19/25 supplementary negatives versus 24/25. Previously completed images now unfinished: falsepositives-2/01.png, falsepositives-2/02.png, falsepositives-2/03.png, falsepositives-2/06.png, falsepositives-2/07.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 66.9% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 45.6% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 68.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 18.8% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 38.1% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 41.0% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 51.0% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 37.2% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 25.5% | 42.3% | 8.7% |
| decoding-ccd0 | 51.2% | 62.0% | 39.1% |
| decoding-ccd1 | 23.3% | 37.2% | 9.5% |
| diagnostic-final-All-32-final | 26.2% | 43.6% | 8.8% |
| diagnostic-final-All-32-qualified | 33.1% | 49.3% | 16.9% |
| diagnostic-final-QrCode-1-final | 27.1% | 41.8% | 12.5% |
| diagnostic-final-QrCode-1-qualified | 41.2% | 53.9% | 28.7% |
| diagnostic-final-QrCode-32-final | 13.1% | 23.7% | 2.4% |
| diagnostic-final-QrCode-32-qualified | 32.2% | 47.4% | 17.1% |
| generation-ccd0 | 38.7% | 53.4% | 23.9% |
| generation-ccd1 | 50.8% | 63.5% | 38.5% |
| holdout-current | 22.7% | 37.2% | 8.4% |
| negative-rotation-ccd0-0-previous | 66.9% | 78.7% | 55.0% |
| negative-rotation-ccd0-1-current | 45.6% | 57.4% | 33.9% |
| negative-rotation-ccd0-2-current | 68.4% | 77.8% | 59.1% |
| negative-rotation-ccd0-3-previous | 18.8% | 32.0% | 5.6% |
| negative-rotation-ccd1-0-current | 38.1% | 52.0% | 24.3% |
| negative-rotation-ccd1-1-previous | 41.0% | 50.6% | 31.3% |
| negative-rotation-ccd1-2-previous | 51.0% | 61.0% | 40.9% |
| negative-rotation-ccd1-3-current | 37.2% | 49.8% | 24.5% |
| negatives-ccd0 | 97.0% | 97.7% | 96.2% |

## Generation on ccd0

Source `c13c8523b44ea5b8f650ceeba81e176254896c92`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 116.45 | 211.59 | 94.58–253.46 | 3832 |
| Numeric | Modules | QRCoder | 127.61 | 229.66 | 114.99–312.48 | 5568 |
| Numeric | Modules | ZXing-C++ | 51.49 | 103.40 | 36.09–104.22 | 1696 |
| Numeric | Modules | ZXing.Net | 97.69 | 145.57 | 50.10–152.12 | 31304 |
| Numeric | Png | CodeGlyphX | 206.63 | 652.94 | 125.01–818.43 | 40340 |
| Numeric | Png | QRCoder | 405.33 | 1303.03 | 273.03–1370.51 | 6744 |
| Numeric | Png | ZXing-C++ | 1576.18 | 3693.08 | 892.58–3834.85 | 848406 |
| Numeric | Png | ZXing.Net | 1808.98 | 4308.76 | 1329.20–5632.84 | 877683 |
| Unicode | Modules | CodeGlyphX | 1968.90 | 3229.96 | 1693.29–4312.42 | 23872 |
| Unicode | Modules | QRCoder | 3037.59 | 5027.92 | 2570.91–7055.06 | 23219 |
| Unicode | Modules | ZXing-C++ | 1249.70 | 1334.89 | 1032.17–1336.21 | 14648 |
| Unicode | Modules | ZXing.Net | 1882.14 | 2880.69 | 1567.85–3736.34 | 606584 |
| Unicode | Png | CodeGlyphX | 2061.15 | 2333.78 | 1835.83–2420.62 | 274942 |
| Unicode | Png | QRCoder | 3338.76 | 3737.38 | 2802.49–3747.91 | 26799 |
| Unicode | Png | ZXing-C++ | 8351.77 | 10023.78 | 7069.00–10593.60 | 6207054 |
| Unicode | Png | ZXing.Net | 10470.80 | 11672.54 | 9092.73–12002.53 | 6798540 |
| Url | Modules | CodeGlyphX | 222.69 | 1087.98 | 160.06–1167.58 | 5057 |
| Url | Modules | QRCoder | 1013.27 | 1148.32 | 357.14–1246.46 | 7397 |
| Url | Modules | ZXing-C++ | 118.42 | 194.92 | 72.58–203.49 | 2697 |
| Url | Modules | ZXing.Net | 229.97 | 1028.65 | 198.32–1088.21 | 71108 |
| Url | Png | CodeGlyphX | 229.54 | 348.00 | 167.11–361.38 | 60278 |
| Url | Png | QRCoder | 502.06 | 554.02 | 441.02–560.60 | 8258 |
| Url | Png | ZXing-C++ | 1540.69 | 1813.63 | 1434.53–1916.88 | 1309024 |
| Url | Png | ZXing.Net | 1631.91 | 2009.20 | 1550.53–2158.11 | 1377458 |

## Generation on ccd1

Source `c13c8523b44ea5b8f650ceeba81e176254896c92`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 61.12 | 95.22 | 59.75–103.13 | 3832 |
| Numeric | Modules | QRCoder | 77.34 | 112.39 | 76.40–118.89 | 5568 |
| Numeric | Modules | ZXing-C++ | 37.00 | 76.26 | 28.47–97.26 | 1696 |
| Numeric | Modules | ZXing.Net | 58.52 | 74.28 | 38.68–78.20 | 31304 |
| Numeric | Png | CodeGlyphX | 77.30 | 101.97 | 75.84–103.56 | 40312 |
| Numeric | Png | QRCoder | 147.33 | 157.19 | 145.23–158.16 | 6568 |
| Numeric | Png | ZXing-C++ | 645.62 | 698.61 | 616.57–699.33 | 848260 |
| Numeric | Png | ZXing.Net | 660.55 | 737.55 | 617.32–746.98 | 877553 |
| Unicode | Modules | CodeGlyphX | 1498.18 | 1529.84 | 1468.90–1548.97 | 23872 |
| Unicode | Modules | QRCoder | 2396.29 | 2427.78 | 2360.38–2430.86 | 23216 |
| Unicode | Modules | ZXing-C++ | 980.80 | 1003.10 | 967.99–1012.32 | 14648 |
| Unicode | Modules | ZXing.Net | 1446.69 | 1467.52 | 1442.56–1470.22 | 606584 |
| Unicode | Png | CodeGlyphX | 1758.93 | 3539.40 | 1641.06–4049.00 | 274832 |
| Unicode | Png | QRCoder | 2959.16 | 5352.96 | 2748.96–5409.85 | 26167 |
| Unicode | Png | ZXing-C++ | 6869.22 | 17956.79 | 6183.22–18375.26 | 6206624 |
| Unicode | Png | ZXing.Net | 9775.35 | 16730.89 | 7785.97–18016.12 | 6798088 |
| Url | Modules | CodeGlyphX | 176.26 | 300.23 | 132.30–340.66 | 5056 |
| Url | Modules | QRCoder | 370.66 | 486.24 | 293.58–504.60 | 7377 |
| Url | Modules | ZXing-C++ | 89.37 | 148.75 | 62.56–169.75 | 2696 |
| Url | Modules | ZXing.Net | 184.61 | 290.32 | 166.06–330.16 | 71064 |
| Url | Png | CodeGlyphX | 156.32 | 236.50 | 125.04–253.84 | 60272 |
| Url | Png | QRCoder | 407.19 | 460.42 | 379.98–482.64 | 8200 |
| Url | Png | ZXing-C++ | 1162.49 | 1471.42 | 1107.07–1625.58 | 1308975 |
| Url | Png | ZXing.Net | 1391.11 | 1576.16 | 1172.85–1680.67 | 1377441 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1435.79 | 1516.92 | 1382.68–1543.26 | 90.37 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 20.84 | 26.26 | 18.46–26.78 | 2.53 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 168.35 | 186.66 | 152.71–188.32 | 13.31 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1423.48 | 1462.74 | 1386.57–1473.92 | 90.40 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 21.14 | 23.33 | 20.20–23.56 | 2.53 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 162.93 | 174.53 | 151.94–174.61 | 13.33 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
