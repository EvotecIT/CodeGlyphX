# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 173/267 | 18 | 2 | 0 | 1 |
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
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 173/267 correct positive images, compared with 174/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: qrcode-3/11.png

Previously completed main-corpus negatives now inconclusive: none in this run.

This implementation changes QR connected-component traversal only. Exact component equivalence is checked separately from public recognition and latency. The current main score is 173/267 versus 174/267 in the previous pass: qrcode-3/11.png, already variable before this change, is missed. Eight targeted alternating old/current passes across both cache domains also miss that photograph with DeadlineExceeded, four per binary. These probes do not isolate a traversal regression or establish a decoding gain; the main result is retained. Shared-host effects and all timing samples remain visible. Main negatives are still 11/12 completed, so a general timeout fix is not established.

Host CPU samples before the main corpus pass: 50.0%, 50.9%, 45.1%. Before the supplementary negative pass: 22.9%, 17.1%, 16.8%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 22.3% |
| ccd0 | 2 | current | 31/31 | 0 | 0 | 20.3% |
| ccd0 | 3 | current | 31/31 | 0 | 0 | 36.6% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 25.8% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 51.8% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 22.5% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 55.7% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 40.6% |

## Generation on ccd0

Source `419b84d950ea481a4775a7f7c717b9efa95bc520`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 102.41 | 133.71 | 63.99–134.72 | 3832 |
| Numeric | Modules | QRCoder | 122.58 | 179.81 | 105.02–203.69 | 5568 |
| Numeric | Modules | ZXing-C++ | 44.51 | 77.14 | 35.43–80.18 | 1696 |
| Numeric | Modules | ZXing.Net | 66.67 | 122.04 | 44.30–135.69 | 31304 |
| Numeric | Png | CodeGlyphX | 120.12 | 157.11 | 99.10–161.18 | 40312 |
| Numeric | Png | QRCoder | 216.73 | 270.67 | 187.88–297.92 | 6568 |
| Numeric | Png | ZXing-C++ | 938.47 | 1031.23 | 879.54–1037.69 | 848281 |
| Numeric | Png | ZXing.Net | 962.62 | 1069.73 | 852.26–1071.80 | 877534 |
| Unicode | Modules | CodeGlyphX | 1943.22 | 2074.20 | 1755.30–2123.07 | 23872 |
| Unicode | Modules | QRCoder | 3001.27 | 3383.06 | 2813.58–3541.48 | 23216 |
| Unicode | Modules | ZXing-C++ | 1210.88 | 1272.68 | 1159.63–1288.11 | 14648 |
| Unicode | Modules | ZXing.Net | 1878.39 | 2179.34 | 1780.20–2369.70 | 606585 |
| Unicode | Png | CodeGlyphX | 2050.96 | 2134.82 | 1828.35–2148.11 | 274832 |
| Unicode | Png | QRCoder | 3351.89 | 3497.56 | 3211.65–3522.27 | 26112 |
| Unicode | Png | ZXing-C++ | 8472.09 | 8749.41 | 7943.42–8823.14 | 6206441 |
| Unicode | Png | ZXing.Net | 10172.56 | 10771.19 | 9334.96–10819.18 | 6797980 |
| Url | Modules | CodeGlyphX | 407.99 | 1014.26 | 156.89–1113.47 | 5057 |
| Url | Modules | QRCoder | 604.17 | 1202.09 | 331.13–1301.72 | 7398 |
| Url | Modules | ZXing-C++ | 103.46 | 190.89 | 73.84–214.58 | 2697 |
| Url | Modules | ZXing.Net | 545.42 | 968.02 | 207.14–1021.04 | 71114 |
| Url | Png | CodeGlyphX | 197.76 | 312.96 | 180.25–317.01 | 60278 |
| Url | Png | QRCoder | 526.86 | 651.85 | 452.39–653.94 | 8200 |
| Url | Png | ZXing-C++ | 1537.29 | 1826.70 | 1403.93–1978.43 | 1308985 |
| Url | Png | ZXing.Net | 1823.73 | 2018.79 | 1535.41–2086.58 | 1377395 |

## Generation on ccd1

Source `419b84d950ea481a4775a7f7c717b9efa95bc520`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 79.56 | 100.64 | 71.50–106.33 | 3832 |
| Numeric | Modules | QRCoder | 110.47 | 128.13 | 79.65–138.98 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.59 | 72.61 | 28.70–74.04 | 1696 |
| Numeric | Modules | ZXing.Net | 55.23 | 79.20 | 38.08–86.59 | 31304 |
| Numeric | Png | CodeGlyphX | 109.48 | 124.93 | 77.81–127.64 | 40312 |
| Numeric | Png | QRCoder | 175.78 | 203.71 | 154.37–204.41 | 6568 |
| Numeric | Png | ZXing-C++ | 764.03 | 802.40 | 677.60–811.05 | 848321 |
| Numeric | Png | ZXing.Net | 784.00 | 901.31 | 715.28–917.21 | 877610 |
| Unicode | Modules | CodeGlyphX | 1598.84 | 1765.47 | 1554.04–1788.10 | 23872 |
| Unicode | Modules | QRCoder | 2626.57 | 3122.29 | 2458.69–3259.61 | 23216 |
| Unicode | Modules | ZXing-C++ | 1077.84 | 1270.98 | 1034.48–1467.42 | 14648 |
| Unicode | Modules | ZXing.Net | 1604.73 | 1995.99 | 1540.85–2026.58 | 606584 |
| Unicode | Png | CodeGlyphX | 1759.43 | 2020.65 | 1675.48–2061.18 | 274832 |
| Unicode | Png | QRCoder | 2980.29 | 3191.15 | 2879.26–3322.72 | 26112 |
| Unicode | Png | ZXing-C++ | 7187.53 | 7879.33 | 6740.17–7985.50 | 6206829 |
| Unicode | Png | ZXing.Net | 8550.58 | 9971.59 | 7393.84–10778.50 | 6798332 |
| Url | Modules | CodeGlyphX | 144.61 | 180.57 | 110.15–194.47 | 5056 |
| Url | Modules | QRCoder | 322.82 | 373.86 | 294.21–380.72 | 7376 |
| Url | Modules | ZXing-C++ | 94.64 | 131.93 | 64.72–132.44 | 2696 |
| Url | Modules | ZXing.Net | 208.49 | 249.49 | 190.48–252.53 | 71064 |
| Url | Png | CodeGlyphX | 177.24 | 196.63 | 140.56–198.64 | 60272 |
| Url | Png | QRCoder | 409.11 | 519.19 | 393.96–600.69 | 8206 |
| Url | Png | ZXing-C++ | 1293.40 | 1403.98 | 1233.31–1417.26 | 1309025 |
| Url | Png | ZXing.Net | 1392.41 | 1521.33 | 1316.48–1538.94 | 1377477 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3365.69 | 3616.18 | 3314.62–3775.33 | 14–15 | 0–0 |
| ccd0 | ZXing-C++ | 30.01 | 50.93 | 25.22–59.06 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 199.95 | 260.08 | 189.87–275.16 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3260.94 | 3566.29 | 3185.91–3685.49 | 14–15 | 0–0 |
| ccd1 | ZXing-C++ | 24.51 | 31.88 | 19.73–36.67 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 165.10 | 306.43 | 158.21–421.29 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
