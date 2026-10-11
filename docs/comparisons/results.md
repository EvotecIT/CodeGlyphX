# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 205/267 | 6 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 10 | 13 | 13 |
| Code39 | 12 | 1 | 9 | 12 | 12 |
| Code93 | 3 | 2 | 3 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 7 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 32 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 205/267 correct positive images, compared with 198/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The 38-image selection was fixed before the first supplemental outcomes. One revealed regression informed the final correction and permanent fixture; the final set is supplemental regression evidence, not an unseen holdout.

Supplemental images from the same upstream collection, outside the 18 frozen comparison images. The initial check exposed a sampling-priority regression that was repaired and added to permanent tests. This is regression evidence used during remediation, not an unseen holdout for the final candidate, a replacement ranking, or proof on an independent dataset. Wall-clock budgets also limit attribution of changed outcomes.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 34/38 | 34/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: none. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

This follow-up records 205/267 correct positive images versus 198/267 in the previous pass. Previously correct images lost: none; gained: aztec-1/7.png, aztec-2/19.png, aztec-2/15.png, aztec-2/13.png, aztec-2/14.png, aztec-2/17.png, aztec-2/10.png. Paired negative completion is [27, 29, 30, 30]/31 with the current binary and [28, 29, 30, 29]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Aztec sampling and perspective recognition recover measured failures without changing total or family recognition allowances. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 62.7%, 74.0%, 67.9%. Before the supplementary negative pass: 23.5%, 39.2%, 33.3%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 25/25. Previously completed images now unfinished: falsepositives-2/09.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 45.8% |
| ccd0 | 2 | current | 27/31 | 4 | 0 | 90.1% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 48.5% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 81.9% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 96.8% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 49.0% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 90.0% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 68.4% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 68.2% | 76.7% | 59.7% |
| decoding-ccd0 | 54.0% | 72.6% | 35.4% |
| decoding-ccd1 | 77.0% | 87.9% | 66.0% |
| generation-ccd0 | 91.6% | 94.3% | 88.9% |
| generation-ccd1 | 83.3% | 89.4% | 77.2% |
| holdout-current | 96.8% | 97.7% | 96.1% |
| negative-rotation-ccd0-0-previous | 45.8% | 64.0% | 27.6% |
| negative-rotation-ccd0-1-current | 90.1% | 93.6% | 86.7% |
| negative-rotation-ccd0-2-current | 48.5% | 72.3% | 24.7% |
| negative-rotation-ccd0-3-previous | 81.9% | 88.7% | 75.3% |
| negative-rotation-ccd1-0-current | 96.8% | 98.2% | 95.4% |
| negative-rotation-ccd1-1-previous | 49.0% | 64.7% | 33.3% |
| negative-rotation-ccd1-2-previous | 90.0% | 93.3% | 86.7% |
| negative-rotation-ccd1-3-current | 68.4% | 77.8% | 58.9% |
| negatives-ccd0 | 32.0% | 49.9% | 14.2% |

## Generation on ccd0

Source `3775926d78bc8fee5190620c413e339ba35632b4`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 119.91 | 175.62 | 97.81–203.49 | 3832 |
| Numeric | Modules | QRCoder | 175.71 | 346.86 | 132.46–358.91 | 5568 |
| Numeric | Modules | ZXing-C++ | 56.21 | 98.18 | 39.52–98.71 | 1696 |
| Numeric | Modules | ZXing.Net | 93.90 | 195.82 | 66.71–234.85 | 31304 |
| Numeric | Png | CodeGlyphX | 160.23 | 192.17 | 119.22–212.53 | 40312 |
| Numeric | Png | QRCoder | 295.72 | 367.49 | 222.06–394.66 | 6568 |
| Numeric | Png | ZXing-C++ | 1086.15 | 1321.71 | 1026.27–1431.63 | 848332 |
| Numeric | Png | ZXing.Net | 1128.04 | 1333.82 | 1053.56–1355.13 | 877600 |
| Unicode | Modules | CodeGlyphX | 2088.18 | 2606.67 | 1865.95–2766.34 | 23872 |
| Unicode | Modules | QRCoder | 3419.81 | 3672.94 | 3201.48–3881.22 | 23216 |
| Unicode | Modules | ZXing-C++ | 1396.73 | 1710.67 | 1288.18–1954.45 | 14648 |
| Unicode | Modules | ZXing.Net | 2163.60 | 2608.48 | 2033.27–2644.39 | 606585 |
| Unicode | Png | CodeGlyphX | 2194.37 | 2711.89 | 2049.19–2948.87 | 274832 |
| Unicode | Png | QRCoder | 3487.15 | 4548.26 | 3314.92–5490.14 | 26112 |
| Unicode | Png | ZXing-C++ | 8890.66 | 11018.15 | 8433.61–11372.79 | 6206803 |
| Unicode | Png | ZXing.Net | 10861.45 | 15286.95 | 10239.41–17282.48 | 6798200 |
| Url | Modules | CodeGlyphX | 1137.34 | 1310.51 | 187.86–1400.47 | 5057 |
| Url | Modules | QRCoder | 1203.91 | 1362.94 | 411.40–1372.37 | 7401 |
| Url | Modules | ZXing-C++ | 199.66 | 305.15 | 119.06–306.25 | 2707 |
| Url | Modules | ZXing.Net | 1016.36 | 1204.73 | 244.43–1312.68 | 71136 |
| Url | Png | CodeGlyphX | 275.12 | 334.55 | 164.42–342.05 | 60310 |
| Url | Png | QRCoder | 569.36 | 611.06 | 506.50–621.37 | 8294 |
| Url | Png | ZXing-C++ | 1683.96 | 1834.30 | 1597.15–1890.45 | 1309083 |
| Url | Png | ZXing.Net | 1819.76 | 1999.40 | 1593.48–2064.96 | 1377520 |

