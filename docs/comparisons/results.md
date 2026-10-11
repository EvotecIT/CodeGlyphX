# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 188/267 | 6 | 3 | 0 | 1 |
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
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 26 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 188/267 correct positive images, compared with 187/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The 38-image selection was fixed before the first supplemental outcomes. One revealed regression informed the final correction and permanent fixture; the final set is supplemental regression evidence, not an unseen holdout.

Supplemental images from the same upstream collection, outside the 18 frozen comparison images. The initial check exposed a sampling-priority regression that was repaired and added to permanent tests. This is regression evidence used during remediation, not an unseen holdout for the final candidate, a replacement ranking, or proof on an independent dataset. Wall-clock budgets also limit attribution of changed outcomes.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 33/38 | 34/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: pdf417-3/05.png. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

Timing-sensitive changes: pdf417-3/05.png. These outcomes do not establish a decoder accuracy gain.

This follow-up records 188/267 correct positive images versus 187/267 in the previous pass. No previous positive successes are lost in this run. Paired negative passes complete 29-30/31 with the current binary and 31-31/31 with the previous binary. Every current paired pass completes fewer negative images than every previous paired pass in this run. This measured completion loss remains an open cost of the candidate and is not counted as a timeout improvement. The PDF417 photograph gain has exact-payload regression proof against the previous qualified binary. Changes in other formats or negative completion remain sensitive to the shared host and wall-clock budgets; no general timeout guarantee is established. Every pass and initial overall/per-domain CPU reading is retained. PDF417 generated shallow-skew compatibility cases are separate from the unchanged frozen corpus. Data-only rows require independently repeated regular framing; erasure correction retains its bounds. Arbitrary occlusion or perspective is not established. The long AI (3922) oracle in rssexpandedstacked-2/13.png exceeds the GS1 field limit; raw encoded digits are returned, but the invalid parenthesized oracle remains verbatim and remains failed under unchanged scoring. One Expanded Stacked image result per request is supported; arbitrary perspective and multiple-symbol localization are not established.

Host CPU samples before the main corpus pass: 9.3%, 16.8%, 11.5%. Before the supplementary negative pass: 10.2%, 9.9%, 10.3%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 24/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 31/31 | 0 | 0 | 44.8% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 56.8% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 32.3% |
| ccd0 | 4 | previous | 31/31 | 0 | 0 | 10.0% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 22.0% |
| ccd1 | 2 | previous | 31/31 | 0 | 0 | 13.0% |
| ccd1 | 3 | previous | 31/31 | 0 | 0 | 11.4% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 16.2% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 12.5% | 23.1% | 2.2% |
| decoding-ccd0 | 18.4% | 27.4% | 9.6% |
| decoding-ccd1 | 79.2% | 87.4% | 70.9% |
| generation-ccd0 | 38.2% | 55.6% | 20.8% |
| generation-ccd1 | 43.8% | 55.4% | 32.1% |
| holdout-current | 11.7% | 20.7% | 2.7% |
| holdout-previous | 14.9% | 25.8% | 4.0% |
| negative-rotation-ccd0-0-previous | 44.8% | 58.0% | 31.5% |
| negative-rotation-ccd0-1-current | 56.8% | 69.9% | 43.7% |
| negative-rotation-ccd0-2-current | 32.3% | 45.9% | 18.7% |
| negative-rotation-ccd0-3-previous | 10.0% | 15.9% | 4.5% |
| negative-rotation-ccd1-0-current | 22.0% | 33.5% | 10.5% |
| negative-rotation-ccd1-1-previous | 13.0% | 21.9% | 4.3% |
| negative-rotation-ccd1-2-previous | 11.4% | 20.6% | 2.6% |
| negative-rotation-ccd1-3-current | 16.2% | 28.6% | 4.0% |
| negatives-ccd0 | 10.1% | 19.1% | 1.7% |

## Generation on ccd0

