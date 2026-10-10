# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 187/267 | 6 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
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
| Pdf417 | 18 | 2 | 17 | 18 | 17 |
| QrCode | 37 | 22 | 26 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 187/267 correct positive images, compared with 184/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

This follow-up records 187/267 correct positive images versus 184/267 in the previous pass. No previous positive successes are lost in this run. Paired negative passes complete 29-31/31 with the current binary and 30-31/31 with the previous binary. The supplementary negative pass completes 24/25 versus 25/25 previously; falsepositives-2/01.png is now inconclusive. The two PDF417 gains have damaged-image regression proof. The additional qrcode-3/11.png success previously exceeded its deadline; QR decoding is unchanged, so that result is not attributed to the PDF417 repair. Every pass and initial overall/per-domain CPU reading is retained. Shared-host load and wall-clock deadlines limit attribution; no general timeout guarantee is established. PDF417 generated damaged-row regressions are separate from the unchanged frozen corpus. Erasure correction requires qualified row assembly and enough readable framing; arbitrary occlusion or perspective is not established. The long AI (3922) oracle in rssexpandedstacked-2/13.png exceeds the GS1 field limit; raw encoded digits are returned, but the invalid parenthesized oracle remains verbatim and remains failed under unchanged scoring. One Expanded Stacked image result per request is supported; arbitrary perspective and multiple-symbol localization are not established.

Host CPU samples before the main corpus pass: 14.8%, 11.9%, 7.0%. Before the supplementary negative pass: 66.0%, 31.2%, 62.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 25/25. Previously completed images now unfinished: falsepositives-2/01.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 50.6% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 27.1% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 36.1% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 27.3% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 14.9% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 24.4% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 27.9% |
| ccd1 | 4 | current | 31/31 | 0 | 0 | 37.6% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 11.2% | 20.4% | 2.0% |
| decoding-ccd0 | 35.3% | 55.5% | 15.2% |
| decoding-ccd1 | 39.6% | 57.8% | 21.5% |
| generation-ccd0 | 19.5% | 30.1% | 8.9% |
| generation-ccd1 | 34.0% | 46.2% | 21.7% |
| negative-rotation-ccd0-0-previous | 50.6% | 73.3% | 27.9% |
| negative-rotation-ccd0-1-current | 27.1% | 42.1% | 12.2% |
| negative-rotation-ccd0-2-current | 36.1% | 55.9% | 16.4% |
| negative-rotation-ccd0-3-previous | 27.3% | 40.7% | 14.0% |
| negative-rotation-ccd1-0-current | 14.9% | 27.8% | 2.4% |
| negative-rotation-ccd1-1-previous | 24.4% | 42.9% | 6.1% |
| negative-rotation-ccd1-2-previous | 27.9% | 41.8% | 14.1% |
| negative-rotation-ccd1-3-current | 37.6% | 54.5% | 20.8% |
| negatives-ccd0 | 53.3% | 63.6% | 43.0% |

## Generation on ccd0

Source `b48ce9f9d44ae45922859c78de480db7095b6db0`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 82.20 | 105.70 | 59.23–115.63 | 3832 |
| Numeric | Modules | QRCoder | 105.91 | 158.78 | 72.51–164.48 | 5568 |
| Numeric | Modules | ZXing-C++ | 51.47 | 71.62 | 27.88–81.10 | 1696 |
| Numeric | Modules | ZXing.Net | 59.78 | 90.04 | 37.78–105.13 | 31304 |
| Numeric | Png | CodeGlyphX | 108.33 | 114.16 | 82.71–117.69 | 40312 |
| Numeric | Png | QRCoder | 171.10 | 215.15 | 150.81–230.53 | 6568 |
| Numeric | Png | ZXing-C++ | 787.43 | 960.08 | 713.42–979.80 | 848320 |
| Numeric | Png | ZXing.Net | 814.73 | 984.94 | 735.08–1070.47 | 877593 |
| Unicode | Modules | CodeGlyphX | 2442.80 | 3664.51 | 1923.84–3735.69 | 23872 |
| Unicode | Modules | QRCoder | 3846.33 | 5426.37 | 2944.20–5486.35 | 23223 |
| Unicode | Modules | ZXing-C++ | 1622.38 | 2258.62 | 1210.21–2562.71 | 14648 |
| Unicode | Modules | ZXing.Net | 2234.35 | 3435.08 | 1878.26–3780.50 | 606592 |
| Unicode | Png | CodeGlyphX | 2186.91 | 3090.26 | 2092.79–3206.21 | 274841 |
| Unicode | Png | QRCoder | 3709.17 | 4338.98 | 3554.13–4472.01 | 26112 |
| Unicode | Png | ZXing-C++ | 9559.16 | 12804.11 | 8553.60–12873.74 | 6206343 |
| Unicode | Png | ZXing.Net | 10737.04 | 14545.41 | 9369.65–16872.20 | 6798046 |
| Url | Modules | CodeGlyphX | 494.66 | 914.25 | 106.97–944.99 | 5057 |
| Url | Modules | QRCoder | 835.75 | 1138.06 | 322.63–1217.09 | 7396 |
| Url | Modules | ZXing-C++ | 111.89 | 208.54 | 58.94–214.28 | 2703 |
| Url | Modules | ZXing.Net | 237.88 | 987.41 | 172.64–1112.17 | 71108 |
| Url | Png | CodeGlyphX | 170.69 | 267.48 | 137.74–321.60 | 60283 |
| Url | Png | QRCoder | 409.91 | 466.26 | 373.91–467.23 | 8200 |
| Url | Png | ZXing-C++ | 1325.63 | 1477.12 | 1179.15–1494.16 | 1308998 |
| Url | Png | ZXing.Net | 1426.12 | 1550.40 | 1307.92–1595.41 | 1377476 |

