# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 155/267 | 18 | 2 | 0 | 2 |
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
| Pdf417 | 18 | 2 | 15 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 11 | 19 | 19 |
| UpcE | 15 | 3 | 10 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up improves CodeGlyphX from 153 to 155/267 positive images compared with the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

Host CPU samples before the main corpus pass: 54.5%, 74.7%, 74.6%. Before the supplementary negative pass: 21.0%, 22.0%, 17.8%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 27/31 | 4 | 0 | 87.7% |
| ccd0 | 2 | current | 28/31 | 3 | 0 | 75.8% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 23.7% |
| ccd0 | 4 | previous | 23/31 | 8 | 0 | 19.0% |
| ccd1 | 1 | current | 19/31 | 12 | 0 | 87.6% |
| ccd1 | 2 | previous | 27/31 | 4 | 0 | 69.4% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 83.4% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 74.1% |

## Generation on ccd0

Source `68bebf1233cf6f0543cc6085a7a9b42c3c3b0163`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 103.92 | 108.29 | 66.84–111.41 | 3832 |
| Numeric | Modules | QRCoder | 126.78 | 165.84 | 91.06–175.11 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.56 | 78.30 | 41.32–80.45 | 1696 |
| Numeric | Modules | ZXing.Net | 76.44 | 99.37 | 47.56–99.98 | 31304 |
| Numeric | Png | CodeGlyphX | 123.61 | 160.37 | 97.30–170.31 | 40312 |
| Numeric | Png | QRCoder | 216.69 | 246.30 | 179.42–249.31 | 6568 |
| Numeric | Png | ZXing-C++ | 938.82 | 1002.05 | 878.40–1008.48 | 848286 |
| Numeric | Png | ZXing.Net | 936.49 | 998.30 | 878.13–1001.19 | 877590 |
| Unicode | Modules | CodeGlyphX | 2091.20 | 2231.62 | 1785.29–2303.57 | 23872 |
| Unicode | Modules | QRCoder | 3289.04 | 3523.63 | 2815.05–3611.45 | 23216 |
| Unicode | Modules | ZXing-C++ | 1373.73 | 2669.87 | 1200.43–3987.95 | 14648 |
| Unicode | Modules | ZXing.Net | 1984.95 | 2185.38 | 1817.75–2210.18 | 606584 |
| Unicode | Png | CodeGlyphX | 2031.24 | 2468.27 | 1800.71–2471.08 | 274843 |
| Unicode | Png | QRCoder | 3372.86 | 4637.04 | 3120.46–5038.39 | 26112 |
| Unicode | Png | ZXing-C++ | 8462.98 | 9296.72 | 8037.86–9297.03 | 6206790 |
| Unicode | Png | ZXing.Net | 9885.09 | 11577.53 | 8934.83–11858.45 | 6798183 |
| Url | Modules | CodeGlyphX | 979.94 | 1179.09 | 490.12–1260.81 | 5057 |
| Url | Modules | QRCoder | 1157.59 | 1340.36 | 543.66–1367.53 | 7408 |
| Url | Modules | ZXing-C++ | 201.02 | 283.27 | 105.19–288.21 | 2713 |
| Url | Modules | ZXing.Net | 986.13 | 1238.45 | 213.64–1293.05 | 71135 |
| Url | Png | CodeGlyphX | 192.36 | 269.56 | 144.34–289.47 | 60278 |
| Url | Png | QRCoder | 505.43 | 572.39 | 449.14–574.53 | 8200 |
| Url | Png | ZXing-C++ | 1577.16 | 1683.32 | 1454.42–1712.97 | 1309037 |
| Url | Png | ZXing.Net | 1667.37 | 1809.32 | 1585.14–1838.50 | 1377455 |

## Generation on ccd1

Source `68bebf1233cf6f0543cc6085a7a9b42c3c3b0163`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 74.39 | 95.47 | 60.67–98.08 | 3832 |
| Numeric | Modules | QRCoder | 91.96 | 118.14 | 75.84–124.95 | 5568 |
| Numeric | Modules | ZXing-C++ | 33.45 | 56.51 | 28.53–57.81 | 1696 |
| Numeric | Modules | ZXing.Net | 46.33 | 79.14 | 36.77–79.73 | 31304 |
| Numeric | Png | CodeGlyphX | 104.73 | 114.57 | 78.10–116.10 | 40312 |
| Numeric | Png | QRCoder | 170.57 | 203.49 | 155.54–210.25 | 6568 |
| Numeric | Png | ZXing-C++ | 786.86 | 854.06 | 746.94–870.41 | 848321 |
| Numeric | Png | ZXing.Net | 804.63 | 922.29 | 710.30–971.60 | 877618 |
| Unicode | Modules | CodeGlyphX | 1876.97 | 2841.36 | 1609.45–3760.85 | 23872 |
| Unicode | Modules | QRCoder | 2830.78 | 3529.20 | 2562.15–3830.32 | 23216 |
| Unicode | Modules | ZXing-C++ | 1149.92 | 1798.42 | 1078.74–2278.40 | 14648 |
| Unicode | Modules | ZXing.Net | 1834.32 | 2310.30 | 1646.01–2740.34 | 606585 |
| Unicode | Png | CodeGlyphX | 1742.19 | 1967.96 | 1696.76–2058.83 | 274832 |
| Unicode | Png | QRCoder | 2997.20 | 3318.80 | 2856.26–3467.22 | 26112 |
| Unicode | Png | ZXing-C++ | 7188.47 | 7812.44 | 6942.91–8164.34 | 6206602 |
| Unicode | Png | ZXing.Net | 8115.04 | 9619.07 | 7777.98–9743.91 | 6798261 |
| Url | Modules | CodeGlyphX | 794.08 | 919.40 | 627.43–946.99 | 5063 |
| Url | Modules | QRCoder | 839.29 | 1089.35 | 764.74–1094.87 | 7409 |
| Url | Modules | ZXing-C++ | 134.67 | 182.76 | 102.72–187.21 | 2711 |
| Url | Modules | ZXing.Net | 717.45 | 854.33 | 673.09–869.99 | 71146 |
| Url | Png | CodeGlyphX | 160.63 | 326.42 | 129.69–449.57 | 60336 |
| Url | Png | QRCoder | 416.04 | 471.33 | 400.07–483.66 | 8200 |
| Url | Png | ZXing-C++ | 1283.24 | 1358.01 | 1148.09–1383.92 | 1309111 |
| Url | Png | ZXing.Net | 1342.24 | 1475.59 | 1206.42–1478.37 | 1377587 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3203.50 | 3329.78 | 3141.65–3340.15 | 15–15 | 0–0 |
| ccd0 | ZXing-C++ | 25.07 | 34.25 | 21.29–36.54 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 186.40 | 242.28 | 167.79–265.25 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3183.40 | 3226.15 | 3148.12–3226.24 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 22.85 | 27.84 | 19.79–28.34 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 175.49 | 210.04 | 159.99–212.30 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
