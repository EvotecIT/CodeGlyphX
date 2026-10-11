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

This decoding follow-up records 187/267 correct positive images, compared with 187/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 187/267 correct positive images versus 187/267 in the previous pass. Previously correct images lost: []; gained: []. Paired negative completion is [30, 30, 31, 30]/31 with the current binary and [30, 30, 30, 31]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Data Matrix avoids sampling dimensions that cannot decode, preserving clock-derived fractional-pitch recovery and supported-grid priority. Acceptance thresholds and configured budgets are unchanged. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 29.6%, 25.5%, 20.9%. Before the supplementary negative pass: 33.7%, 26.5%, 26.5%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 14.1% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 24.5% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 8.5% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 23.7% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 14.7% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 13.2% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 41.8% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 14.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 25.4% | 41.6% | 9.2% |
| decoding-ccd0 | 26.5% | 40.9% | 12.3% |
| decoding-ccd1 | 10.0% | 17.0% | 3.4% |
| diagnostic-final-DataMatrix-final | 83.4% | 88.0% | 78.7% |
| diagnostic-final-DataMatrix-qualified | 71.9% | 83.1% | 60.6% |
| generation-ccd0 | 9.9% | 18.3% | 1.7% |
| generation-ccd1 | 30.6% | 40.6% | 20.6% |
| grid-baseline | 28.3% | 44.4% | 12.2% |
| holdout-current | 61.4% | 91.7% | 31.0% |
| negative-rotation-ccd0-0-previous | 14.1% | 24.4% | 4.1% |
| negative-rotation-ccd0-1-current | 24.5% | 36.1% | 13.0% |
| negative-rotation-ccd0-2-current | 8.5% | 15.9% | 1.6% |
| negative-rotation-ccd0-3-previous | 23.7% | 34.5% | 12.9% |
| negative-rotation-ccd1-0-current | 14.7% | 26.6% | 2.9% |
| negative-rotation-ccd1-1-previous | 13.2% | 24.6% | 2.0% |
| negative-rotation-ccd1-2-previous | 41.8% | 54.3% | 29.3% |
| negative-rotation-ccd1-3-current | 14.5% | 27.7% | 1.5% |
| negatives-ccd0 | 28.9% | 45.2% | 12.5% |

## Generation on ccd0

Source `3266d3b059d9bfb6158b8be981dc68075aa9961a`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 63.96 | 92.81 | 60.90–94.34 | 3832 |
| Numeric | Modules | QRCoder | 84.62 | 110.79 | 76.91–113.47 | 5568 |
| Numeric | Modules | ZXing-C++ | 31.40 | 61.47 | 28.31–64.44 | 1696 |
| Numeric | Modules | ZXing.Net | 41.56 | 81.17 | 37.87–89.54 | 31304 |
| Numeric | Png | CodeGlyphX | 89.00 | 110.56 | 76.22–119.47 | 40312 |
| Numeric | Png | QRCoder | 148.36 | 164.56 | 145.16–165.80 | 6568 |
| Numeric | Png | ZXing-C++ | 687.69 | 733.57 | 649.55–737.32 | 848275 |
| Numeric | Png | ZXing.Net | 720.47 | 739.84 | 688.73–749.04 | 877590 |
| Unicode | Modules | CodeGlyphX | 1655.08 | 1808.38 | 1511.57–1818.89 | 23872 |
| Unicode | Modules | QRCoder | 2681.43 | 2878.74 | 2408.70–2905.36 | 23222 |
| Unicode | Modules | ZXing-C++ | 1133.33 | 1193.17 | 972.86–1204.53 | 14648 |
| Unicode | Modules | ZXing.Net | 1603.04 | 1849.92 | 1509.08–1869.54 | 606588 |
| Unicode | Png | CodeGlyphX | 1683.65 | 1895.80 | 1605.00–2036.18 | 274843 |
| Unicode | Png | QRCoder | 2801.74 | 3212.79 | 2747.20–3231.69 | 26112 |
| Unicode | Png | ZXing-C++ | 6589.47 | 7325.27 | 6300.22–7372.85 | 6206537 |
| Unicode | Png | ZXing.Net | 7225.86 | 7692.63 | 6952.09–7744.47 | 6798108 |
| Url | Modules | CodeGlyphX | 536.26 | 931.47 | 99.24–940.39 | 5057 |
| Url | Modules | QRCoder | 606.40 | 1143.31 | 312.60–1234.22 | 7395 |
| Url | Modules | ZXing-C++ | 114.25 | 195.58 | 58.99–222.06 | 2701 |
| Url | Modules | ZXing.Net | 433.13 | 859.24 | 169.61–862.41 | 71098 |
| Url | Png | CodeGlyphX | 174.08 | 224.04 | 134.76–268.28 | 60283 |
| Url | Png | QRCoder | 401.85 | 503.29 | 384.15–518.86 | 8200 |
| Url | Png | ZXing-C++ | 1210.52 | 1252.66 | 1144.43–1263.08 | 1309056 |
| Url | Png | ZXing.Net | 1341.86 | 1418.10 | 1212.48–1434.05 | 1377485 |

