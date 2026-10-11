# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 198/267 | 6 | 3 | 0 | 3 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 9 | 10 | 12 |
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

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png, falsepositives-2/06.png

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

This follow-up records 198/267 correct positive images versus 198/267 in the previous pass. Previously correct images lost: none; gained: none. Paired negative completion is [29, 30, 30, 30]/31 with the current binary and [29, 29, 27, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. QR candidate and extraction reuse removes repeated work without changing the total or family recognition allowances. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 58.2%, 56.2%, 58.7%. Before the supplementary negative pass: 41.1%, 47.6%, 42.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 19/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 35.6% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 25.8% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 29.2% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 32.6% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 25.7% |
| ccd1 | 2 | previous | 27/31 | 4 | 0 | 81.7% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 65.1% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 25.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 57.7% | 69.4% | 46.0% |
| decoding-ccd0 | 40.0% | 53.8% | 26.2% |
| decoding-ccd1 | 33.5% | 48.8% | 18.2% |
| diagnostic-final-All-32-final | 71.8% | 80.0% | 63.6% |
| diagnostic-final-All-32-qualified | 68.8% | 75.6% | 61.4% |
| diagnostic-final-QrCode-1-final | 78.1% | 83.7% | 72.6% |
| diagnostic-final-QrCode-1-qualified | 96.0% | 98.0% | 94.1% |
| diagnostic-final-QrCode-32-final | 49.9% | 61.0% | 38.8% |
| diagnostic-final-QrCode-32-qualified | 77.9% | 84.8% | 71.0% |
| generation-ccd0 | 54.4% | 66.6% | 42.1% |
| generation-ccd1 | 40.8% | 56.4% | 25.2% |
| holdout-current | 34.9% | 49.6% | 20.2% |
| negative-rotation-ccd0-0-previous | 35.6% | 52.1% | 19.2% |
| negative-rotation-ccd0-1-current | 25.8% | 40.1% | 11.6% |
| negative-rotation-ccd0-2-current | 29.2% | 45.3% | 13.1% |
| negative-rotation-ccd0-3-previous | 32.6% | 47.7% | 17.7% |
| negative-rotation-ccd1-0-current | 25.7% | 40.3% | 11.1% |
| negative-rotation-ccd1-1-previous | 81.7% | 86.4% | 76.9% |
| negative-rotation-ccd1-2-previous | 65.1% | 73.5% | 56.8% |
| negative-rotation-ccd1-3-current | 25.8% | 39.8% | 11.7% |
| negatives-ccd0 | 43.6% | 57.1% | 30.1% |

## Generation on ccd0

Source `98217be2a1949d71a74d1319f9310617357aca6f`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 69.14 | 158.82 | 60.95–223.23 | 3832 |
| Numeric | Modules | QRCoder | 101.35 | 203.25 | 75.33–261.13 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.08 | 61.36 | 28.64–61.52 | 1696 |
| Numeric | Modules | ZXing.Net | 48.56 | 93.68 | 38.29–98.33 | 31304 |
| Numeric | Png | CodeGlyphX | 79.10 | 112.94 | 77.25–126.38 | 40312 |
| Numeric | Png | QRCoder | 149.35 | 156.04 | 143.60–157.52 | 6568 |
| Numeric | Png | ZXing-C++ | 676.89 | 706.45 | 626.06–707.12 | 848301 |
| Numeric | Png | ZXing.Net | 746.58 | 850.34 | 678.35–897.51 | 877575 |
| Unicode | Modules | CodeGlyphX | 1794.59 | 2823.11 | 1529.34–2898.95 | 23872 |
| Unicode | Modules | QRCoder | 2776.04 | 4352.86 | 2410.49–4674.04 | 23216 |
| Unicode | Modules | ZXing-C++ | 1198.51 | 2334.84 | 1033.22–2574.67 | 14648 |
| Unicode | Modules | ZXing.Net | 1918.28 | 2530.62 | 1546.00–2640.39 | 606584 |
| Unicode | Png | CodeGlyphX | 2135.48 | 2393.63 | 1731.72–2492.66 | 274832 |
| Unicode | Png | QRCoder | 3364.28 | 4051.93 | 2918.32–4374.40 | 26112 |
| Unicode | Png | ZXing-C++ | 8276.49 | 10770.66 | 6929.85–13054.35 | 6206794 |
| Unicode | Png | ZXing.Net | 10252.22 | 12412.42 | 9070.08–14487.41 | 6798183 |
| Url | Modules | CodeGlyphX | 923.15 | 1231.10 | 629.58–1334.11 | 5057 |
| Url | Modules | QRCoder | 1225.61 | 1482.25 | 821.95–1497.00 | 7415 |
| Url | Modules | ZXing-C++ | 167.28 | 281.77 | 120.95–288.10 | 2707 |
| Url | Modules | ZXing.Net | 908.88 | 1241.07 | 328.76–1315.59 | 71141 |
| Url | Png | CodeGlyphX | 154.73 | 218.38 | 127.21–244.81 | 60278 |
| Url | Png | QRCoder | 387.60 | 457.63 | 374.36–475.36 | 8200 |
| Url | Png | ZXing-C++ | 1133.08 | 1281.91 | 1083.53–1341.27 | 1309016 |
| Url | Png | ZXing.Net | 1237.09 | 1375.10 | 1156.41–1403.96 | 1377466 |

## Generation on ccd1

Source `98217be2a1949d71a74d1319f9310617357aca6f`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 104.73 | 149.23 | 70.51–151.97 | 3832 |
| Numeric | Modules | QRCoder | 124.67 | 190.35 | 73.43–200.14 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.27 | 96.86 | 36.88–106.06 | 1696 |
| Numeric | Modules | ZXing.Net | 70.99 | 111.65 | 57.60–120.94 | 31304 |
| Numeric | Png | CodeGlyphX | 109.88 | 158.98 | 78.65–200.01 | 40312 |
| Numeric | Png | QRCoder | 157.42 | 219.89 | 141.30–235.64 | 6568 |
| Numeric | Png | ZXing-C++ | 796.38 | 942.62 | 707.40–964.60 | 848296 |
| Numeric | Png | ZXing.Net | 851.33 | 924.96 | 754.64–941.58 | 877598 |
| Unicode | Modules | CodeGlyphX | 1634.91 | 1905.42 | 1545.81–2069.90 | 23872 |
| Unicode | Modules | QRCoder | 2525.14 | 2799.67 | 2362.74–2863.38 | 23216 |
| Unicode | Modules | ZXing-C++ | 1074.64 | 1245.12 | 1012.30–1301.91 | 14648 |
| Unicode | Modules | ZXing.Net | 1629.09 | 1865.80 | 1492.76–1924.99 | 606584 |
| Unicode | Png | CodeGlyphX | 1880.50 | 2171.98 | 1700.92–2222.24 | 274832 |
| Unicode | Png | QRCoder | 3183.36 | 3495.70 | 2878.34–3549.32 | 26112 |
| Unicode | Png | ZXing-C++ | 7698.96 | 8854.73 | 6892.51–9164.61 | 6206769 |
| Unicode | Png | ZXing.Net | 8539.48 | 10114.56 | 7476.77–10407.79 | 6798383 |
| Url | Modules | CodeGlyphX | 777.17 | 970.18 | 674.27–995.98 | 5057 |
| Url | Modules | QRCoder | 939.87 | 1154.75 | 805.96–1155.40 | 7409 |
| Url | Modules | ZXing-C++ | 159.77 | 200.80 | 90.79–214.13 | 2715 |
| Url | Modules | ZXing.Net | 770.82 | 895.97 | 627.36–921.29 | 71146 |
| Url | Png | CodeGlyphX | 192.71 | 302.78 | 140.31–356.47 | 60294 |
| Url | Png | QRCoder | 431.92 | 501.32 | 401.96–561.52 | 8200 |
| Url | Png | ZXing-C++ | 1366.30 | 1463.99 | 1211.41–1471.18 | 1309022 |
| Url | Png | ZXing.Net | 1571.66 | 1774.25 | 1335.34–1896.67 | 1377480 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1435.77 | 1487.96 | 1370.12–1490.65 | 89.71 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 22.35 | 24.73 | 19.90–25.79 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 171.59 | 194.89 | 158.99–198.48 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1408.50 | 1599.98 | 1321.95–1629.40 | 89.86 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 21.65 | 27.30 | 18.41–30.04 | 2.20 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 159.68 | 215.17 | 148.02–223.44 | 13.00 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
