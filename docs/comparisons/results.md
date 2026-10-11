# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 208/267 | 6 | 3 | 0 | 4 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 8 | 10 | 12 |
| Aztec | 12 | 4 | 12 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 13 | 13 | 13 |
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

This decoding follow-up records 208/267 correct positive images, compared with 207/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png, falsepositives/03.png, falsepositives-2/15.png

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

This follow-up records 208/267 correct positive images versus 207/267 in the previous pass. Previously correct images lost: none; gained: code128-1/7.png. Paired negative completion is [28, 29, 30, 30]/31 with the current binary and [28, 29, 30, 29]/31 with the previous qualified binary. All outcomes remain sensitive to shared-host contention and wall-clock budgets; no universal timeout or recognition advantage is established. The newly recovered Code 128 image contains FNC3 reader initialization; its exact contents are exposed with explicit configuration metadata. No recognition allowance, family hint, oracle or scoring changes. Additional default-format checks are contract diagnostics, not a representative accuracy survey or performance comparison. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. The invalid GS1 AI (3922) oracle and undeclared QR character encoding policies remain unchanged in scoring. Stacked DataBar and Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported.

Host CPU samples before the main corpus pass: 95.4%, 99.3%, 99.2%. Before the supplementary negative pass: 97.8%, 93.4%, 92.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 21/25 | 0 | 4 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 21/25 supplementary negatives versus 25/25. Previously completed images now unfinished: falsepositives-2/01.png, falsepositives-2/02.png, falsepositives-2/06.png, falsepositives-2/09.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 89.7% |
| ccd0 | 2 | current | 28/31 | 3 | 0 | 90.2% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 99.0% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 61.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 48.5% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 76.6% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 33.3% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 47.3% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 98.0% | 99.0% | 96.9% |
| decoding-ccd0 | 83.4% | 90.2% | 76.6% |
| decoding-ccd1 | 54.5% | 64.5% | 44.5% |
| generation-ccd0 | 68.9% | 77.9% | 59.7% |
| generation-ccd1 | 52.2% | 63.3% | 41.0% |
| holdout-current | 85.0% | 91.5% | 78.6% |
| negative-rotation-ccd0-0-previous | 89.7% | 94.3% | 85.0% |
| negative-rotation-ccd0-1-current | 90.2% | 94.4% | 86.0% |
| negative-rotation-ccd0-2-current | 99.0% | 99.7% | 98.4% |
| negative-rotation-ccd0-3-previous | 61.4% | 70.8% | 52.2% |
| negative-rotation-ccd1-0-current | 48.5% | 62.8% | 34.3% |
| negative-rotation-ccd1-1-previous | 76.6% | 83.3% | 70.0% |
| negative-rotation-ccd1-2-previous | 33.3% | 48.4% | 18.3% |
| negative-rotation-ccd1-3-current | 47.3% | 61.1% | 33.4% |
| negatives-ccd0 | 94.6% | 97.4% | 91.8% |

## Generation on ccd0

Source `a2b44ae5ea361edea1b79139c17210f2c79cdcc1`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 83.89 | 143.20 | 60.00–163.87 | 3832 |
| Numeric | Modules | QRCoder | 111.51 | 244.44 | 76.97–296.85 | 5568 |
| Numeric | Modules | ZXing-C++ | 36.79 | 83.65 | 28.37–90.66 | 1696 |
| Numeric | Modules | ZXing.Net | 48.61 | 96.13 | 36.69–99.31 | 31304 |
| Numeric | Png | CodeGlyphX | 182.83 | 330.53 | 92.84–338.71 | 40312 |
| Numeric | Png | QRCoder | 305.10 | 521.12 | 189.52–551.07 | 6568 |
| Numeric | Png | ZXing-C++ | 1420.77 | 2257.89 | 1069.51–2342.09 | 848326 |
| Numeric | Png | ZXing.Net | 1347.93 | 1894.75 | 867.84–1906.07 | 877583 |
| Unicode | Modules | CodeGlyphX | 2322.49 | 2751.01 | 2105.75–2855.14 | 23872 |
| Unicode | Modules | QRCoder | 3460.29 | 4454.18 | 2781.62–4732.59 | 23219 |
| Unicode | Modules | ZXing-C++ | 1539.32 | 2124.03 | 1263.30–2418.65 | 14648 |
| Unicode | Modules | ZXing.Net | 2272.09 | 2941.29 | 1991.56–3041.20 | 606584 |
| Unicode | Png | CodeGlyphX | 1850.43 | 3075.69 | 1664.30–3549.50 | 274887 |
| Unicode | Png | QRCoder | 3603.00 | 4592.31 | 3022.71–4688.44 | 26501 |
| Unicode | Png | ZXing-C++ | 7581.45 | 12419.49 | 6506.01–14829.32 | 6207118 |
| Unicode | Png | ZXing.Net | 10179.04 | 16921.12 | 8397.80–20978.18 | 6798471 |
| Url | Modules | CodeGlyphX | 773.99 | 1082.26 | 278.44–1096.52 | 5063 |
| Url | Modules | QRCoder | 970.21 | 1865.10 | 769.87–2396.18 | 7409 |
| Url | Modules | ZXing-C++ | 143.52 | 182.11 | 119.58–194.25 | 2711 |
| Url | Modules | ZXing.Net | 723.76 | 1133.13 | 590.53–1178.67 | 71146 |
| Url | Png | CodeGlyphX | 135.62 | 249.39 | 124.91–259.47 | 60278 |
| Url | Png | QRCoder | 401.15 | 454.23 | 383.24–470.38 | 8200 |
| Url | Png | ZXing-C++ | 1272.80 | 1557.93 | 1177.35–1685.54 | 1309034 |
| Url | Png | ZXing.Net | 1445.39 | 1599.51 | 1295.51–1663.50 | 1377487 |

