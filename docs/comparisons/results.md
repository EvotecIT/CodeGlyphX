# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 184/267 | 6 | 3 | 0 | 1 |
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
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 15 | 18 | 17 |
| QrCode | 37 | 22 | 25 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 184/267 correct positive images, compared with 173/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

Expanded Stacked image scanning records 11/12 correct images that were previously unsupported. The complete positive score is 184/267 versus 173/267 in the previous pass. The long AI (3922) oracle in rssexpandedstacked-2/13.png exceeds the GS1 field limit; raw encoded digits are returned, but the invalid parenthesized oracle remains verbatim and the result remains failed under unchanged scoring. No previous positive successes are lost in this run. Current paired negative passes complete 28-30/31, versus 30-31/31 for the previous binary. All passes and load readings remain visible; the added stage and changed deadline shares require further completion investigation. The comparison retains all outcomes and host load. One image result per Expanded Stacked request is supported; arbitrary perspective and multiple-symbol localization are not established. Main and supplementary negative completion and paired passes remain separate; a general timeout fix is not established.

Host CPU samples before the main corpus pass: 39.6%, 36.2%, 39.3%. Before the supplementary negative pass: 99.6%, 99.4%, 97.4%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 23/25 | 0 | 2 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 71.9% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 20.9% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 37.4% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 39.2% |
| ccd1 | 1 | current | 28/31 | 3 | 0 | 51.3% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 26.1% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 67.2% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 42.8% |

## Generation on ccd0

Source `c6deddd5187924cd03cdc2c7592b52b6ab3cc355`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 87.99 | 141.00 | 76.00–158.48 | 3832 |
| Numeric | Modules | QRCoder | 130.72 | 181.08 | 85.63–186.46 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.63 | 106.94 | 29.87–142.82 | 1696 |
| Numeric | Modules | ZXing.Net | 78.15 | 159.00 | 41.58–203.96 | 31304 |
| Numeric | Png | CodeGlyphX | 142.47 | 191.95 | 110.40–192.65 | 40312 |
| Numeric | Png | QRCoder | 250.39 | 443.99 | 180.23–480.32 | 6568 |
| Numeric | Png | ZXing-C++ | 1006.54 | 1894.65 | 858.60–2160.85 | 848281 |
| Numeric | Png | ZXing.Net | 992.65 | 1450.65 | 861.41–1521.89 | 877566 |
| Unicode | Modules | CodeGlyphX | 1905.02 | 2375.98 | 1662.07–2559.61 | 23872 |
| Unicode | Modules | QRCoder | 3006.49 | 4177.87 | 2728.71–4230.16 | 23216 |
| Unicode | Modules | ZXing-C++ | 1164.18 | 1550.02 | 1093.30–1683.52 | 14648 |
| Unicode | Modules | ZXing.Net | 1920.63 | 2971.67 | 1667.50–4077.09 | 606584 |
| Unicode | Png | CodeGlyphX | 1949.94 | 2203.74 | 1840.37–2213.44 | 274832 |
| Unicode | Png | QRCoder | 3246.97 | 3599.54 | 3104.52–3648.48 | 26112 |
| Unicode | Png | ZXing-C++ | 8228.99 | 8516.05 | 7698.36–8584.36 | 6206560 |
| Unicode | Png | ZXing.Net | 9417.34 | 11140.31 | 8743.54–12468.12 | 6797930 |
| Url | Modules | CodeGlyphX | 335.63 | 1060.14 | 135.25–1123.65 | 5057 |
| Url | Modules | QRCoder | 893.23 | 1248.03 | 324.77–1463.15 | 7395 |
| Url | Modules | ZXing-C++ | 116.78 | 208.62 | 72.40–239.44 | 2697 |
| Url | Modules | ZXing.Net | 462.56 | 970.26 | 211.19–984.70 | 71114 |
| Url | Png | CodeGlyphX | 201.63 | 278.05 | 143.44–314.92 | 60278 |
| Url | Png | QRCoder | 493.02 | 562.92 | 435.54–584.81 | 8200 |
| Url | Png | ZXing-C++ | 1468.13 | 1614.91 | 1339.63–1688.27 | 1308984 |
| Url | Png | ZXing.Net | 1654.38 | 1806.19 | 1465.87–1853.09 | 1377427 |

## Generation on ccd1

Source `c6deddd5187924cd03cdc2c7592b52b6ab3cc355`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 66.02 | 95.81 | 61.83–99.69 | 3832 |
| Numeric | Modules | QRCoder | 91.85 | 126.61 | 79.62–126.69 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.59 | 61.85 | 31.50–62.26 | 1696 |
| Numeric | Modules | ZXing.Net | 57.58 | 91.99 | 39.19–102.29 | 31304 |
| Numeric | Png | CodeGlyphX | 103.68 | 128.78 | 78.64–133.31 | 40312 |
| Numeric | Png | QRCoder | 165.16 | 193.00 | 151.39–200.24 | 6568 |
| Numeric | Png | ZXing-C++ | 725.42 | 817.21 | 690.89–856.20 | 848310 |
| Numeric | Png | ZXing.Net | 771.24 | 919.21 | 695.16–963.23 | 877575 |
| Unicode | Modules | CodeGlyphX | 1644.84 | 1799.51 | 1543.17–1946.34 | 23872 |
| Unicode | Modules | QRCoder | 2587.69 | 2915.93 | 2426.47–3034.96 | 23216 |
| Unicode | Modules | ZXing-C++ | 1073.21 | 1193.17 | 1001.96–1218.64 | 14648 |
| Unicode | Modules | ZXing.Net | 1593.62 | 1744.33 | 1505.22–1828.81 | 606584 |
| Unicode | Png | CodeGlyphX | 1756.20 | 1983.47 | 1651.38–2017.53 | 274887 |
| Unicode | Png | QRCoder | 2995.87 | 3151.51 | 2775.58–3157.52 | 26570 |
| Unicode | Png | ZXing-C++ | 7519.86 | 11707.17 | 6234.13–13644.54 | 6207345 |
| Unicode | Png | ZXing.Net | 9561.12 | 11788.75 | 8389.19–14274.34 | 6798831 |
| Url | Modules | CodeGlyphX | 135.26 | 159.21 | 105.33–160.59 | 5056 |
| Url | Modules | QRCoder | 330.63 | 346.98 | 296.40–350.72 | 7376 |
| Url | Modules | ZXing-C++ | 87.38 | 110.71 | 62.73–113.75 | 2696 |
| Url | Modules | ZXing.Net | 185.61 | 252.21 | 170.40–289.71 | 71065 |
| Url | Png | CodeGlyphX | 207.33 | 228.86 | 156.22–232.59 | 60272 |
| Url | Png | QRCoder | 436.43 | 501.95 | 409.38–550.77 | 8200 |
| Url | Png | ZXing-C++ | 1300.51 | 1397.60 | 1262.27–1447.07 | 1309009 |
| Url | Png | ZXing.Net | 1469.94 | 1668.65 | 1298.28–1733.61 | 1377453 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3336.97 | 3376.45 | 3213.74–3377.38 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 25.15 | 29.99 | 19.16–30.69 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 177.31 | 208.30 | 149.54–220.16 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3252.33 | 3374.07 | 3221.53–3385.70 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 22.20 | 27.45 | 18.59–29.33 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 166.18 | 177.97 | 160.02–180.84 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
