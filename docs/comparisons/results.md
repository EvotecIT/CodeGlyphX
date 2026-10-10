# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 162/267 | 18 | 2 | 0 | 1 |
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
| Ean | 30 | 4 | 15 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 0 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 15 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 11 | 19 | 19 |
| UpcE | 15 | 3 | 10 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 162/267 correct positive images, compared with 162/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

Host CPU samples before the main corpus pass: 37.4%, 29.5%, 30.8%. Before the supplementary negative pass: 51.7%, 48.7%, 31.9%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 27/31 | 4 | 0 | 40.0% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 32.8% |
| ccd0 | 3 | current | 28/31 | 3 | 0 | 25.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 42.9% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 40.1% |
| ccd1 | 2 | previous | 26/31 | 5 | 0 | 34.8% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 85.4% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 31.2% |

## Generation on ccd0

Source `8d93e8e3cd502e482c7764d5f2909c5e2222b0ab`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 104.55 | 135.72 | 67.05–138.19 | 3832 |
| Numeric | Modules | QRCoder | 136.52 | 175.95 | 84.06–185.82 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.99 | 80.60 | 35.69–80.99 | 1696 |
| Numeric | Modules | ZXing.Net | 64.43 | 91.92 | 54.47–98.63 | 31304 |
| Numeric | Png | CodeGlyphX | 132.44 | 171.07 | 105.11–175.81 | 40312 |
| Numeric | Png | QRCoder | 218.92 | 274.98 | 210.03–294.67 | 6568 |
| Numeric | Png | ZXing-C++ | 962.13 | 1049.83 | 801.17–1057.89 | 848273 |
| Numeric | Png | ZXing.Net | 951.70 | 1012.85 | 850.01–1044.13 | 877558 |
| Unicode | Modules | CodeGlyphX | 1855.06 | 1983.27 | 1742.46–1995.45 | 23872 |
| Unicode | Modules | QRCoder | 2870.33 | 3011.03 | 2753.43–3012.44 | 23216 |
| Unicode | Modules | ZXing-C++ | 1201.04 | 1231.99 | 1121.05–1250.17 | 14648 |
| Unicode | Modules | ZXing.Net | 1830.44 | 1904.74 | 1701.36–1935.08 | 606589 |
| Unicode | Png | CodeGlyphX | 2140.11 | 2288.45 | 2032.92–2296.67 | 274832 |
| Unicode | Png | QRCoder | 3371.50 | 3471.38 | 3255.55–3512.23 | 26112 |
| Unicode | Png | ZXing-C++ | 8588.87 | 9083.65 | 8292.27–9089.17 | 6206517 |
| Unicode | Png | ZXing.Net | 10005.51 | 11085.01 | 9472.54–11190.99 | 6798008 |
| Url | Modules | CodeGlyphX | 258.11 | 1146.06 | 119.97–1255.08 | 5057 |
| Url | Modules | QRCoder | 1046.23 | 1237.48 | 369.89–1244.98 | 7395 |
| Url | Modules | ZXing-C++ | 116.23 | 184.97 | 80.41–187.29 | 2697 |
| Url | Modules | ZXing.Net | 347.95 | 1027.19 | 213.23–1103.81 | 71114 |
| Url | Png | CodeGlyphX | 235.48 | 298.35 | 180.42–328.68 | 60278 |
| Url | Png | QRCoder | 506.67 | 581.20 | 464.23–623.51 | 8200 |
| Url | Png | ZXing-C++ | 1616.16 | 1715.09 | 1516.54–1755.63 | 1308999 |
| Url | Png | ZXing.Net | 1756.67 | 1948.01 | 1649.37–1999.96 | 1377417 |

## Generation on ccd1

Source `8d93e8e3cd502e482c7764d5f2909c5e2222b0ab`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 68.38 | 117.00 | 61.35–144.12 | 3832 |
| Numeric | Modules | QRCoder | 100.49 | 125.46 | 77.86–126.17 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.57 | 62.85 | 28.95–64.27 | 1696 |
| Numeric | Modules | ZXing.Net | 50.86 | 77.41 | 37.52–83.80 | 31304 |
| Numeric | Png | CodeGlyphX | 88.08 | 112.49 | 77.91–114.90 | 40312 |
| Numeric | Png | QRCoder | 170.79 | 191.15 | 149.25–195.13 | 6568 |
| Numeric | Png | ZXing-C++ | 746.75 | 834.04 | 721.73–844.46 | 848283 |
| Numeric | Png | ZXing.Net | 810.55 | 927.22 | 754.13–942.16 | 877560 |
| Unicode | Modules | CodeGlyphX | 1654.69 | 2236.44 | 1595.16–2424.35 | 23872 |
| Unicode | Modules | QRCoder | 2608.17 | 3124.39 | 2466.02–3295.53 | 23216 |
| Unicode | Modules | ZXing-C++ | 1097.51 | 1449.74 | 1057.81–1464.58 | 14648 |
| Unicode | Modules | ZXing.Net | 1649.77 | 1929.32 | 1482.82–1987.86 | 606584 |
| Unicode | Png | CodeGlyphX | 1833.23 | 2143.47 | 1751.19–2157.57 | 274832 |
| Unicode | Png | QRCoder | 2963.20 | 3895.34 | 2872.28–3968.41 | 26112 |
| Unicode | Png | ZXing-C++ | 7061.25 | 12023.30 | 6842.59–17483.59 | 6206834 |
| Unicode | Png | ZXing.Net | 8140.20 | 9875.46 | 7611.72–10155.24 | 6798400 |
| Url | Modules | CodeGlyphX | 123.90 | 146.49 | 103.54–152.02 | 5056 |
| Url | Modules | QRCoder | 317.00 | 352.74 | 296.61–354.51 | 7376 |
| Url | Modules | ZXing-C++ | 88.76 | 107.95 | 62.69–109.27 | 2696 |
| Url | Modules | ZXing.Net | 183.91 | 238.70 | 160.70–245.43 | 71065 |
| Url | Png | CodeGlyphX | 165.27 | 203.06 | 128.68–214.06 | 60272 |
| Url | Png | QRCoder | 424.02 | 529.00 | 399.33–530.67 | 8200 |
| Url | Png | ZXing-C++ | 1320.41 | 1542.09 | 1225.22–1563.17 | 1309000 |
| Url | Png | ZXing.Net | 1454.33 | 1697.02 | 1287.56–1740.28 | 1377460 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3343.65 | 3384.55 | 3292.53–3398.26 | 15–15 | 0–0 |
| ccd0 | ZXing-C++ | 27.58 | 31.03 | 22.20–31.18 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 195.33 | 209.52 | 181.62–211.02 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3202.66 | 3293.43 | 3138.53–3302.29 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 21.99 | 28.37 | 18.92–30.86 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 172.29 | 188.37 | 145.02–190.06 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
