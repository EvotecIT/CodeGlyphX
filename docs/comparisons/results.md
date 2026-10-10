# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 174/267 | 18 | 2 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |
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
| Gs1DataBarExpandedStacked | 12 | 0 | 0 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 15 | 18 | 17 |
| QrCode | 37 | 22 | 26 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 174/267 correct positive images, compared with 162/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

This implementation changes retail scanline sampling only. The current main pass also completed one additional QR photograph, qrcode-3/11.png, with no QR source change; that result is retained as run variability rather than a retail-algorithm gain. Shared-host deadline effects remain visible in every negative and timing pass.

Host CPU samples before the main corpus pass: 10.9%, 9.7%, 25.2%. Before the supplementary negative pass: 31.0%, 26.0%, 31.2%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 49.7% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 23.0% |
| ccd0 | 3 | current | 31/31 | 0 | 0 | 20.7% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 19.7% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 17.1% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 10.8% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 11.6% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 22.6% |

## Generation on ccd0

Source `8fc393b060701645bc397e6fc9c128ead4db1a69`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 69.13 | 83.06 | 61.21–86.79 | 3832 |
| Numeric | Modules | QRCoder | 106.20 | 125.97 | 77.66–136.68 | 5568 |
| Numeric | Modules | ZXing-C++ | 37.03 | 69.01 | 29.19–75.67 | 1696 |
| Numeric | Modules | ZXing.Net | 48.70 | 92.48 | 35.56–99.71 | 31304 |
| Numeric | Png | CodeGlyphX | 114.75 | 158.25 | 81.47–166.62 | 40312 |
| Numeric | Png | QRCoder | 198.88 | 227.20 | 169.65–247.09 | 6568 |
| Numeric | Png | ZXing-C++ | 897.82 | 962.61 | 765.11–965.23 | 848303 |
| Numeric | Png | ZXing.Net | 933.48 | 1008.90 | 830.10–1022.57 | 877595 |
| Unicode | Modules | CodeGlyphX | 1669.20 | 1750.73 | 1605.18–1772.16 | 23872 |
| Unicode | Modules | QRCoder | 2675.36 | 2738.46 | 2536.66–2743.21 | 23216 |
| Unicode | Modules | ZXing-C++ | 1087.39 | 1134.38 | 1018.31–1145.75 | 14648 |
| Unicode | Modules | ZXing.Net | 1666.03 | 1792.17 | 1559.54–1823.00 | 606584 |
| Unicode | Png | CodeGlyphX | 1820.74 | 2054.65 | 1743.09–2098.34 | 274832 |
| Unicode | Png | QRCoder | 2961.79 | 3153.57 | 2876.52–3211.79 | 26112 |
| Unicode | Png | ZXing-C++ | 7673.23 | 8196.78 | 6973.10–8295.90 | 6206441 |
| Unicode | Png | ZXing.Net | 8405.11 | 9069.92 | 7798.24–9244.93 | 6798043 |
| Url | Modules | CodeGlyphX | 460.04 | 981.23 | 114.57–1021.99 | 5057 |
| Url | Modules | QRCoder | 961.41 | 1119.01 | 301.62–1163.42 | 7398 |
| Url | Modules | ZXing-C++ | 125.46 | 179.64 | 68.40–187.32 | 2697 |
| Url | Modules | ZXing.Net | 469.56 | 880.66 | 169.83–902.51 | 71114 |
| Url | Png | CodeGlyphX | 172.63 | 254.98 | 138.36–276.69 | 60283 |
| Url | Png | QRCoder | 420.58 | 475.44 | 394.65–482.03 | 8200 |
| Url | Png | ZXing-C++ | 1339.26 | 1463.08 | 1274.76–1487.78 | 1308977 |
| Url | Png | ZXing.Net | 1417.91 | 1570.09 | 1327.08–1641.83 | 1377459 |

## Generation on ccd1

Source `8fc393b060701645bc397e6fc9c128ead4db1a69`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 60.86 | 95.54 | 59.35–107.90 | 3832 |
| Numeric | Modules | QRCoder | 88.92 | 107.83 | 78.88–110.87 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.37 | 66.37 | 27.88–66.97 | 1696 |
| Numeric | Modules | ZXing.Net | 42.63 | 75.86 | 36.76–77.25 | 31304 |
| Numeric | Png | CodeGlyphX | 92.39 | 114.98 | 77.81–119.56 | 40312 |
| Numeric | Png | QRCoder | 161.61 | 185.53 | 154.46–188.50 | 6568 |
| Numeric | Png | ZXing-C++ | 711.36 | 779.86 | 686.24–803.04 | 848273 |
| Numeric | Png | ZXing.Net | 774.61 | 939.01 | 690.07–981.75 | 877580 |
| Unicode | Modules | CodeGlyphX | 1663.77 | 2050.52 | 1563.75–2192.04 | 23883 |
| Unicode | Modules | QRCoder | 2574.25 | 2811.18 | 2460.65–2868.98 | 23216 |
| Unicode | Modules | ZXing-C++ | 1073.57 | 1158.18 | 1038.04–1169.36 | 14648 |
| Unicode | Modules | ZXing.Net | 1632.73 | 1791.85 | 1565.15–1803.75 | 606584 |
| Unicode | Png | CodeGlyphX | 1737.16 | 1848.87 | 1684.24–1895.90 | 274832 |
| Unicode | Png | QRCoder | 2890.82 | 3155.27 | 2846.75–3228.13 | 26112 |
| Unicode | Png | ZXing-C++ | 6936.78 | 7358.88 | 6755.82–7416.57 | 6206949 |
| Unicode | Png | ZXing.Net | 7906.79 | 8987.34 | 7522.71–9227.69 | 6798466 |
| Url | Modules | CodeGlyphX | 115.04 | 170.51 | 99.80–186.65 | 5056 |
| Url | Modules | QRCoder | 306.23 | 337.74 | 286.14–339.45 | 7376 |
| Url | Modules | ZXing-C++ | 75.52 | 142.14 | 60.75–145.83 | 2696 |
| Url | Modules | ZXing.Net | 194.80 | 241.68 | 170.03–254.82 | 71064 |
| Url | Png | CodeGlyphX | 175.67 | 294.40 | 131.82–300.98 | 60272 |
| Url | Png | QRCoder | 416.28 | 635.12 | 398.97–655.64 | 8201 |
| Url | Png | ZXing-C++ | 1277.38 | 1523.09 | 1211.30–1566.83 | 1309008 |
| Url | Png | ZXing.Net | 1356.54 | 1566.27 | 1308.58–1627.06 | 1377448 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3277.49 | 3605.13 | 3212.37–3818.95 | 14–15 | 0–0 |
| ccd0 | ZXing-C++ | 23.30 | 34.62 | 21.36–34.64 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 179.51 | 212.52 | 164.77–233.66 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3217.74 | 3295.88 | 3181.07–3300.40 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 22.53 | 33.15 | 18.77–40.13 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 165.54 | 197.71 | 159.62–221.62 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
