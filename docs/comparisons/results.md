# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 162/267 | 18 | 2 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
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

This decoding follow-up improves CodeGlyphX from 155 to 162/267 positive images compared with the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

Host CPU samples before the main corpus pass: 57.0%, 45.2%, 38.0%. Before the supplementary negative pass: 99.9%, 99.8%, 91.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 22/25 | 0 | 3 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 74.7% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 33.5% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 27.1% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 49.3% |
| ccd1 | 1 | current | 29/31 | 2 | 0 | 51.7% |
| ccd1 | 2 | previous | 29/31 | 2 | 0 | 64.2% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 28.5% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 34.8% |

## Generation on ccd0

Source `7c6f8db8b11bd98bbf84e8173cf0496046ac9f97`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 92.96 | 122.20 | 77.97–127.99 | 3832 |
| Numeric | Modules | QRCoder | 123.85 | 151.60 | 103.06–157.46 | 5568 |
| Numeric | Modules | ZXing-C++ | 49.93 | 76.47 | 37.25–77.81 | 1696 |
| Numeric | Modules | ZXing.Net | 64.67 | 95.67 | 43.32–99.72 | 31304 |
| Numeric | Png | CodeGlyphX | 124.63 | 148.29 | 104.72–150.85 | 40312 |
| Numeric | Png | QRCoder | 196.48 | 232.60 | 176.32–259.19 | 6568 |
| Numeric | Png | ZXing-C++ | 939.45 | 1013.13 | 875.68–1025.14 | 848290 |
| Numeric | Png | ZXing.Net | 978.42 | 1099.82 | 919.16–1102.92 | 877583 |
| Unicode | Modules | CodeGlyphX | 2010.28 | 2869.28 | 1832.56–3155.34 | 23872 |
| Unicode | Modules | QRCoder | 3126.13 | 4396.74 | 2876.15–4909.16 | 23216 |
| Unicode | Modules | ZXing-C++ | 1354.95 | 1496.06 | 1118.09–1532.15 | 14648 |
| Unicode | Modules | ZXing.Net | 2051.86 | 3173.08 | 1818.51–4104.45 | 606585 |
| Unicode | Png | CodeGlyphX | 2089.02 | 2398.63 | 1881.20–2404.58 | 274832 |
| Unicode | Png | QRCoder | 3365.63 | 4022.50 | 3100.32–4077.88 | 26112 |
| Unicode | Png | ZXing-C++ | 8484.38 | 9278.27 | 8050.78–9383.56 | 6206509 |
| Unicode | Png | ZXing.Net | 10108.35 | 11875.68 | 9443.07–12829.04 | 6798036 |
| Url | Modules | CodeGlyphX | 390.66 | 1068.64 | 147.29–1077.15 | 5057 |
| Url | Modules | QRCoder | 990.60 | 1256.25 | 333.31–1303.79 | 7397 |
| Url | Modules | ZXing-C++ | 139.36 | 190.05 | 80.72–197.82 | 2703 |
| Url | Modules | ZXing.Net | 573.17 | 1053.71 | 205.72–1080.24 | 71113 |
| Url | Png | CodeGlyphX | 220.15 | 269.70 | 188.64–276.64 | 60283 |
| Url | Png | QRCoder | 470.14 | 530.69 | 445.50–546.49 | 8200 |
| Url | Png | ZXing-C++ | 1579.70 | 1609.48 | 1500.98–1612.37 | 1308981 |
| Url | Png | ZXing.Net | 1702.13 | 1774.14 | 1528.32–1794.70 | 1377424 |

## Generation on ccd1

Source `7c6f8db8b11bd98bbf84e8173cf0496046ac9f97`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 71.36 | 104.05 | 62.29–104.78 | 3832 |
| Numeric | Modules | QRCoder | 84.45 | 99.80 | 72.70–107.61 | 5568 |
| Numeric | Modules | ZXing-C++ | 31.28 | 61.39 | 28.83–65.34 | 1696 |
| Numeric | Modules | ZXing.Net | 49.02 | 80.34 | 38.23–81.42 | 31304 |
| Numeric | Png | CodeGlyphX | 88.40 | 130.09 | 77.81–132.49 | 40312 |
| Numeric | Png | QRCoder | 164.84 | 178.63 | 149.84–182.80 | 6568 |
| Numeric | Png | ZXing-C++ | 785.57 | 817.53 | 738.10–820.19 | 848291 |
| Numeric | Png | ZXing.Net | 789.73 | 878.72 | 762.94–955.87 | 877565 |
| Unicode | Modules | CodeGlyphX | 1730.55 | 2066.87 | 1620.45–2242.79 | 23872 |
| Unicode | Modules | QRCoder | 2657.30 | 3049.18 | 2492.02–3092.73 | 23216 |
| Unicode | Modules | ZXing-C++ | 1147.76 | 1291.05 | 1061.69–1327.57 | 14648 |
| Unicode | Modules | ZXing.Net | 1706.49 | 1932.47 | 1558.84–1947.91 | 606586 |
| Unicode | Png | CodeGlyphX | 2196.93 | 3152.44 | 1827.29–3525.05 | 274832 |
| Unicode | Png | QRCoder | 4215.27 | 5743.74 | 3072.46–6025.86 | 26112 |
| Unicode | Png | ZXing-C++ | 10476.05 | 17221.02 | 7241.99–21457.27 | 6206699 |
| Unicode | Png | ZXing.Net | 11835.01 | 19660.00 | 9182.80–21463.99 | 6798267 |
| Url | Modules | CodeGlyphX | 130.15 | 163.57 | 123.14–172.39 | 5056 |
| Url | Modules | QRCoder | 314.85 | 344.72 | 297.39–346.18 | 7376 |
| Url | Modules | ZXing-C++ | 74.62 | 109.07 | 68.09–114.02 | 2696 |
| Url | Modules | ZXing.Net | 189.91 | 213.74 | 162.98–221.72 | 71064 |
| Url | Png | CodeGlyphX | 173.14 | 192.22 | 145.07–197.74 | 60272 |
| Url | Png | QRCoder | 406.71 | 435.58 | 386.56–444.56 | 8200 |
| Url | Png | ZXing-C++ | 1294.67 | 1352.45 | 1244.63–1363.06 | 1309097 |
| Url | Png | ZXing.Net | 1415.32 | 1510.29 | 1373.97–1561.46 | 1377533 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3386.59 | 3756.88 | 3337.88–3783.51 | 14–15 | 0–0 |
| ccd0 | ZXing-C++ | 26.53 | 32.23 | 22.74–36.89 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 192.22 | 244.69 | 180.86–252.40 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3268.98 | 3331.77 | 3243.38–3348.77 | 15–15 | 0–0 |
| ccd1 | ZXing-C++ | 22.43 | 29.34 | 19.55–33.30 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 174.09 | 185.35 | 164.97–189.03 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