## Generation on ccd1

Source `b48ce9f9d44ae45922859c78de480db7095b6db0`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 76.94 | 150.87 | 59.67–183.10 | 3832 |
| Numeric | Modules | QRCoder | 117.62 | 171.56 | 72.38–188.46 | 5568 |
| Numeric | Modules | ZXing-C++ | 39.37 | 72.38 | 27.98–75.75 | 1696 |
| Numeric | Modules | ZXing.Net | 55.26 | 86.44 | 36.39–100.37 | 31304 |
| Numeric | Png | CodeGlyphX | 118.26 | 178.97 | 82.15–234.67 | 40322 |
| Numeric | Png | QRCoder | 182.90 | 221.72 | 143.08–223.34 | 6568 |
| Numeric | Png | ZXing-C++ | 880.90 | 942.19 | 750.10–951.69 | 848294 |
| Numeric | Png | ZXing.Net | 865.07 | 1032.88 | 819.31–1047.83 | 877579 |
| Unicode | Modules | CodeGlyphX | 1735.75 | 2052.00 | 1506.39–2317.27 | 23872 |
| Unicode | Modules | QRCoder | 2636.21 | 3140.35 | 2495.60–3256.69 | 23216 |
| Unicode | Modules | ZXing-C++ | 1123.48 | 1250.36 | 1003.38–1282.22 | 14648 |
| Unicode | Modules | ZXing.Net | 1653.25 | 2192.01 | 1588.46–2256.73 | 606585 |
| Unicode | Png | CodeGlyphX | 1849.10 | 2412.77 | 1683.79–2701.48 | 274832 |
| Unicode | Png | QRCoder | 3116.15 | 3724.18 | 2914.90–3781.67 | 26112 |
| Unicode | Png | ZXing-C++ | 7075.48 | 7655.87 | 6814.70–7728.35 | 6206850 |
| Unicode | Png | ZXing.Net | 8835.60 | 10401.81 | 7544.21–10698.24 | 6798391 |
| Url | Modules | CodeGlyphX | 103.52 | 228.32 | 98.19–245.34 | 5056 |
| Url | Modules | QRCoder | 286.09 | 373.16 | 272.64–411.68 | 7376 |
| Url | Modules | ZXing-C++ | 78.43 | 99.66 | 57.71–102.13 | 2696 |
| Url | Modules | ZXing.Net | 185.03 | 246.44 | 163.85–263.74 | 71064 |
| Url | Png | CodeGlyphX | 154.96 | 263.31 | 130.56–310.78 | 60272 |
| Url | Png | QRCoder | 408.05 | 460.85 | 382.24–466.67 | 8200 |
| Url | Png | ZXing-C++ | 1290.00 | 1430.06 | 1179.41–1436.60 | 1309019 |
| Url | Png | ZXing.Net | 1409.60 | 1703.44 | 1268.35–1775.98 | 1377472 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3339.82 | 3390.41 | 3212.70–3422.15 | 91.24 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 26.68 | 31.80 | 20.70–33.69 | 2.14 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 185.45 | 208.05 | 165.35–211.42 | 12.94 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3247.88 | 3306.23 | 3198.78–3334.99 | 91.83 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 21.17 | 27.03 | 18.41–27.77 | 2.12 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 167.67 | 191.34 | 154.65–194.06 | 12.93 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
