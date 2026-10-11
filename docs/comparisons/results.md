# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 187/267 | 6 | 3 | 0 | 1 |
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
| Code93 | 3 | 2 | 2 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 187/267 correct positive images, compared with 188/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: qrcode-3/11.png

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

This follow-up records 187/267 correct positive images versus 188/267 in the previous pass. Previously correct images lost: ['qrcode-3/11.png']; gained: []. Paired negative completion is [30, 31, 30, 30]/31 with the current binary and [30, 30, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Data Matrix changes copy work and clears stale success diagnostics, without changing acceptance thresholds or search effort. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. The lost QR photograph records DeadlineExceeded in the frozen pass; the unchanged QR source and bounded follow-up remain documented. Both binaries time out in all four cold single-image checks each. These checks do not replace the original frozen outcome or isolate host load from process warmup.

Host CPU samples before the main corpus pass: 68.5%, 36.3%, 32.6%. Before the supplementary negative pass: 17.3%, 16.8%, 21.0%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 17.9% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 18.3% |
| ccd0 | 3 | current | 31/31 | 0 | 0 | 27.3% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 33.9% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 24.8% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 17.0% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 18.4% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 30.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 45.8% | 64.9% | 26.7% |
| decoding-ccd0 | 34.8% | 51.9% | 17.8% |
| decoding-ccd1 | 17.2% | 31.6% | 3.1% |
| diagnostic-final-DataMatrix-final | 24.7% | 36.8% | 12.6% |
| diagnostic-final-DataMatrix-qualified | 29.4% | 44.5% | 14.3% |
| generation-ccd0 | 42.7% | 60.8% | 24.6% |
| generation-ccd1 | 44.8% | 56.1% | 33.5% |
| holdout-current | 25.2% | 39.7% | 10.7% |
| negative-rotation-ccd0-0-previous | 17.9% | 31.8% | 4.0% |
| negative-rotation-ccd0-1-current | 18.3% | 33.5% | 3.2% |
| negative-rotation-ccd0-2-current | 27.3% | 39.6% | 14.9% |
| negative-rotation-ccd0-3-previous | 33.9% | 46.9% | 21.0% |
| negative-rotation-ccd1-0-current | 24.8% | 46.0% | 3.7% |
| negative-rotation-ccd1-1-previous | 17.0% | 30.4% | 3.8% |
| negative-rotation-ccd1-2-previous | 18.4% | 31.1% | 5.8% |
| negative-rotation-ccd1-3-current | 30.7% | 45.6% | 15.9% |
| negatives-ccd0 | 18.4% | 32.4% | 4.4% |
| qr-deadline-ccd0-0-previous | 16.2% | 31.4% | 1.3% |
| qr-deadline-ccd0-1-current | 14.3% | 26.8% | 2.1% |
| qr-deadline-ccd0-2-current | 23.9% | 39.4% | 8.4% |
| qr-deadline-ccd0-3-previous | 14.9% | 25.6% | 4.3% |
| qr-deadline-ccd1-4-current | 24.2% | 41.1% | 7.4% |
| qr-deadline-ccd1-5-previous | 19.6% | 32.8% | 6.5% |
| qr-deadline-ccd1-6-previous | 18.5% | 26.9% | 10.1% |
| qr-deadline-ccd1-7-current | 8.7% | 15.6% | 2.1% |

## Generation on ccd0

Source `8c7ff95a79790b104ba3575b28d20883109e12fa`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 98.46 | 135.65 | 61.02–143.36 | 3832 |
| Numeric | Modules | QRCoder | 130.04 | 155.49 | 107.17–168.95 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.55 | 75.84 | 38.19–77.36 | 1696 |
| Numeric | Modules | ZXing.Net | 66.04 | 95.68 | 41.32–96.59 | 31304 |
| Numeric | Png | CodeGlyphX | 116.36 | 143.44 | 75.71–146.34 | 40312 |
| Numeric | Png | QRCoder | 212.37 | 230.40 | 175.34–234.20 | 6568 |
| Numeric | Png | ZXing-C++ | 863.97 | 927.95 | 729.30–945.25 | 848294 |
| Numeric | Png | ZXing.Net | 866.90 | 964.66 | 758.72–970.70 | 877584 |
| Unicode | Modules | CodeGlyphX | 1839.59 | 2454.39 | 1553.80–3110.19 | 23872 |
| Unicode | Modules | QRCoder | 2852.04 | 3109.18 | 2511.13–3184.26 | 23216 |
| Unicode | Modules | ZXing-C++ | 1181.00 | 1227.05 | 1040.02–1243.80 | 14648 |
| Unicode | Modules | ZXing.Net | 1838.69 | 2353.52 | 1514.05–2854.51 | 606584 |
| Unicode | Png | CodeGlyphX | 1972.76 | 2374.20 | 1714.95–2534.57 | 274832 |
| Unicode | Png | QRCoder | 3311.72 | 3473.74 | 2950.83–3500.50 | 26112 |
| Unicode | Png | ZXing-C++ | 7925.41 | 8715.96 | 6924.29–8731.72 | 6206499 |
| Unicode | Png | ZXing.Net | 9381.98 | 10978.89 | 7404.54–11069.00 | 6797973 |
| Url | Modules | CodeGlyphX | 243.58 | 1151.91 | 108.68–1218.03 | 5057 |
| Url | Modules | QRCoder | 909.65 | 1217.00 | 383.61–1237.79 | 7395 |
| Url | Modules | ZXing-C++ | 148.78 | 248.53 | 91.66–255.39 | 2697 |
| Url | Modules | ZXing.Net | 238.55 | 976.44 | 172.71–981.37 | 71108 |
| Url | Png | CodeGlyphX | 224.76 | 276.04 | 136.31–312.16 | 60278 |
| Url | Png | QRCoder | 491.41 | 531.70 | 443.82–557.26 | 8200 |
| Url | Png | ZXing-C++ | 1454.15 | 1520.27 | 1329.56–1528.10 | 1309015 |
| Url | Png | ZXing.Net | 1506.98 | 1646.15 | 1411.80–1654.26 | 1377475 |

## Generation on ccd1

Source `8c7ff95a79790b104ba3575b28d20883109e12fa`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 78.15 | 113.00 | 62.77–113.18 | 3832 |
| Numeric | Modules | QRCoder | 93.43 | 129.20 | 79.93–133.81 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.62 | 62.34 | 28.48–62.34 | 1696 |
| Numeric | Modules | ZXing.Net | 55.35 | 86.76 | 37.95–88.22 | 31304 |
| Numeric | Png | CodeGlyphX | 90.77 | 117.66 | 83.75–120.88 | 40312 |
| Numeric | Png | QRCoder | 176.26 | 195.78 | 152.17–198.27 | 6568 |
| Numeric | Png | ZXing-C++ | 786.39 | 827.45 | 739.24–838.54 | 848264 |
| Numeric | Png | ZXing.Net | 827.00 | 878.89 | 752.91–883.12 | 877612 |
| Unicode | Modules | CodeGlyphX | 1622.96 | 1812.72 | 1545.65–1932.35 | 23872 |
| Unicode | Modules | QRCoder | 2563.40 | 2817.04 | 2497.18–2901.96 | 23216 |
| Unicode | Modules | ZXing-C++ | 1069.92 | 1143.60 | 1021.66–1164.14 | 14648 |
| Unicode | Modules | ZXing.Net | 1566.10 | 1703.80 | 1531.55–1734.42 | 606584 |
| Unicode | Png | CodeGlyphX | 1919.71 | 2202.10 | 1760.89–2220.90 | 274832 |
| Unicode | Png | QRCoder | 3153.42 | 3704.29 | 2879.53–4022.45 | 26112 |
| Unicode | Png | ZXing-C++ | 7274.59 | 8777.65 | 6754.54–9532.83 | 6206634 |
| Unicode | Png | ZXing.Net | 8780.18 | 11120.50 | 7427.70–11753.70 | 6798188 |
| Url | Modules | CodeGlyphX | 137.54 | 190.08 | 116.77–207.14 | 5056 |
| Url | Modules | QRCoder | 370.95 | 398.45 | 313.33–399.05 | 7376 |
| Url | Modules | ZXing-C++ | 89.06 | 139.27 | 59.19–147.51 | 2696 |
| Url | Modules | ZXing.Net | 202.97 | 237.03 | 170.45–244.65 | 71064 |
| Url | Png | CodeGlyphX | 168.67 | 208.93 | 144.56–210.26 | 60272 |
| Url | Png | QRCoder | 431.74 | 483.85 | 401.77–489.13 | 8200 |
| Url | Png | ZXing-C++ | 1333.60 | 1398.72 | 1245.22–1403.05 | 1309034 |
| Url | Png | ZXing.Net | 1379.39 | 1467.66 | 1321.42–1478.47 | 1377477 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3228.73 | 3254.53 | 3180.94–3256.44 | 88.65 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 24.94 | 28.82 | 21.05–30.22 | 2.53 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 190.08 | 207.57 | 183.54–208.88 | 13.54 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3159.51 | 3213.29 | 3132.42–3232.45 | 88.73 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 21.62 | 27.43 | 19.87–27.44 | 2.53 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 168.23 | 184.46 | 160.53–185.83 | 13.58 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
