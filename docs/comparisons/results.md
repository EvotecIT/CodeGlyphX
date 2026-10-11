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

This decoding follow-up records 195/267 correct positive images, compared with 195/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

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

This follow-up records 195/267 correct positive images versus 195/267 in the previous pass. Previously correct images lost: none; gained: none. Paired negative completion is [30, 29, 30, 28]/31 with the current binary and [30, 30, 30, 29]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. QR scoring reuses exact BCH distances and avoids timing work only when the primary format score cannot win. All candidate hypotheses and tie rules are preserved. Recognition budgets and search limits remain unchanged. Demonstrated regression gains do not establish accuracy on unseen photographs. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. The upstream RSS-14 group is recorded under Gs1DataBarOmnidirectional and includes stacked layouts: rss14-1/2.png and rss14-1/5.png are confirmed examples. CodeGlyphX has stacked DataBar module decoders but no stacked DataBar image scanner; those misses remain counted in the broader family coverage.

Host CPU samples before the main corpus pass: 52.6%, 27.1%, 31.2%. Before the supplementary negative pass: 80.4%, 35.6%, 35.1%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 30/31 | 1 | 0 | 23.9% |
| ccd0 | 2 | current | 30/31 | 1 | 0 | 18.8% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 33.0% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 39.8% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 37.2% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 33.7% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 44.1% |
| ccd1 | 4 | current | 28/31 | 3 | 0 | 91.5% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| baseline-family-cost | 16.4% | 29.9% | 2.9% |
| baseline-family-no-tiles-cost | 20.2% | 34.7% | 5.8% |
| baseline-latency-cost | 88.1% | 94.4% | 81.5% |
| corpus-ccd0 | 37.0% | 53.0% | 21.0% |
| decoding-ccd0 | 15.8% | 28.7% | 3.2% |
| decoding-ccd1 | 13.5% | 25.1% | 2.3% |
| diagnostic-final-All-32-final | 23.2% | 35.1% | 11.4% |
| diagnostic-final-All-32-qualified | 31.8% | 49.7% | 14.0% |
| diagnostic-final-QrCode-1-final | 6.9% | 12.9% | 1.3% |
| diagnostic-final-QrCode-1-qualified | 7.7% | 14.0% | 1.8% |
| diagnostic-final-QrCode-32-final | 25.6% | 39.2% | 12.0% |
| diagnostic-final-QrCode-32-qualified | 21.3% | 32.4% | 10.2% |
| generation-ccd0 | 46.0% | 64.2% | 27.8% |
| generation-ccd1 | 37.8% | 54.5% | 21.1% |
| holdout-current | 10.5% | 18.9% | 2.7% |
| negative-rotation-ccd0-0-previous | 23.9% | 42.1% | 5.7% |
| negative-rotation-ccd0-1-current | 18.8% | 34.8% | 3.1% |
| negative-rotation-ccd0-2-current | 33.0% | 50.4% | 15.5% |
| negative-rotation-ccd0-3-previous | 39.8% | 52.8% | 26.9% |
| negative-rotation-ccd1-0-current | 37.2% | 50.0% | 24.4% |
| negative-rotation-ccd1-1-previous | 33.7% | 50.2% | 17.3% |
| negative-rotation-ccd1-2-previous | 44.1% | 58.5% | 29.9% |
| negative-rotation-ccd1-3-current | 91.5% | 94.2% | 88.8% |
| negatives-ccd0 | 50.4% | 65.9% | 34.9% |
| paired-cost-0-baseline | 54.4% | 66.1% | 42.7% |
| paired-cost-1-candidate | 37.6% | 54.4% | 20.8% |
| paired-cost-2-candidate | 20.7% | 35.5% | 5.8% |
| paired-cost-3-baseline | 53.5% | 64.8% | 42.3% |

## Generation on ccd0

