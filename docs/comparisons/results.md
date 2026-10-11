# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 195/267 | 6 | 3 | 0 | 1 |
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
| Code93 | 3 | 2 | 3 | 3 | 3 |
| DataMatrix | 18 | 8 | 18 | 18 | 18 |
| Ean | 30 | 4 | 17 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 18 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 11 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 18 | 18 | 17 |
| QrCode | 37 | 22 | 32 | 34 | 34 |
| UpcA | 36 | 0 | 17 | 19 | 19 |
| UpcE | 15 | 3 | 13 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 195/267 correct positive images, compared with 191/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 195/267 correct positive images versus 191/267 in the previous pass. Previously correct images lost: none; gained: code93-1/2.png, qrcode-1/6.png, qrcode-5/11.png, qrcode-5/09.png. Paired negative completion is [30, 30, 31, 30]/31 with the current binary and [30, 30, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Code93 localization retains checksum and termination checks, while QR perspective fitting uses observed finder and alignment centers with the existing sampler and acceptance checks. Recognition budgets and search limits remain unchanged. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 14.0%, 14.5%, 10.6%. Before the supplementary negative pass: 21.8%, 29.4%, 39.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 25/25 | 0 | 0 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 25/25 supplementary negatives versus 25/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 18.9% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 16.9% |
| ccd0 | 3 | current | 30/31 | 1 | 0 | 38.6% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 25.7% |
| ccd1 | 1 | current | 31/31 | 0 | 0 | 24.3% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 36.0% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 26.3% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 30.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 13.0% | 24.0% | 2.0% |
| decoding-ccd0 | 19.9% | 35.7% | 4.3% |
| decoding-ccd1 | 23.9% | 39.1% | 8.8% |
| diagnostic-final-All-final | 22.5% | 36.6% | 8.4% |
| diagnostic-final-All-qualified | 36.3% | 49.7% | 23.0% |
| diagnostic-final-Code93-final | 22.4% | 40.2% | 4.5% |
| diagnostic-final-Code93-qualified | 55.3% | 71.8% | 39.0% |
| diagnostic-final-QrCode-final | 46.1% | 59.0% | 33.2% |
| diagnostic-final-QrCode-qualified | 30.1% | 47.0% | 13.3% |
| generation-ccd0 | 70.7% | 77.9% | 63.5% |
| generation-ccd1 | 35.0% | 52.6% | 17.4% |
| holdout-current | 22.5% | 42.5% | 2.7% |
| negative-rotation-ccd0-0-previous | 18.9% | 35.5% | 2.5% |
| negative-rotation-ccd0-1-current | 16.9% | 31.2% | 2.7% |
| negative-rotation-ccd0-2-current | 38.6% | 50.8% | 26.5% |
| negative-rotation-ccd0-3-previous | 25.7% | 42.2% | 9.3% |
| negative-rotation-ccd1-0-current | 24.3% | 40.1% | 8.7% |
| negative-rotation-ccd1-1-previous | 36.0% | 47.5% | 24.5% |
| negative-rotation-ccd1-2-previous | 26.3% | 44.2% | 8.4% |
| negative-rotation-ccd1-3-current | 30.7% | 45.8% | 15.7% |
| negatives-ccd0 | 30.3% | 42.8% | 17.8% |

## Generation on ccd0

Source `3fecb39f331d953c8c28792b26c0f3dc6c527496`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 97.60 | 131.11 | 61.87–150.29 | 3832 |
| Numeric | Modules | QRCoder | 123.15 | 168.33 | 99.30–172.52 | 5568 |
| Numeric | Modules | ZXing-C++ | 45.65 | 74.88 | 29.39–77.99 | 1696 |
| Numeric | Modules | ZXing.Net | 67.47 | 102.20 | 62.28–104.66 | 31304 |
| Numeric | Png | CodeGlyphX | 121.71 | 157.57 | 90.72–161.62 | 40312 |
| Numeric | Png | QRCoder | 213.21 | 240.25 | 167.90–261.02 | 6568 |
| Numeric | Png | ZXing-C++ | 946.51 | 1040.10 | 829.90–1045.42 | 848293 |
| Numeric | Png | ZXing.Net | 984.61 | 1077.37 | 901.16–1111.90 | 877594 |
| Unicode | Modules | CodeGlyphX | 2025.66 | 2156.84 | 1822.38–2194.62 | 23872 |
| Unicode | Modules | QRCoder | 3067.56 | 3256.57 | 2990.80–3312.11 | 23217 |
| Unicode | Modules | ZXing-C++ | 1230.23 | 1444.27 | 1152.37–1556.72 | 14648 |
| Unicode | Modules | ZXing.Net | 2035.36 | 2180.71 | 1865.04–2217.11 | 606589 |
| Unicode | Png | CodeGlyphX | 2174.66 | 2739.88 | 1933.18–3158.39 | 274832 |
| Unicode | Png | QRCoder | 3598.41 | 4955.94 | 3173.35–5514.78 | 26112 |
| Unicode | Png | ZXing-C++ | 8656.96 | 12848.20 | 7884.27–13116.21 | 6206513 |
| Unicode | Png | ZXing.Net | 9700.33 | 14282.73 | 8033.41–15684.92 | 6798029 |
| Url | Modules | CodeGlyphX | 287.64 | 1207.12 | 156.64–1279.99 | 5057 |
| Url | Modules | QRCoder | 1091.66 | 1305.58 | 349.36–1372.16 | 7395 |
| Url | Modules | ZXing-C++ | 96.83 | 196.31 | 85.48–208.80 | 2697 |
| Url | Modules | ZXing.Net | 278.04 | 1045.42 | 215.03–1057.50 | 71092 |
| Url | Png | CodeGlyphX | 213.57 | 299.97 | 162.55–316.24 | 60278 |
| Url | Png | QRCoder | 485.79 | 532.33 | 440.70–551.88 | 8200 |
| Url | Png | ZXing-C++ | 1577.88 | 1755.10 | 1383.83–1761.75 | 1308972 |
| Url | Png | ZXing.Net | 1675.03 | 1907.44 | 1627.59–2057.11 | 1377441 |

## Generation on ccd1

Source `3fecb39f331d953c8c28792b26c0f3dc6c527496`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 92.25 | 110.89 | 58.63–123.38 | 3832 |
| Numeric | Modules | QRCoder | 119.23 | 148.37 | 78.66–150.91 | 5568 |
| Numeric | Modules | ZXing-C++ | 43.99 | 85.04 | 28.03–88.35 | 1696 |
| Numeric | Modules | ZXing.Net | 47.97 | 83.42 | 37.21–98.92 | 31304 |
| Numeric | Png | CodeGlyphX | 104.72 | 146.97 | 86.10–158.88 | 40312 |
| Numeric | Png | QRCoder | 181.77 | 238.53 | 143.35–251.15 | 6568 |
| Numeric | Png | ZXing-C++ | 866.10 | 940.27 | 789.28–972.46 | 848296 |
| Numeric | Png | ZXing.Net | 834.53 | 959.49 | 745.11–971.43 | 877580 |
| Unicode | Modules | CodeGlyphX | 1573.46 | 1674.94 | 1518.11–1691.31 | 23872 |
| Unicode | Modules | QRCoder | 2457.67 | 2722.65 | 2411.90–2737.59 | 23216 |
| Unicode | Modules | ZXing-C++ | 1063.25 | 1195.48 | 1011.23–1206.07 | 14648 |
| Unicode | Modules | ZXing.Net | 1663.55 | 1832.06 | 1528.72–1861.28 | 606584 |
| Unicode | Png | CodeGlyphX | 1760.84 | 2157.98 | 1637.78–2223.69 | 274832 |
| Unicode | Png | QRCoder | 2947.98 | 3365.91 | 2745.23–3456.93 | 26123 |
| Unicode | Png | ZXing-C++ | 7026.11 | 8124.03 | 6434.33–8488.04 | 6206625 |
| Unicode | Png | ZXing.Net | 7858.85 | 10421.70 | 7196.86–10496.11 | 6798282 |
| Url | Modules | CodeGlyphX | 130.12 | 177.67 | 100.46–209.53 | 5056 |
| Url | Modules | QRCoder | 300.52 | 394.91 | 279.38–429.22 | 7376 |
| Url | Modules | ZXing-C++ | 72.11 | 106.49 | 57.67–116.01 | 2696 |
| Url | Modules | ZXing.Net | 197.73 | 240.03 | 175.89–250.47 | 71064 |
| Url | Png | CodeGlyphX | 163.84 | 197.10 | 125.80–207.22 | 60272 |
| Url | Png | QRCoder | 392.51 | 461.21 | 368.90–488.17 | 8200 |
| Url | Png | ZXing-C++ | 1265.64 | 1347.76 | 1150.26–1363.48 | 1309069 |
| Url | Png | ZXing.Net | 1364.88 | 1560.09 | 1256.91–1628.37 | 1377490 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1622.66 | 1682.58 | 1591.11–1688.01 | 83.90 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 27.61 | 36.74 | 22.29–43.65 | 2.15 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 195.38 | 308.33 | 183.11–313.34 | 12.96 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1559.24 | 1860.82 | 1501.46–1906.46 | 84.09 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 23.77 | 31.17 | 20.01–32.38 | 2.15 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 181.82 | 260.56 | 158.00–262.22 | 12.95 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
