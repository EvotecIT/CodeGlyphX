# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 207/267 | 6 | 3 | 0 | 1 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 11 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 12 | 13 | 13 |
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

This decoding follow-up records 207/267 correct positive images, compared with 205/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 207/267 correct positive images versus 205/267 in the previous pass. Previously correct images lost: none; gained: code128-1/1.png, code128-1/5.png. Paired negative completion is [24, 29, 30, 30]/31 with the current binary and [30, 29, 30, 30]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. Micro QR validation rejects measured false acceptance without changing total or family recognition allowances. Additional default-format Aztec checks are contract diagnostics, not a representative accuracy survey or performance comparison. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. The Code 128 comparison hint includes GS1-128, matching the physical family in both ZXing readers. Two exact-image gains over the previous pass follow from this hint correction; input hashes, oracles and payload scoring remain unchanged.

Host CPU samples before the main corpus pass: 15.1%, 18.4%, 16.3%. Before the supplementary negative pass: 32.4%, 23.4%, 27.0%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 14.3% |
| ccd0 | 2 | current | 24/31 | 7 | 0 | 74.5% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 44.4% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 44.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 19.1% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 22.3% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 25.1% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 20.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 16.6% | 30.3% | 3.0% |
| decoding-ccd0 | 39.7% | 53.8% | 26.0% |
| decoding-ccd1 | 23.9% | 37.5% | 10.5% |
| generation-ccd0 | 35.7% | 49.1% | 22.4% |
| generation-ccd1 | 29.7% | 43.3% | 16.1% |
| holdout-current | 15.2% | 27.3% | 3.3% |
| negative-rotation-ccd0-0-previous | 14.3% | 24.9% | 4.2% |
| negative-rotation-ccd0-1-current | 74.5% | 82.8% | 66.2% |
| negative-rotation-ccd0-2-current | 44.4% | 57.7% | 31.0% |
| negative-rotation-ccd0-3-previous | 44.4% | 56.4% | 32.4% |
| negative-rotation-ccd1-0-current | 19.1% | 31.5% | 6.9% |
| negative-rotation-ccd1-1-previous | 22.3% | 35.7% | 9.0% |
| negative-rotation-ccd1-2-previous | 25.1% | 40.0% | 10.3% |
| negative-rotation-ccd1-3-current | 20.5% | 34.6% | 6.4% |
| negatives-ccd0 | 27.6% | 42.7% | 12.6% |

## Generation on ccd0

Source `8d2a49c1539e35b2ab419011281cd63d81efc16c`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 59.14 | 92.89 | 58.03–101.74 | 3832 |
| Numeric | Modules | QRCoder | 69.95 | 137.89 | 69.21–177.94 | 5568 |
| Numeric | Modules | ZXing-C++ | 28.70 | 75.97 | 27.99–101.66 | 1696 |
| Numeric | Modules | ZXing.Net | 39.37 | 63.67 | 37.00–63.86 | 31304 |
| Numeric | Png | CodeGlyphX | 107.78 | 148.37 | 76.68–161.03 | 40312 |
| Numeric | Png | QRCoder | 158.36 | 244.00 | 138.97–244.99 | 6568 |
| Numeric | Png | ZXing-C++ | 728.52 | 1179.68 | 647.16–1463.57 | 848314 |
| Numeric | Png | ZXing.Net | 784.80 | 1120.57 | 704.44–1181.93 | 877592 |
| Unicode | Modules | CodeGlyphX | 1510.58 | 1846.44 | 1452.06–2082.55 | 23872 |
| Unicode | Modules | QRCoder | 2481.72 | 3019.93 | 2318.22–3469.71 | 23216 |
| Unicode | Modules | ZXing-C++ | 1016.57 | 1266.38 | 989.78–1294.43 | 14648 |
| Unicode | Modules | ZXing.Net | 1567.68 | 1842.30 | 1461.55–2035.69 | 606584 |
| Unicode | Png | CodeGlyphX | 1703.84 | 2187.92 | 1591.16–2481.32 | 274832 |
| Unicode | Png | QRCoder | 2816.80 | 3064.31 | 2651.26–3168.30 | 26112 |
| Unicode | Png | ZXing-C++ | 6337.52 | 9797.86 | 6074.16–10952.52 | 6206843 |
| Unicode | Png | ZXing.Net | 8192.67 | 12566.21 | 7517.41–16504.68 | 6798096 |
| Url | Modules | CodeGlyphX | 633.67 | 749.34 | 586.75–772.13 | 5057 |
| Url | Modules | QRCoder | 777.81 | 936.41 | 706.61–942.76 | 7410 |
| Url | Modules | ZXing-C++ | 117.74 | 169.20 | 93.21–177.45 | 2707 |
| Url | Modules | ZXing.Net | 605.58 | 724.63 | 566.88–781.77 | 71146 |
| Url | Png | CodeGlyphX | 181.50 | 244.31 | 124.52–254.62 | 60294 |
| Url | Png | QRCoder | 406.50 | 473.73 | 367.77–507.21 | 8200 |
| Url | Png | ZXing-C++ | 1272.65 | 1421.51 | 1161.69–1496.47 | 1309047 |
| Url | Png | ZXing.Net | 1339.29 | 1527.48 | 1257.09–1560.84 | 1377502 |

