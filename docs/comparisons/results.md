# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 151/267 | 18 | 2 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
| Aztec | 12 | 4 | 4 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 10 | 13 | 13 |
| Code39 | 12 | 1 | 9 | 12 | 12 |
| Code93 | 3 | 2 | 2 | 3 | 3 |
| DataMatrix | 18 | 8 | 12 | 18 | 18 |
| Ean | 30 | 4 | 15 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 0 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 11 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 11 | 19 | 19 |
| UpcE | 15 | 3 | 10 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up improves CodeGlyphX from 121 to 151/267 positive images compared with the [previous pass](evidence/previous-candidate-corpus.json). Previously correct images lost in this follow-up: none in this run.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 23/25 | 0 | 2 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

## Generation on ccd0

Source `3b9515353e536bc549062acf840d5411d0c012f4`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 69.98 | 153.37 | 59.54–170.47 | 3832 |
| Numeric | Modules | QRCoder | 108.03 | 131.17 | 76.79–136.65 | 5568 |
| Numeric | Modules | ZXing-C++ | 46.34 | 77.40 | 28.33–89.88 | 1696 |
| Numeric | Modules | ZXing.Net | 61.93 | 155.87 | 37.34–200.76 | 31304 |
| Numeric | Png | CodeGlyphX | 103.21 | 136.74 | 76.59–165.90 | 40312 |
| Numeric | Png | QRCoder | 172.67 | 234.88 | 146.55–243.73 | 6610 |
| Numeric | Png | ZXing-C++ | 715.37 | 1083.37 | 602.14–1084.33 | 848330 |
| Numeric | Png | ZXing.Net | 864.98 | 1121.12 | 657.48–1123.18 | 877608 |
| Unicode | Modules | CodeGlyphX | 1741.59 | 2416.76 | 1472.16–2644.83 | 23872 |
| Unicode | Modules | QRCoder | 2801.01 | 3484.71 | 2354.50–3565.09 | 23216 |
| Unicode | Modules | ZXing-C++ | 1072.59 | 1527.78 | 969.84–1541.35 | 14648 |
| Unicode | Modules | ZXing.Net | 1638.49 | 2324.42 | 1397.49–2541.09 | 606585 |
| Unicode | Png | CodeGlyphX | 1763.47 | 2112.14 | 1616.11–2115.01 | 274832 |
| Unicode | Png | QRCoder | 2940.61 | 3208.22 | 2719.61–3332.55 | 26112 |
| Unicode | Png | ZXing-C++ | 6527.19 | 7678.23 | 6023.54–7792.80 | 6206767 |
| Unicode | Png | ZXing.Net | 7803.89 | 9055.86 | 6871.47–9524.63 | 6798212 |
| Url | Modules | CodeGlyphX | 612.58 | 843.51 | 577.40–851.49 | 5057 |
| Url | Modules | QRCoder | 732.13 | 888.29 | 686.32–922.79 | 7409 |
| Url | Modules | ZXing-C++ | 116.44 | 148.53 | 93.47–167.02 | 2713 |
| Url | Modules | ZXing.Net | 596.37 | 744.03 | 552.05–789.72 | 71146 |
| Url | Png | CodeGlyphX | 158.70 | 436.72 | 123.25–695.44 | 60351 |
| Url | Png | QRCoder | 401.58 | 475.94 | 377.83–476.94 | 8382 |
| Url | Png | ZXing-C++ | 1254.82 | 1563.70 | 1072.61–1621.20 | 1309163 |
| Url | Png | ZXing.Net | 1279.40 | 1797.89 | 1219.08–2237.49 | 1377660 |

## Generation on ccd1

Source `3b9515353e536bc549062acf840d5411d0c012f4`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 59.50 | 88.10 | 58.41–95.83 | 3832 |
| Numeric | Modules | QRCoder | 70.83 | 94.30 | 69.95–94.34 | 5568 |
| Numeric | Modules | ZXing-C++ | 27.88 | 49.52 | 27.53–49.72 | 1696 |
| Numeric | Modules | ZXing.Net | 36.10 | 60.20 | 35.45–60.63 | 31304 |
| Numeric | Png | CodeGlyphX | 83.44 | 100.23 | 74.42–101.43 | 40312 |
| Numeric | Png | QRCoder | 135.73 | 137.27 | 134.53–137.68 | 6568 |
| Numeric | Png | ZXing-C++ | 585.07 | 615.44 | 562.15–630.78 | 848332 |
| Numeric | Png | ZXing.Net | 605.51 | 632.12 | 569.34–633.03 | 877592 |
| Unicode | Modules | CodeGlyphX | 1886.81 | 2557.78 | 1492.65–2591.60 | 23872 |
| Unicode | Modules | QRCoder | 2916.85 | 3625.63 | 2255.48–3765.79 | 23216 |
| Unicode | Modules | ZXing-C++ | 1310.68 | 1936.54 | 975.73–2419.75 | 14648 |
| Unicode | Modules | ZXing.Net | 1669.24 | 2330.74 | 1390.07–2480.84 | 606585 |
| Unicode | Png | CodeGlyphX | 1675.49 | 2286.64 | 1609.39–2705.90 | 274832 |
| Unicode | Png | QRCoder | 2768.72 | 3322.24 | 2663.23–3415.40 | 26112 |
| Unicode | Png | ZXing-C++ | 6528.87 | 8151.58 | 5913.22–8281.54 | 6206863 |
| Unicode | Png | ZXing.Net | 7609.44 | 9040.10 | 6422.38–9946.97 | 6798213 |
| Url | Modules | CodeGlyphX | 591.62 | 669.11 | 576.59–707.76 | 5057 |
| Url | Modules | QRCoder | 691.14 | 760.22 | 669.87–808.92 | 7409 |
| Url | Modules | ZXing-C++ | 107.84 | 123.82 | 90.51–125.36 | 2713 |
| Url | Modules | ZXing.Net | 582.20 | 653.97 | 546.98–703.43 | 71146 |
| Url | Png | CodeGlyphX | 145.42 | 422.63 | 123.98–663.26 | 60352 |
| Url | Png | QRCoder | 363.79 | 777.62 | 360.19–781.20 | 8206 |
| Url | Png | ZXing-C++ | 1034.59 | 1064.36 | 957.17–1073.18 | 1309103 |
| Url | Png | ZXing.Net | 1117.42 | 1428.41 | 1051.91–1432.70 | 1377592 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3049.34 | 3090.96 | 3040.80–3093.05 | 15–15 | 0–0 |
| ccd0 | ZXing-C++ | 20.14 | 23.20 | 19.54–24.47 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 149.13 | 181.38 | 143.40–182.31 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3134.39 | 3355.55 | 3049.00–3401.33 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 20.08 | 40.11 | 18.06–43.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 168.24 | 247.40 | 145.36–248.86 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