## Generation on ccd1

Source `3775926d78bc8fee5190620c413e339ba35632b4`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 77.39 | 97.59 | 67.92–103.71 | 3832 |
| Numeric | Modules | QRCoder | 100.62 | 128.29 | 93.54–129.19 | 5568 |
| Numeric | Modules | ZXing-C++ | 35.46 | 64.54 | 31.05–66.48 | 1696 |
| Numeric | Modules | ZXing.Net | 57.29 | 77.83 | 48.48–80.35 | 31304 |
| Numeric | Png | CodeGlyphX | 111.71 | 152.47 | 92.20–156.90 | 40312 |
| Numeric | Png | QRCoder | 212.08 | 411.26 | 177.11–431.69 | 6568 |
| Numeric | Png | ZXing-C++ | 925.94 | 1058.09 | 881.25–1087.10 | 848316 |
| Numeric | Png | ZXing.Net | 979.59 | 1102.44 | 854.12–1105.58 | 877604 |
| Unicode | Modules | CodeGlyphX | 2137.16 | 3286.70 | 1824.27–3361.39 | 23872 |
| Unicode | Modules | QRCoder | 3164.70 | 6126.22 | 2849.39–6546.01 | 23216 |
| Unicode | Modules | ZXing-C++ | 1309.97 | 2163.72 | 1193.18–2368.19 | 14648 |
| Unicode | Modules | ZXing.Net | 2148.70 | 3253.30 | 1867.94–3825.80 | 606584 |
| Unicode | Png | CodeGlyphX | 2285.52 | 3481.53 | 1895.21–3681.15 | 274832 |
| Unicode | Png | QRCoder | 3822.70 | 6480.54 | 3144.27–6567.44 | 26112 |
| Unicode | Png | ZXing-C++ | 8549.62 | 14776.01 | 7656.82–14952.57 | 6206783 |
| Unicode | Png | ZXing.Net | 10839.63 | 16651.67 | 8566.67–19240.80 | 6798372 |
| Url | Modules | CodeGlyphX | 798.85 | 943.35 | 149.07–966.80 | 5057 |
| Url | Modules | QRCoder | 978.56 | 1166.20 | 779.51–1191.67 | 7409 |
| Url | Modules | ZXing-C++ | 145.38 | 169.27 | 82.10–181.36 | 2711 |
| Url | Modules | ZXing.Net | 783.12 | 855.61 | 378.84–882.13 | 71146 |
| Url | Png | CodeGlyphX | 166.79 | 316.81 | 144.40–331.99 | 60283 |
| Url | Png | QRCoder | 453.41 | 579.38 | 419.09–620.07 | 8200 |
| Url | Png | ZXing-C++ | 1416.24 | 1556.52 | 1347.80–1604.93 | 1309088 |
| Url | Png | ZXing.Net | 1525.07 | 1617.48 | 1396.10–1658.91 | 1377489 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1748.98 | 1900.41 | 1688.28–1957.27 | 89.69 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 29.78 | 60.35 | 24.85–61.14 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 237.86 | 532.43 | 214.99–612.50 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1631.70 | 1908.62 | 1451.52–1989.99 | 94.29 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 25.69 | 33.27 | 21.39–37.05 | 2.23 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 181.90 | 297.56 | 170.32–302.94 | 13.21 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
