# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 153/267 | 18 | 2 | 0 | 7 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 5 | 10 | 12 |
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
| Pdf417 | 18 | 2 | 13 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 11 | 19 | 19 |
| UpcE | 15 | 3 | 10 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up improves CodeGlyphX from 151 to 153/267 positive images compared with the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png, falsepositives/03.png, falsepositives-2/17.png, falsepositives-2/16.png, falsepositives-2/18.png

Host CPU samples before the main corpus pass: 73.3%, 92.7%, 68.4%. Before the supplementary negative pass: 47.3%, 61.3%, 45.1%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 25/31 | 6 | 0 | 64.4% |
| ccd0 | 2 | current | 26/31 | 5 | 0 | 63.2% |
| ccd0 | 3 | current | 28/31 | 3 | 0 | 51.7% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 49.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 58.2% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 28.1% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 26.5% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 19.4% |

## Generation on ccd0

Source `5e9962442a7d5b88a78ed5fefdf541eb3bcc53aa`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 91.08 | 119.51 | 66.99–122.40 | 3832 |
| Numeric | Modules | QRCoder | 111.96 | 139.35 | 83.16–141.01 | 5568 |
| Numeric | Modules | ZXing-C++ | 40.10 | 67.59 | 31.71–76.26 | 1696 |
| Numeric | Modules | ZXing.Net | 66.35 | 97.33 | 56.65–122.79 | 31304 |
| Numeric | Png | CodeGlyphX | 109.91 | 127.02 | 87.73–127.64 | 40312 |
| Numeric | Png | QRCoder | 190.15 | 241.10 | 153.11–242.37 | 6568 |
| Numeric | Png | ZXing-C++ | 885.59 | 993.79 | 805.38–997.22 | 848308 |
| Numeric | Png | ZXing.Net | 914.32 | 987.30 | 841.82–992.10 | 877567 |
| Unicode | Modules | CodeGlyphX | 1727.63 | 1828.50 | 1642.80–1850.25 | 23872 |
| Unicode | Modules | QRCoder | 2679.19 | 2838.07 | 2551.13–2848.62 | 23216 |
| Unicode | Modules | ZXing-C++ | 1123.17 | 1196.71 | 1087.78–1221.84 | 14648 |
| Unicode | Modules | ZXing.Net | 1761.22 | 1814.71 | 1628.68–1828.60 | 606585 |
| Unicode | Png | CodeGlyphX | 1945.07 | 2321.81 | 1809.27–2580.07 | 274843 |
| Unicode | Png | QRCoder | 3164.12 | 3495.88 | 3051.67–3570.89 | 26112 |
| Unicode | Png | ZXing-C++ | 8199.73 | 9250.48 | 7747.55–9352.43 | 6206777 |
| Unicode | Png | ZXing.Net | 9419.50 | 10501.76 | 8916.23–10834.56 | 6798233 |
| Url | Modules | CodeGlyphX | 864.63 | 1083.44 | 567.24–1153.57 | 5057 |
| Url | Modules | QRCoder | 988.49 | 1211.68 | 879.17–1326.18 | 7409 |
| Url | Modules | ZXing-C++ | 175.73 | 234.58 | 88.50–238.52 | 2710 |
| Url | Modules | ZXing.Net | 878.56 | 997.76 | 802.21–1051.91 | 71152 |
| Url | Png | CodeGlyphX | 212.56 | 335.80 | 160.22–365.19 | 60278 |
| Url | Png | QRCoder | 460.33 | 650.84 | 433.75–715.79 | 8200 |
| Url | Png | ZXing-C++ | 1503.58 | 3099.52 | 1423.71–3774.37 | 1308994 |
| Url | Png | ZXing.Net | 1573.65 | 2718.88 | 1509.47–3966.67 | 1377454 |

## Generation on ccd1

Source `5e9962442a7d5b88a78ed5fefdf541eb3bcc53aa`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 73.31 | 112.84 | 60.38–124.41 | 3832 |
| Numeric | Modules | QRCoder | 98.05 | 121.12 | 73.77–123.21 | 5568 |
| Numeric | Modules | ZXing-C++ | 34.04 | 58.78 | 29.09–58.78 | 1696 |
| Numeric | Modules | ZXing.Net | 44.47 | 63.74 | 39.82–74.63 | 31304 |
| Numeric | Png | CodeGlyphX | 92.48 | 113.22 | 77.48–113.39 | 40312 |
| Numeric | Png | QRCoder | 155.23 | 189.16 | 148.99–217.22 | 6568 |
| Numeric | Png | ZXing-C++ | 760.52 | 826.58 | 710.41–842.75 | 848314 |
| Numeric | Png | ZXing.Net | 777.29 | 835.24 | 721.26–850.37 | 877637 |
| Unicode | Modules | CodeGlyphX | 1607.63 | 1689.44 | 1558.02–1730.94 | 23872 |
| Unicode | Modules | QRCoder | 2497.54 | 2549.49 | 2386.65–2557.02 | 23216 |
| Unicode | Modules | ZXing-C++ | 1067.24 | 1085.86 | 1037.40–1096.99 | 14648 |
| Unicode | Modules | ZXing.Net | 1608.60 | 1647.58 | 1538.75–1649.21 | 606585 |
| Unicode | Png | CodeGlyphX | 1765.77 | 2040.85 | 1691.67–2099.89 | 274832 |
| Unicode | Png | QRCoder | 2918.26 | 3432.17 | 2834.69–3474.92 | 26112 |
| Unicode | Png | ZXing-C++ | 7033.50 | 7797.22 | 6660.61–7853.81 | 6206758 |
| Unicode | Png | ZXing.Net | 7748.11 | 8727.89 | 7304.11–9422.44 | 6798432 |
| Url | Modules | CodeGlyphX | 712.53 | 987.49 | 673.48–1005.94 | 5057 |
| Url | Modules | QRCoder | 828.36 | 1018.34 | 740.85–1040.45 | 7409 |
| Url | Modules | ZXing-C++ | 131.33 | 194.03 | 103.62–218.66 | 2711 |
| Url | Modules | ZXing.Net | 732.96 | 914.90 | 635.26–953.35 | 71146 |
| Url | Png | CodeGlyphX | 161.31 | 220.96 | 127.82–272.26 | 60294 |
| Url | Png | QRCoder | 400.79 | 481.50 | 381.09–493.28 | 8200 |
| Url | Png | ZXing-C++ | 1264.22 | 1312.86 | 1197.49–1348.75 | 1309100 |
| Url | Png | ZXing.Net | 1325.14 | 1451.38 | 1207.52–1537.25 | 1377554 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3271.03 | 3680.79 | 3096.48–3703.32 | 14–15 | 0–0 |
| ccd0 | ZXing-C++ | 26.14 | 31.94 | 20.37–33.52 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 195.06 | 215.05 | 152.20–221.05 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3176.70 | 3233.99 | 3105.72–3241.53 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 23.31 | 28.55 | 20.09–29.93 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 170.27 | 189.26 | 153.60–194.63 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