## Generation on ccd1

Source `3266d3b059d9bfb6158b8be981dc68075aa9961a`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 63.25 | 87.93 | 58.96–89.08 | 3832 |
| Numeric | Modules | QRCoder | 76.50 | 104.05 | 74.66–104.13 | 5568 |
| Numeric | Modules | ZXing-C++ | 28.68 | 56.94 | 27.42–59.41 | 1696 |
| Numeric | Modules | ZXing.Net | 40.75 | 69.11 | 37.32–69.19 | 31304 |
| Numeric | Png | CodeGlyphX | 89.41 | 103.69 | 75.67–103.73 | 40312 |
| Numeric | Png | QRCoder | 152.05 | 163.79 | 147.70–166.75 | 6568 |
| Numeric | Png | ZXing-C++ | 728.98 | 740.92 | 688.07–741.65 | 848315 |
| Numeric | Png | ZXing.Net | 754.15 | 812.59 | 730.52–831.22 | 877636 |
| Unicode | Modules | CodeGlyphX | 1498.20 | 1620.52 | 1470.33–1748.78 | 23872 |
| Unicode | Modules | QRCoder | 2373.38 | 2459.94 | 2345.03–2540.86 | 23216 |
| Unicode | Modules | ZXing-C++ | 991.93 | 1042.32 | 981.17–1050.65 | 14648 |
| Unicode | Modules | ZXing.Net | 1474.02 | 1528.01 | 1452.07–1538.40 | 606585 |
| Unicode | Png | CodeGlyphX | 1729.35 | 1802.34 | 1647.68–1803.92 | 274832 |
| Unicode | Png | QRCoder | 2819.83 | 2882.87 | 2748.79–2888.93 | 26112 |
| Unicode | Png | ZXing-C++ | 6710.15 | 7351.09 | 6406.70–7482.10 | 6206559 |
| Unicode | Png | ZXing.Net | 7593.91 | 8100.04 | 7063.47–8577.55 | 6798337 |
| Url | Modules | CodeGlyphX | 108.43 | 140.97 | 100.85–155.13 | 5056 |
| Url | Modules | QRCoder | 310.68 | 339.19 | 284.61–357.79 | 7376 |
| Url | Modules | ZXing-C++ | 70.33 | 122.36 | 59.32–160.97 | 2696 |
| Url | Modules | ZXing.Net | 197.92 | 238.47 | 168.90–265.15 | 71066 |
| Url | Png | CodeGlyphX | 130.47 | 178.48 | 125.71–183.61 | 60272 |
| Url | Png | QRCoder | 385.20 | 417.17 | 377.72–439.41 | 8200 |
| Url | Png | ZXing-C++ | 1198.37 | 1248.91 | 1162.38–1254.63 | 1309091 |
| Url | Png | ZXing.Net | 1276.97 | 1392.65 | 1213.56–1409.59 | 1377493 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3012.02 | 3114.20 | 2979.02–3142.66 | 88.30 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 21.60 | 24.82 | 18.41–25.17 | 2.15 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 164.57 | 177.98 | 152.95–183.34 | 12.93 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3148.45 | 3317.54 | 2974.51–3321.82 | 87.30 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 23.79 | 43.31 | 19.98–51.59 | 2.16 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 208.89 | 293.13 | 150.00–315.70 | 12.95 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
