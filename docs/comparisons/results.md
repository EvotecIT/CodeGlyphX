# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 184/267 | 6 | 3 | 0 | 2 |
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
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 15 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 184/267 correct positive images, compared with 184/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png

This follow-up records 184/267 correct positive images versus 184/267 in the previous pass. No previous positive successes are lost in this run. Paired negative passes complete 25-31/31 with the current binary and 29-31/31 with the previous binary. Every pass and host CPU reading is retained. Shared-host load and wall-clock deadlines limit attribution; no general timeout guarantee is established. Compact PDF417 width and polarity regressions are covered by separate generated public-API tests; they do not expand or alter the frozen image corpus. The long AI (3922) oracle in rssexpandedstacked-2/13.png exceeds the GS1 field limit; raw encoded digits are returned, but the invalid parenthesized oracle remains verbatim and remains failed under unchanged scoring. One Expanded Stacked image result per request is supported; arbitrary perspective and multiple-symbol localization are not established.

Host CPU samples before the main corpus pass: 62.0%, 45.4%, 58.6%. Before the supplementary negative pass: 56.8%, 86.0%, 76.3%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 98.9% |
| ccd0 | 2 | current | 25/31 | 6 | 0 | 64.4% |
| ccd0 | 3 | current | 27/31 | 4 | 0 | 86.6% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 45.7% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 31.5% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 37.9% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 38.5% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 34.7% |

## Generation on ccd0

Source `138e0180f10cbc897eb5a4bebbdf1c28d131b2b3`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 105.41 | 135.30 | 84.03–142.14 | 3832 |
| Numeric | Modules | QRCoder | 131.92 | 165.58 | 97.05–169.43 | 5568 |
| Numeric | Modules | ZXing-C++ | 56.82 | 80.91 | 45.30–81.54 | 1696 |
| Numeric | Modules | ZXing.Net | 69.03 | 104.61 | 39.00–104.70 | 31304 |
| Numeric | Png | CodeGlyphX | 121.26 | 157.02 | 90.90–165.56 | 40312 |
| Numeric | Png | QRCoder | 214.78 | 240.45 | 160.27–246.60 | 6568 |
| Numeric | Png | ZXing-C++ | 900.24 | 1016.96 | 802.26–1102.75 | 848316 |
| Numeric | Png | ZXing.Net | 943.13 | 976.80 | 840.36–1000.67 | 877586 |
| Unicode | Modules | CodeGlyphX | 1911.42 | 1982.45 | 1748.03–1985.49 | 23872 |
| Unicode | Modules | QRCoder | 2944.62 | 3027.03 | 2733.88–3051.22 | 23216 |
| Unicode | Modules | ZXing-C++ | 1234.58 | 1318.08 | 1105.58–1393.94 | 14648 |
| Unicode | Modules | ZXing.Net | 1882.13 | 1979.73 | 1629.13–2002.82 | 606588 |
| Unicode | Png | CodeGlyphX | 2112.70 | 2504.33 | 1975.14–2546.20 | 274832 |
| Unicode | Png | QRCoder | 3393.20 | 4745.55 | 3169.43–5594.38 | 26112 |
| Unicode | Png | ZXing-C++ | 8381.79 | 13445.72 | 8010.27–17104.80 | 6206534 |
| Unicode | Png | ZXing.Net | 9976.57 | 10980.34 | 9171.06–11170.20 | 6797997 |
| Url | Modules | CodeGlyphX | 212.74 | 1053.21 | 113.23–1075.83 | 5057 |
| Url | Modules | QRCoder | 934.55 | 1190.17 | 340.51–1301.65 | 7398 |
| Url | Modules | ZXing-C++ | 122.95 | 197.76 | 87.22–212.46 | 2700 |
| Url | Modules | ZXing.Net | 505.45 | 1141.54 | 217.33–1195.89 | 71097 |
| Url | Png | CodeGlyphX | 220.79 | 262.97 | 168.57–280.63 | 60278 |
| Url | Png | QRCoder | 514.56 | 546.55 | 469.23–555.52 | 8200 |
| Url | Png | ZXing-C++ | 1548.34 | 1688.91 | 1321.92–1703.89 | 1308981 |
| Url | Png | ZXing.Net | 1614.34 | 1729.41 | 1442.62–1735.28 | 1377435 |

## Generation on ccd1

Source `138e0180f10cbc897eb5a4bebbdf1c28d131b2b3`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 64.91 | 115.05 | 59.79–137.45 | 3832 |
| Numeric | Modules | QRCoder | 110.66 | 141.03 | 83.47–146.85 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.14 | 72.12 | 28.05–75.52 | 1696 |
| Numeric | Modules | ZXing.Net | 51.02 | 66.85 | 37.21–67.43 | 31304 |
| Numeric | Png | CodeGlyphX | 100.64 | 132.74 | 76.77–155.30 | 40312 |
| Numeric | Png | QRCoder | 176.12 | 217.51 | 152.50–217.68 | 6568 |
| Numeric | Png | ZXing-C++ | 762.31 | 874.13 | 683.60–881.64 | 848319 |
| Numeric | Png | ZXing.Net | 796.42 | 915.05 | 693.37–956.53 | 877568 |
| Unicode | Modules | CodeGlyphX | 1658.25 | 1833.01 | 1549.21–1916.00 | 23872 |
| Unicode | Modules | QRCoder | 2618.30 | 2769.27 | 2512.35–2827.59 | 23216 |
| Unicode | Modules | ZXing-C++ | 1072.32 | 1196.53 | 1023.00–1197.01 | 14648 |
| Unicode | Modules | ZXing.Net | 1612.31 | 1756.40 | 1517.20–1760.62 | 606584 |
| Unicode | Png | CodeGlyphX | 1824.65 | 1916.74 | 1729.18–1930.37 | 274832 |
| Unicode | Png | QRCoder | 3001.03 | 3262.90 | 2902.06–3434.25 | 26112 |
| Unicode | Png | ZXing-C++ | 7467.42 | 7792.15 | 6956.71–7889.35 | 6206891 |
| Unicode | Png | ZXing.Net | 8926.59 | 9423.00 | 8126.88–9435.18 | 6798383 |
| Url | Modules | CodeGlyphX | 125.46 | 294.03 | 108.26–300.83 | 5056 |
| Url | Modules | QRCoder | 359.73 | 755.90 | 299.67–842.66 | 7376 |
| Url | Modules | ZXing-C++ | 100.77 | 155.38 | 57.36–179.77 | 2696 |
| Url | Modules | ZXing.Net | 202.21 | 433.59 | 173.58–555.55 | 71064 |
| Url | Png | CodeGlyphX | 182.83 | 278.18 | 140.67–279.29 | 60272 |
| Url | Png | QRCoder | 430.08 | 516.78 | 398.01–537.67 | 8200 |
| Url | Png | ZXing-C++ | 1317.04 | 1498.17 | 1228.59–1565.29 | 1308995 |
| Url | Png | ZXing.Net | 1509.59 | 1634.69 | 1355.94–1663.74 | 1377420 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3346.17 | 3465.97 | 3305.78–3475.24 | 91.66 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 27.26 | 61.17 | 19.18–71.60 | 2.42 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 197.91 | 272.95 | 167.87–346.17 | 13.21 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3218.32 | 3280.92 | 3178.07–3308.62 | 92.41 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 21.73 | 25.95 | 18.49–27.22 | 2.43 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 163.66 | 180.23 | 154.85–190.81 | 13.23 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