## Generation on ccd1

Source `a2b44ae5ea361edea1b79139c17210f2c79cdcc1`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 58.80 | 84.74 | 58.11–85.41 | 3832 |
| Numeric | Modules | QRCoder | 70.46 | 96.81 | 69.37–100.22 | 5568 |
| Numeric | Modules | ZXing-C++ | 28.47 | 51.28 | 27.63–51.84 | 1696 |
| Numeric | Modules | ZXing.Net | 36.54 | 70.20 | 35.43–80.93 | 31304 |
| Numeric | Png | CodeGlyphX | 86.80 | 125.82 | 74.75–138.16 | 40312 |
| Numeric | Png | QRCoder | 137.88 | 142.22 | 135.97–144.39 | 6568 |
| Numeric | Png | ZXing-C++ | 632.81 | 826.72 | 596.71–928.92 | 848329 |
| Numeric | Png | ZXing.Net | 651.51 | 1010.76 | 615.93–1257.74 | 877577 |
| Unicode | Modules | CodeGlyphX | 1698.57 | 2128.16 | 1470.25–2171.56 | 23872 |
| Unicode | Modules | QRCoder | 2515.59 | 3389.74 | 2303.29–3507.61 | 23216 |
| Unicode | Modules | ZXing-C++ | 1100.91 | 1265.11 | 954.84–1276.60 | 14648 |
| Unicode | Modules | ZXing.Net | 1640.73 | 1905.17 | 1409.59–2002.15 | 606584 |
| Unicode | Png | CodeGlyphX | 2382.42 | 3419.39 | 1657.69–3811.97 | 274832 |
| Unicode | Png | QRCoder | 4080.47 | 5165.30 | 2772.16–5300.86 | 26112 |
| Unicode | Png | ZXing-C++ | 11025.15 | 13681.62 | 6498.33–14330.52 | 6206668 |
| Unicode | Png | ZXing.Net | 12875.58 | 14741.71 | 7021.03–14917.82 | 6798045 |
| Url | Modules | CodeGlyphX | 604.13 | 751.32 | 587.21–756.48 | 5057 |
| Url | Modules | QRCoder | 769.50 | 843.46 | 727.07–900.81 | 7410 |
| Url | Modules | ZXing-C++ | 98.26 | 210.02 | 92.91–226.52 | 2707 |
| Url | Modules | ZXing.Net | 613.82 | 876.85 | 577.89–1043.54 | 71152 |
| Url | Png | CodeGlyphX | 176.59 | 280.71 | 126.73–305.83 | 60278 |
| Url | Png | QRCoder | 422.27 | 678.90 | 367.93–834.16 | 8258 |
| Url | Png | ZXing-C++ | 1299.33 | 1707.98 | 1085.71–1750.37 | 1309106 |
| Url | Png | ZXing.Net | 1505.10 | 2228.15 | 1215.68–2307.21 | 1377624 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1567.63 | 1726.86 | 1462.00–1744.27 | 89.63 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 25.82 | 30.21 | 19.24–31.09 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 196.30 | 252.93 | 154.76–256.84 | 12.99 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1886.38 | 2129.00 | 1453.44–2133.87 | 88.13 | 17–18 | 0–0 |
| ccd1 | ZXing-C++ | 34.79 | 62.67 | 25.68–71.59 | 2.20 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 268.33 | 331.67 | 160.03–354.76 | 13.01 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