Source `8a123f69bcc2333cc0d9e0ec41a05af400e75401`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 107.19 | 124.58 | 65.26–125.92 | 3832 |
| Numeric | Modules | QRCoder | 124.31 | 157.95 | 90.47–158.28 | 5568 |
| Numeric | Modules | ZXing-C++ | 57.50 | 88.89 | 37.15–92.72 | 1696 |
| Numeric | Modules | ZXing.Net | 71.10 | 110.36 | 37.05–118.09 | 31304 |
| Numeric | Png | CodeGlyphX | 132.15 | 171.35 | 89.71–193.56 | 40340 |
| Numeric | Png | QRCoder | 219.78 | 265.85 | 195.91–283.13 | 6737 |
| Numeric | Png | ZXing-C++ | 962.12 | 1128.45 | 865.20–1143.04 | 848467 |
| Numeric | Png | ZXing.Net | 999.15 | 1132.64 | 869.69–1135.97 | 877716 |
| Unicode | Modules | CodeGlyphX | 1985.65 | 2527.63 | 1874.24–2731.33 | 23872 |
| Unicode | Modules | QRCoder | 3033.45 | 3212.02 | 2910.63–3231.93 | 23220 |
| Unicode | Modules | ZXing-C++ | 1260.64 | 1529.17 | 1185.81–1770.87 | 14648 |
| Unicode | Modules | ZXing.Net | 1911.00 | 2321.41 | 1743.77–2502.26 | 606585 |
| Unicode | Png | CodeGlyphX | 2162.97 | 2279.98 | 1928.28–2282.83 | 274832 |
| Unicode | Png | QRCoder | 3441.89 | 3663.10 | 3304.27–3737.29 | 26112 |
| Unicode | Png | ZXing-C++ | 8216.40 | 8810.76 | 7521.72–8952.84 | 6206420 |
| Unicode | Png | ZXing.Net | 9335.16 | 10347.06 | 8384.96–10618.00 | 6797994 |
| Url | Modules | CodeGlyphX | 196.24 | 1100.38 | 140.79–1164.50 | 5057 |
| Url | Modules | QRCoder | 701.82 | 1110.76 | 288.11–1143.62 | 7395 |
| Url | Modules | ZXing-C++ | 114.64 | 250.39 | 71.17–281.12 | 2697 |
| Url | Modules | ZXing.Net | 264.07 | 974.35 | 190.20–989.47 | 71108 |
| Url | Png | CodeGlyphX | 257.47 | 366.51 | 207.40–379.65 | 60305 |
| Url | Png | QRCoder | 605.85 | 788.20 | 479.24–788.96 | 8381 |
| Url | Png | ZXing-C++ | 1840.16 | 2069.68 | 1490.08–2212.76 | 1309160 |
| Url | Png | ZXing.Net | 2023.71 | 2331.71 | 1616.30–2516.93 | 1377592 |

## Generation on ccd1

Source `8a123f69bcc2333cc0d9e0ec41a05af400e75401`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 66.65 | 112.91 | 61.74–119.42 | 3832 |
| Numeric | Modules | QRCoder | 81.29 | 133.82 | 73.14–139.27 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.59 | 64.12 | 28.78–64.37 | 1696 |
| Numeric | Modules | ZXing.Net | 51.92 | 92.61 | 37.49–95.71 | 31304 |
| Numeric | Png | CodeGlyphX | 98.04 | 123.12 | 77.95–125.72 | 40312 |
| Numeric | Png | QRCoder | 172.56 | 196.91 | 144.75–197.00 | 6568 |
| Numeric | Png | ZXing-C++ | 777.06 | 863.16 | 715.72–881.43 | 848281 |
| Numeric | Png | ZXing.Net | 798.07 | 869.71 | 710.31–891.30 | 877572 |
| Unicode | Modules | CodeGlyphX | 1692.61 | 1847.86 | 1602.37–1851.46 | 23872 |
| Unicode | Modules | QRCoder | 2663.89 | 2782.64 | 2421.47–2822.31 | 23216 |
| Unicode | Modules | ZXing-C++ | 1094.09 | 1172.06 | 1043.83–1185.32 | 14648 |
| Unicode | Modules | ZXing.Net | 1643.90 | 1792.14 | 1529.16–1854.54 | 606584 |
| Unicode | Png | CodeGlyphX | 1764.25 | 1964.81 | 1658.81–2137.05 | 274832 |
| Unicode | Png | QRCoder | 2950.11 | 3184.08 | 2742.64–3203.20 | 26112 |
| Unicode | Png | ZXing-C++ | 7000.25 | 7315.10 | 6524.40–7349.37 | 6206699 |
| Unicode | Png | ZXing.Net | 7668.44 | 8884.67 | 7328.82–9817.46 | 6798386 |
| Url | Modules | CodeGlyphX | 138.54 | 270.40 | 102.31–287.96 | 5056 |
| Url | Modules | QRCoder | 334.13 | 416.78 | 278.87–446.34 | 7376 |
| Url | Modules | ZXing-C++ | 101.09 | 157.94 | 69.22–182.09 | 2696 |
| Url | Modules | ZXing.Net | 215.66 | 373.79 | 178.29–411.68 | 71065 |
| Url | Png | CodeGlyphX | 161.90 | 224.92 | 136.61–238.73 | 60272 |
| Url | Png | QRCoder | 417.66 | 428.25 | 390.51–434.23 | 8200 |
| Url | Png | ZXing-C++ | 1288.39 | 1391.72 | 1238.10–1410.11 | 1309036 |
| Url | Png | ZXing.Net | 1389.77 | 1514.09 | 1305.86–1529.35 | 1377468 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1537.13 | 1588.95 | 1502.34–1595.11 | 84.61 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 21.75 | 24.77 | 20.45–24.97 | 2.16 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 165.50 | 197.60 | 156.43–205.48 | 12.93 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1495.25 | 1529.64 | 1479.33–1532.94 | 84.79 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 20.17 | 22.75 | 18.24–24.52 | 2.15 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 153.27 | 155.11 | 148.98–155.24 | 12.96 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
