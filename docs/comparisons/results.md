# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 191/267 | 6 | 3 | 0 | 1 |
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
| QrCode | 37 | 22 | 29 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 191/267 correct positive images, compared with 188/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 191/267 correct positive images versus 188/267 in the previous pass. Previously correct images lost: none; gained: qrcode-6/13.png, qrcode-6/7.png, qrcode-6/12.png. Paired negative completion is [30, 30, 31, 30]/31 with the current binary and [30, 30, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. The QR diagonal finder check now admits the run-count ratio of a rotated square, preserving the upper mismatch bound, all five run ratios, horizontal/vertical cross-checks and payload acceptance. Recognition budgets and search limits remain unchanged. A demonstrated regression gain does not establish accuracy on unseen photographs, and legacy fallback is unchanged. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 36.9%, 27.1%, 25.0%. Before the supplementary negative pass: 26.9%, 23.8%, 28.5%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 20.6% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 61.7% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 35.1% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 19.6% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 10.0% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 22.8% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 35.6% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 20.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 29.7% | 48.2% | 11.2% |
| decoding-ccd0 | 20.8% | 35.2% | 6.6% |
| decoding-ccd1 | 17.9% | 29.3% | 6.8% |
| diagnostic-final-QrCode-final | 39.8% | 55.9% | 23.7% |
| diagnostic-final-QrCode-qualified | 32.1% | 43.6% | 20.5% |
| generation-ccd0 | 15.5% | 26.6% | 4.5% |
| generation-ccd1 | 66.6% | 75.8% | 57.5% |
| holdout-current | 17.3% | 31.9% | 2.8% |
| negative-rotation-ccd0-0-previous | 20.6% | 30.6% | 10.6% |
| negative-rotation-ccd0-1-current | 61.7% | 70.6% | 52.9% |
| negative-rotation-ccd0-2-current | 35.1% | 49.0% | 21.2% |
| negative-rotation-ccd0-3-previous | 19.6% | 34.9% | 4.4% |
| negative-rotation-ccd1-0-current | 10.0% | 18.9% | 1.8% |
| negative-rotation-ccd1-1-previous | 22.8% | 34.7% | 10.9% |
| negative-rotation-ccd1-2-previous | 35.6% | 50.0% | 21.2% |
| negative-rotation-ccd1-3-current | 20.5% | 34.3% | 6.7% |
| negatives-ccd0 | 26.4% | 39.8% | 13.1% |

## Generation on ccd0

Source `24097f313b3aea860c27e68fafaedc6d38d5dab3`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 133.06 | 312.06 | 82.96–436.38 | 3832 |
| Numeric | Modules | QRCoder | 171.54 | 576.74 | 122.42–765.30 | 5568 |
| Numeric | Modules | ZXing-C++ | 55.58 | 96.11 | 47.58–98.41 | 1696 |
| Numeric | Modules | ZXing.Net | 89.75 | 294.03 | 65.42–411.37 | 31304 |
| Numeric | Png | CodeGlyphX | 197.33 | 817.08 | 94.91–1318.29 | 40312 |
| Numeric | Png | QRCoder | 426.55 | 1440.17 | 212.67–1535.82 | 6568 |
| Numeric | Png | ZXing-C++ | 1927.87 | 3373.83 | 1113.38–3994.45 | 848285 |
| Numeric | Png | ZXing.Net | 1904.74 | 2497.00 | 1231.70–2760.62 | 877590 |
| Unicode | Modules | CodeGlyphX | 2208.13 | 3616.97 | 1799.55–4734.78 | 23872 |
| Unicode | Modules | QRCoder | 3314.62 | 5228.52 | 2807.53–5255.02 | 23216 |
| Unicode | Modules | ZXing-C++ | 1429.61 | 2278.04 | 1215.32–2389.08 | 14648 |
| Unicode | Modules | ZXing.Net | 2026.18 | 3182.68 | 1784.59–3412.06 | 606588 |
| Unicode | Png | CodeGlyphX | 2158.49 | 2570.95 | 2051.58–2927.18 | 274832 |
| Unicode | Png | QRCoder | 3452.65 | 3690.73 | 3254.72–3874.28 | 26112 |
| Unicode | Png | ZXing-C++ | 8692.52 | 11422.81 | 7990.57–12662.69 | 6206407 |
| Unicode | Png | ZXing.Net | 9624.87 | 15225.22 | 8418.89–16721.42 | 6798031 |
| Url | Modules | CodeGlyphX | 248.86 | 1141.42 | 138.30–1175.39 | 5057 |
| Url | Modules | QRCoder | 764.06 | 1071.39 | 366.56–1113.66 | 7395 |
| Url | Modules | ZXing-C++ | 144.51 | 220.89 | 59.61–221.92 | 2697 |
| Url | Modules | ZXing.Net | 426.10 | 1003.72 | 187.32–1148.18 | 71098 |
| Url | Png | CodeGlyphX | 197.87 | 283.60 | 133.67–300.09 | 60278 |
| Url | Png | QRCoder | 499.23 | 594.60 | 384.02–627.96 | 8200 |
| Url | Png | ZXing-C++ | 1426.27 | 1587.80 | 1251.43–1611.74 | 1308985 |
| Url | Png | ZXing.Net | 1639.96 | 1898.44 | 1412.19–1919.61 | 1377450 |

## Generation on ccd1

Source `24097f313b3aea860c27e68fafaedc6d38d5dab3`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 99.98 | 115.68 | 60.62–132.79 | 3832 |
| Numeric | Modules | QRCoder | 122.44 | 161.13 | 75.39–167.56 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.02 | 75.64 | 28.82–76.26 | 1696 |
| Numeric | Modules | ZXing.Net | 51.08 | 76.48 | 36.45–81.03 | 31304 |
| Numeric | Png | CodeGlyphX | 107.86 | 134.46 | 74.89–152.65 | 40312 |
| Numeric | Png | QRCoder | 176.42 | 209.09 | 145.05–216.10 | 6568 |
| Numeric | Png | ZXing-C++ | 787.77 | 865.95 | 654.36–884.85 | 848321 |
| Numeric | Png | ZXing.Net | 839.28 | 939.83 | 708.12–952.27 | 877618 |
| Unicode | Modules | CodeGlyphX | 1672.74 | 1821.21 | 1535.35–1884.87 | 23872 |
| Unicode | Modules | QRCoder | 2531.79 | 2781.21 | 2401.99–2806.32 | 23216 |
| Unicode | Modules | ZXing-C++ | 1089.23 | 1200.45 | 1008.75–1204.94 | 14648 |
| Unicode | Modules | ZXing.Net | 1627.33 | 1786.78 | 1497.77–1838.65 | 606585 |
| Unicode | Png | CodeGlyphX | 1783.39 | 1908.17 | 1715.07–1912.03 | 274832 |
| Unicode | Png | QRCoder | 3162.60 | 3436.34 | 2859.54–3442.23 | 26112 |
| Unicode | Png | ZXing-C++ | 7215.48 | 7921.02 | 6640.37–7950.10 | 6206672 |
| Unicode | Png | ZXing.Net | 7938.71 | 10003.37 | 7420.65–11127.31 | 6798317 |
| Url | Modules | CodeGlyphX | 240.75 | 358.88 | 147.45–364.50 | 5057 |
| Url | Modules | QRCoder | 478.95 | 549.91 | 345.93–575.76 | 7376 |
| Url | Modules | ZXing-C++ | 131.41 | 226.69 | 72.01–240.86 | 2696 |
| Url | Modules | ZXing.Net | 328.26 | 395.85 | 202.10–411.05 | 71064 |
| Url | Png | CodeGlyphX | 171.97 | 248.28 | 139.71–280.51 | 60272 |
| Url | Png | QRCoder | 460.27 | 487.58 | 408.58–487.69 | 8200 |
| Url | Png | ZXing-C++ | 1280.77 | 1500.97 | 1169.13–1535.25 | 1309044 |
| Url | Png | ZXing.Net | 1464.28 | 1596.46 | 1300.45–1597.87 | 1377495 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3117.42 | 3166.72 | 3063.16–3174.56 | 87.22 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 23.97 | 27.52 | 18.63–28.02 | 2.13 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 192.10 | 216.09 | 173.27–219.61 | 13.18 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3006.27 | 3037.85 | 2977.29–3053.92 | 88.11 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 20.88 | 25.68 | 20.21–27.26 | 2.15 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 164.18 | 180.68 | 151.70–181.31 | 13.19 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
