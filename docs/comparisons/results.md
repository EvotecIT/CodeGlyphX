# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 188/267 | 6 | 3 | 0 | 1 |
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
| QrCode | 37 | 22 | 26 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 188/267 correct positive images, compared with 187/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 188/267 correct positive images versus 187/267 in the previous pass. Previously correct images lost: none; gained: qrcode-3/11.png. Paired negative completion is [30, 29, 30, 31]/31 with the current binary and [30, 30, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. A bounded early QR photograph retry uses existing normalization, hybrid thresholding and perspective precision within the same configured allowance. This changes modern QR recognition search order; acceptance checks and deadlines remain unchanged. A demonstrated regression gain does not establish accuracy on unseen photographs, and legacy fallback is unchanged. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 27.4%, 33.9%, 37.5%. Before the supplementary negative pass: 22.2%, 25.3%, 64.2%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 39.8% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 28.6% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 12.5% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 14.3% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 60.2% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 15.1% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 21.1% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 95.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 32.9% | 48.8% | 17.1% |
| decoding-ccd0 | 38.9% | 56.9% | 20.8% |
| decoding-ccd1 | 40.8% | 55.2% | 26.4% |
| diagnostic-final-QrCode-final | 58.4% | 72.0% | 44.8% |
| diagnostic-final-QrCode-qualified | 95.5% | 97.6% | 93.4% |
| generation-ccd0 | 14.5% | 27.7% | 1.6% |
| generation-ccd1 | 30.0% | 44.3% | 15.8% |
| holdout-current | 69.1% | 78.7% | 59.6% |
| negative-rotation-ccd0-0-previous | 39.8% | 53.5% | 26.2% |
| negative-rotation-ccd0-1-current | 28.6% | 41.4% | 15.9% |
| negative-rotation-ccd0-2-current | 12.5% | 22.7% | 2.0% |
| negative-rotation-ccd0-3-previous | 14.3% | 23.4% | 5.3% |
| negative-rotation-ccd1-0-current | 60.2% | 72.0% | 48.5% |
| negative-rotation-ccd1-1-previous | 15.1% | 28.8% | 1.9% |
| negative-rotation-ccd1-2-previous | 21.1% | 34.3% | 7.8% |
| negative-rotation-ccd1-3-current | 95.8% | 97.2% | 94.4% |
| negatives-ccd0 | 37.2% | 50.8% | 23.6% |

## Generation on ccd0

Source `d4f24483f3afef201c614986a12871ecc349bfd1`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 107.93 | 163.66 | 77.98–165.94 | 3832 |
| Numeric | Modules | QRCoder | 130.31 | 207.80 | 76.14–219.19 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.66 | 94.55 | 35.96–111.33 | 1696 |
| Numeric | Modules | ZXing.Net | 83.80 | 104.57 | 38.51–106.05 | 31304 |
| Numeric | Png | CodeGlyphX | 129.87 | 161.04 | 82.55–166.37 | 40312 |
| Numeric | Png | QRCoder | 215.90 | 258.44 | 169.45–277.24 | 6568 |
| Numeric | Png | ZXing-C++ | 987.01 | 1106.74 | 882.21–1109.80 | 848302 |
| Numeric | Png | ZXing.Net | 1000.94 | 1055.81 | 928.29–1062.39 | 877588 |
| Unicode | Modules | CodeGlyphX | 1848.65 | 1925.70 | 1754.78–1933.36 | 23872 |
| Unicode | Modules | QRCoder | 2793.10 | 2883.20 | 2737.38–2926.17 | 23216 |
| Unicode | Modules | ZXing-C++ | 1175.27 | 1220.70 | 1059.89–1225.23 | 14648 |
| Unicode | Modules | ZXing.Net | 1769.65 | 1840.95 | 1695.96–1846.63 | 606589 |
| Unicode | Png | CodeGlyphX | 1989.96 | 2183.81 | 1850.57–2238.45 | 274832 |
| Unicode | Png | QRCoder | 3269.42 | 3589.13 | 3183.76–3660.87 | 26112 |
| Unicode | Png | ZXing-C++ | 8025.54 | 8730.38 | 7729.01–8985.69 | 6206453 |
| Unicode | Png | ZXing.Net | 8879.65 | 9995.23 | 8620.30–10123.05 | 6798026 |
| Url | Modules | CodeGlyphX | 157.91 | 980.49 | 106.84–1063.03 | 5057 |
| Url | Modules | QRCoder | 934.14 | 1076.94 | 303.46–1096.62 | 7395 |
| Url | Modules | ZXing-C++ | 97.60 | 176.96 | 68.79–181.88 | 2697 |
| Url | Modules | ZXing.Net | 252.29 | 909.69 | 201.87–911.50 | 71092 |
| Url | Png | CodeGlyphX | 265.62 | 335.32 | 144.73–350.76 | 60283 |
| Url | Png | QRCoder | 510.57 | 889.02 | 480.41–918.20 | 8200 |
| Url | Png | ZXing-C++ | 1662.64 | 2571.64 | 1399.15–2649.60 | 1308993 |
| Url | Png | ZXing.Net | 1695.44 | 2724.38 | 1564.86–2866.84 | 1377443 |

## Generation on ccd1

Source `d4f24483f3afef201c614986a12871ecc349bfd1`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 74.07 | 111.10 | 59.90–121.86 | 3832 |
| Numeric | Modules | QRCoder | 97.75 | 133.21 | 71.66–140.88 | 5568 |
| Numeric | Modules | ZXing-C++ | 34.42 | 55.41 | 29.56–55.91 | 1696 |
| Numeric | Modules | ZXing.Net | 51.04 | 102.46 | 37.39–126.81 | 31304 |
| Numeric | Png | CodeGlyphX | 85.75 | 124.44 | 76.39–126.38 | 40312 |
| Numeric | Png | QRCoder | 157.39 | 185.09 | 141.15–186.00 | 6568 |
| Numeric | Png | ZXing-C++ | 768.48 | 861.58 | 700.88–889.61 | 848331 |
| Numeric | Png | ZXing.Net | 782.51 | 840.74 | 712.31–856.88 | 877618 |
| Unicode | Modules | CodeGlyphX | 1712.45 | 1775.78 | 1572.64–1812.14 | 23872 |
| Unicode | Modules | QRCoder | 2690.47 | 2758.06 | 2460.00–2770.19 | 23216 |
| Unicode | Modules | ZXing-C++ | 1164.88 | 1178.77 | 1051.57–1187.96 | 14648 |
| Unicode | Modules | ZXing.Net | 1677.37 | 1741.93 | 1582.86–1745.08 | 606585 |
| Unicode | Png | CodeGlyphX | 1852.23 | 1977.60 | 1702.81–2030.30 | 274832 |
| Unicode | Png | QRCoder | 2990.38 | 3913.46 | 2767.28–4359.25 | 26112 |
| Unicode | Png | ZXing-C++ | 7210.51 | 7671.88 | 6752.74–7692.96 | 6206646 |
| Unicode | Png | ZXing.Net | 8000.48 | 9630.12 | 7397.84–9646.89 | 6798281 |
| Url | Modules | CodeGlyphX | 126.27 | 151.90 | 100.33–157.85 | 5056 |
| Url | Modules | QRCoder | 305.41 | 340.27 | 290.82–345.15 | 7376 |
| Url | Modules | ZXing-C++ | 88.82 | 108.84 | 64.70–116.10 | 2696 |
| Url | Modules | ZXing.Net | 191.26 | 224.37 | 158.84–238.20 | 71064 |
| Url | Png | CodeGlyphX | 175.03 | 199.53 | 139.61–219.86 | 60272 |
| Url | Png | QRCoder | 413.79 | 508.07 | 387.09–544.17 | 8200 |
| Url | Png | ZXing-C++ | 1334.48 | 1405.53 | 1252.69–1411.94 | 1309072 |
| Url | Png | ZXing.Net | 1349.01 | 1458.50 | 1275.58–1468.95 | 1377468 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3102.04 | 3182.41 | 3084.29–3186.89 | 87.52 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 26.94 | 33.81 | 23.57–37.05 | 2.14 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 196.84 | 239.59 | 186.33–267.98 | 12.93 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3099.90 | 3346.42 | 3040.75–3451.05 | 87.77 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 26.14 | 33.00 | 22.26–35.22 | 2.15 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 191.48 | 246.20 | 177.37–296.89 | 12.94 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