## Generation on ccd1

Source `8d2a49c1539e35b2ab419011281cd63d81efc16c`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 59.94 | 82.29 | 57.83–84.22 | 3832 |
| Numeric | Modules | QRCoder | 79.31 | 114.65 | 74.52–126.23 | 5568 |
| Numeric | Modules | ZXing-C++ | 29.33 | 57.15 | 27.70–62.00 | 1696 |
| Numeric | Modules | ZXing.Net | 38.35 | 65.06 | 36.03–69.36 | 31304 |
| Numeric | Png | CodeGlyphX | 74.49 | 104.44 | 74.01–107.45 | 40312 |
| Numeric | Png | QRCoder | 142.82 | 155.70 | 140.02–160.98 | 6568 |
| Numeric | Png | ZXing-C++ | 625.53 | 644.41 | 594.21–645.50 | 848306 |
| Numeric | Png | ZXing.Net | 650.50 | 961.20 | 578.34–1176.06 | 877603 |
| Unicode | Modules | CodeGlyphX | 1491.73 | 1696.48 | 1450.74–1833.14 | 23872 |
| Unicode | Modules | QRCoder | 2418.06 | 2470.66 | 2370.36–2481.85 | 23216 |
| Unicode | Modules | ZXing-C++ | 1007.99 | 1057.86 | 975.84–1095.03 | 14648 |
| Unicode | Modules | ZXing.Net | 1461.43 | 1525.44 | 1417.95–1542.98 | 606584 |
| Unicode | Png | CodeGlyphX | 1648.62 | 1728.74 | 1585.06–1756.46 | 274832 |
| Unicode | Png | QRCoder | 2773.36 | 2874.90 | 2695.36–2894.88 | 26112 |
| Unicode | Png | ZXing-C++ | 6020.81 | 6533.59 | 5913.88–6562.26 | 6206952 |
| Unicode | Png | ZXing.Net | 7272.27 | 8421.32 | 6472.92–8491.82 | 6798288 |
| Url | Modules | CodeGlyphX | 604.52 | 1220.25 | 580.72–1254.60 | 5057 |
| Url | Modules | QRCoder | 775.00 | 1310.12 | 731.15–1418.21 | 7409 |
| Url | Modules | ZXing-C++ | 123.93 | 199.83 | 91.88–210.71 | 2707 |
| Url | Modules | ZXing.Net | 590.67 | 947.82 | 557.61–1050.65 | 71153 |
| Url | Png | CodeGlyphX | 158.31 | 303.04 | 123.98–322.79 | 60288 |
| Url | Png | QRCoder | 388.62 | 485.04 | 369.44–517.05 | 8200 |
| Url | Png | ZXing-C++ | 1148.90 | 1921.63 | 1084.89–2104.42 | 1309038 |
| Url | Png | ZXing.Net | 1240.85 | 2186.50 | 1159.02–2885.97 | 1377461 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1527.76 | 1699.60 | 1363.02–1829.85 | 90.12 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 24.01 | 29.33 | 18.68–30.90 | 2.21 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 168.48 | 227.76 | 148.13–247.12 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1379.76 | 1461.27 | 1359.14–1482.39 | 90.68 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 20.08 | 23.89 | 18.03–25.56 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 150.81 | 165.91 | 147.79–174.94 | 12.99 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