Source `1c806479848c36e668e3478ea8b0419e4eae2d8c`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 88.72 | 100.73 | 61.85–102.03 | 3832 |
| Numeric | Modules | QRCoder | 109.84 | 139.71 | 76.90–156.52 | 5568 |
| Numeric | Modules | ZXing-C++ | 39.34 | 72.89 | 28.97–78.85 | 1696 |
| Numeric | Modules | ZXing.Net | 51.26 | 77.47 | 38.35–79.50 | 31304 |
| Numeric | Png | CodeGlyphX | 86.40 | 206.30 | 77.50–316.43 | 40316 |
| Numeric | Png | QRCoder | 171.79 | 247.59 | 147.76–250.38 | 6568 |
| Numeric | Png | ZXing-C++ | 782.28 | 845.11 | 723.82–886.62 | 848314 |
| Numeric | Png | ZXing.Net | 769.27 | 945.97 | 721.74–969.40 | 877580 |
| Unicode | Modules | CodeGlyphX | 1608.22 | 1778.58 | 1504.70–1796.18 | 23872 |
| Unicode | Modules | QRCoder | 2629.31 | 2853.71 | 2466.54–2919.64 | 23216 |
| Unicode | Modules | ZXing-C++ | 1085.70 | 1216.64 | 985.57–1217.35 | 14648 |
| Unicode | Modules | ZXing.Net | 1594.29 | 1791.78 | 1469.44–1841.66 | 606584 |
| Unicode | Png | CodeGlyphX | 1977.84 | 2125.58 | 1684.52–2136.61 | 274832 |
| Unicode | Png | QRCoder | 3167.53 | 3446.19 | 2756.10–3521.92 | 26112 |
| Unicode | Png | ZXing-C++ | 7483.55 | 8354.93 | 6426.24–8499.22 | 6206487 |
| Unicode | Png | ZXing.Net | 8746.32 | 10690.21 | 7345.76–10717.89 | 6797976 |
| Url | Modules | CodeGlyphX | 158.23 | 1102.06 | 100.83–1164.28 | 5057 |
| Url | Modules | QRCoder | 390.09 | 1197.33 | 287.19–1329.65 | 7388 |
| Url | Modules | ZXing-C++ | 107.59 | 182.00 | 61.48–186.49 | 2697 |
| Url | Modules | ZXing.Net | 253.50 | 1150.66 | 197.79–1230.47 | 71086 |
| Url | Png | CodeGlyphX | 172.95 | 268.57 | 135.96–274.50 | 60283 |
| Url | Png | QRCoder | 465.37 | 512.36 | 381.44–537.37 | 8200 |
| Url | Png | ZXing-C++ | 1357.77 | 1586.56 | 1175.21–1588.09 | 1308992 |
| Url | Png | ZXing.Net | 1384.73 | 1541.57 | 1281.07–1580.76 | 1377456 |

## Generation on ccd1

Source `1c806479848c36e668e3478ea8b0419e4eae2d8c`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 64.56 | 91.04 | 59.26–93.09 | 3832 |
| Numeric | Modules | QRCoder | 79.59 | 113.24 | 76.61–119.25 | 5568 |
| Numeric | Modules | ZXing-C++ | 29.90 | 58.55 | 27.73–63.06 | 1696 |
| Numeric | Modules | ZXing.Net | 46.90 | 70.83 | 36.37–73.35 | 31304 |
| Numeric | Png | CodeGlyphX | 79.33 | 106.11 | 76.30–107.34 | 40312 |
| Numeric | Png | QRCoder | 150.81 | 206.69 | 146.42–213.92 | 6568 |
| Numeric | Png | ZXing-C++ | 680.48 | 765.00 | 636.59–817.27 | 848293 |
| Numeric | Png | ZXing.Net | 702.24 | 818.19 | 680.97–856.15 | 877596 |
| Unicode | Modules | CodeGlyphX | 1720.36 | 2232.27 | 1558.05–2573.55 | 23872 |
| Unicode | Modules | QRCoder | 2667.49 | 3527.77 | 2560.70–3635.22 | 23216 |
| Unicode | Modules | ZXing-C++ | 1109.34 | 1617.68 | 1006.03–1796.48 | 14648 |
| Unicode | Modules | ZXing.Net | 1575.62 | 2111.00 | 1505.27–2565.04 | 606584 |
| Unicode | Png | CodeGlyphX | 1871.38 | 2439.26 | 1657.11–2927.07 | 274832 |
| Unicode | Png | QRCoder | 3216.22 | 4560.67 | 2776.91–5570.89 | 26112 |
| Unicode | Png | ZXing-C++ | 7041.39 | 8147.08 | 6448.92–8284.08 | 6206729 |
| Unicode | Png | ZXing.Net | 9226.21 | 12071.51 | 7409.54–13040.40 | 6798208 |
| Url | Modules | CodeGlyphX | 127.53 | 181.32 | 102.66–207.41 | 5056 |
| Url | Modules | QRCoder | 340.23 | 402.21 | 288.95–402.48 | 7376 |
| Url | Modules | ZXing-C++ | 116.61 | 146.00 | 85.86–156.35 | 2696 |
| Url | Modules | ZXing.Net | 206.40 | 279.09 | 167.19–292.48 | 71064 |
| Url | Png | CodeGlyphX | 154.04 | 189.78 | 131.99–198.73 | 60272 |
| Url | Png | QRCoder | 400.99 | 453.39 | 381.03–454.61 | 8200 |
| Url | Png | ZXing-C++ | 1188.30 | 1416.50 | 1159.57–1447.61 | 1309004 |
| Url | Png | ZXing.Net | 1354.38 | 1413.83 | 1184.14–1432.04 | 1377419 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 3201.47 | 3280.38 | 3161.99–3293.31 | 92.76 | 16–16 | 0–0 |
| ccd0 | ZXing-C++ | 20.50 | 26.31 | 18.23–26.47 | 2.55 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 162.88 | 178.18 | 152.97–186.88 | 13.34 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 3179.99 | 3223.81 | 3157.15–3230.07 | 93.11 | 16–16 | 0–0 |
| ccd1 | ZXing-C++ | 21.55 | 26.45 | 18.83–27.11 | 2.55 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 157.36 | 183.65 | 147.74–193.66 | 13.35 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
