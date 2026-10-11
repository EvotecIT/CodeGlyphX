# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and 6 images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 219/267 | 6 | 3 | 0 | 2 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 10 | 10 | 12 |
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
| UpcA | 36 | 0 | 26 | 19 | 19 |
| UpcE | 15 | 3 | 15 | 11 | 10 |

Previously correct images now missed: none in this run.

This decoding follow-up records 219/267 correct positive images, compared with 219/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: none in this run.

Previously completed main-corpus negatives now inconclusive: falsepositives/19.png

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

## Retail recovery and search cost

Supplementary contract diagnostics, not a representative accuracy or negative-rate survey. Thirty-one fixed negative IDs are each requested with three explicit retail-family hints (93 requests).

The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.

| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 64/87 | 64/87 | 0 |
| ZXing.Net | 55/87 | 55/87 | 2 |
| ZXing-C++ | 54/87 | 54/87 | 1 |

Each of 31 fixed negative images is scanned with EAN, UPC-A and UPC-E hints: 93 requests, rather than 93 independent images.

| Reader | Previous correct rejection | Current correct rejection | Current unexpected output | Current inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 93/93 | 93/93 | 0 | 0 |
| ZXing.Net | 89/93 | 89/93 | 4 | 0 |
| ZXing-C++ | 93/93 | 93/93 | 0 | 0 |

Nine prespecified requests: noise 03, stripes 14 and texture 19, each with EAN, UPC-A and UPC-E hints. PowerForge owns timing: 3 warmups, 12 grouped rotated measurements, no outlier removal, fixed Normal priority/affinity. Baseline and candidate use separate assembly contexts with identical preloaded RGBA32 and options. Loading and validation are excluded. Costs and correctness are measured together; all samples and losses are retained. Shared-host contention prevents portable latency claims.

| Domain | Previous median ms / nine requests | Current median ms / nine requests | Latency ratio | Previous mean managed MiB | Current mean managed MiB | Allocation ratio | Previous/current correct requests |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | 236.11 | 238.43 | 1.01x | 21.36 | 21.37 | 1.00x | 9/9 → 9/9 |
| ccd1 | 259.48 | 226.97 | 0.87x | 21.37 | 21.37 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [28, 22, 30, 30]/31 currently and [28, 29, 30, 30]/31 previously. The seeded QR fine search changes no recognition hypothesis. Paired nineteen-image costs and nine-request retail costs remain separate, with every sample and recognition failure retained. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by prior remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 50.3%, 62.2%, 52.7%. Before the supplementary negative pass: 67.4%, 75.6%, 69.0%. These are separate passes; overlapping images can have different completion outcomes.

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
| ccd0 | 1 | previous | 28/31 | 3 | 0 | 63.7% |
| ccd0 | 2 | current | 28/31 | 3 | 0 | 45.8% |
| ccd0 | 3 | current | 22/31 | 9 | 0 | 44.3% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 93.0% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 27.7% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 36.2% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 52.5% |
| ccd1 | 4 | current | 30/31 | 1 | 0 | 42.7% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 55.0% | 67.1% | 43.0% |
| decoding-ccd0 | 93.7% | 96.0% | 91.3% |
| decoding-ccd1 | 34.4% | 50.3% | 18.5% |
| diagnostic-final-retail-negative | 52.8% | 63.7% | 41.9% |
| diagnostic-final-retail-positive | 44.5% | 59.0% | 30.0% |
| generation-ccd0 | 97.2% | 98.1% | 96.3% |
| generation-ccd1 | 78.8% | 85.8% | 71.6% |
| holdout-current | 40.1% | 54.6% | 25.6% |
| negative-rotation-ccd0-0-previous | 63.7% | 77.0% | 50.4% |
| negative-rotation-ccd0-1-current | 45.8% | 68.5% | 22.9% |
| negative-rotation-ccd0-2-current | 44.3% | 60.8% | 27.8% |
| negative-rotation-ccd0-3-previous | 93.0% | 95.3% | 90.6% |
| negative-rotation-ccd1-0-current | 27.7% | 46.8% | 8.8% |
| negative-rotation-ccd1-1-previous | 36.2% | 54.7% | 17.8% |
| negative-rotation-ccd1-2-previous | 52.5% | 63.9% | 41.1% |
| negative-rotation-ccd1-3-current | 42.7% | 60.3% | 25.3% |
| negatives-ccd0 | 70.7% | 81.8% | 59.5% |

## Generation on ccd0

Source `4c716a38fa02833177964b57b823b24ab343d207`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 121.97 | 239.25 | 97.81–264.85 | 3832 |
| Numeric | Modules | QRCoder | 182.65 | 387.56 | 106.81–548.61 | 5568 |
| Numeric | Modules | ZXing-C++ | 80.93 | 121.81 | 47.10–124.62 | 1696 |
| Numeric | Modules | ZXing.Net | 110.08 | 327.02 | 63.42–499.71 | 31304 |
| Numeric | Png | CodeGlyphX | 149.58 | 833.01 | 85.84–1490.58 | 40312 |
| Numeric | Png | QRCoder | 313.49 | 617.43 | 188.07–680.77 | 6568 |
| Numeric | Png | ZXing-C++ | 1266.72 | 3246.17 | 856.44–5095.29 | 848284 |
| Numeric | Png | ZXing.Net | 1523.71 | 3419.15 | 956.39–4445.74 | 877595 |
| Unicode | Modules | CodeGlyphX | 2401.50 | 3052.30 | 1952.21–3288.42 | 23872 |
| Unicode | Modules | QRCoder | 3831.00 | 5139.50 | 3388.54–5160.48 | 23216 |
| Unicode | Modules | ZXing-C++ | 1494.34 | 2168.77 | 1363.84–2663.75 | 14648 |
| Unicode | Modules | ZXing.Net | 2582.44 | 3819.04 | 1918.47–4568.97 | 606585 |
| Unicode | Png | CodeGlyphX | 2715.31 | 4672.80 | 2137.34–4713.54 | 274942 |
| Unicode | Png | QRCoder | 4732.57 | 7034.94 | 3584.23–7712.57 | 26805 |
| Unicode | Png | ZXing-C++ | 11365.79 | 18053.45 | 9565.94–19798.85 | 6207067 |
| Unicode | Png | ZXing.Net | 14116.22 | 18965.38 | 10783.12–22625.70 | 6798552 |
| Url | Modules | CodeGlyphX | 995.31 | 1153.48 | 205.32–1237.12 | 5057 |
| Url | Modules | QRCoder | 1147.56 | 1282.08 | 409.38–1327.98 | 7403 |
| Url | Modules | ZXing-C++ | 169.66 | 272.34 | 94.47–298.20 | 2705 |
| Url | Modules | ZXing.Net | 920.70 | 1040.25 | 267.93–1114.72 | 71119 |
| Url | Png | CodeGlyphX | 395.50 | 644.62 | 233.36–663.80 | 60278 |
| Url | Png | QRCoder | 765.43 | 1075.29 | 498.05–1161.12 | 8200 |
| Url | Png | ZXing-C++ | 2590.56 | 3122.99 | 1752.46–3164.34 | 1309007 |
| Url | Png | ZXing.Net | 2469.96 | 4009.54 | 1679.97–4822.39 | 1377410 |

## Generation on ccd1

Source `4c716a38fa02833177964b57b823b24ab343d207`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 122.27 | 174.71 | 97.54–199.93 | 3832 |
| Numeric | Modules | QRCoder | 145.56 | 242.04 | 95.81–245.35 | 5568 |
| Numeric | Modules | ZXing-C++ | 51.90 | 157.15 | 35.81–173.68 | 1696 |
| Numeric | Modules | ZXing.Net | 77.42 | 176.32 | 59.85–230.21 | 31304 |
| Numeric | Png | CodeGlyphX | 155.61 | 325.29 | 78.59–451.85 | 40312 |
| Numeric | Png | QRCoder | 274.70 | 484.06 | 149.84–520.18 | 6582 |
| Numeric | Png | ZXing-C++ | 1216.99 | 1707.14 | 666.11–1720.51 | 848372 |
| Numeric | Png | ZXing.Net | 1245.65 | 1643.83 | 960.05–1839.37 | 877608 |
| Unicode | Modules | CodeGlyphX | 2080.30 | 2825.61 | 1511.40–2917.34 | 23872 |
| Unicode | Modules | QRCoder | 3399.39 | 3956.61 | 2527.76–4061.73 | 23216 |
| Unicode | Modules | ZXing-C++ | 1332.29 | 1882.45 | 1026.53–2079.82 | 14648 |
| Unicode | Modules | ZXing.Net | 2071.76 | 2562.39 | 1504.41–2828.54 | 606585 |
| Unicode | Png | CodeGlyphX | 2070.48 | 3103.27 | 1631.17–3820.14 | 274832 |
| Unicode | Png | QRCoder | 3351.37 | 4731.81 | 2825.46–5088.37 | 26168 |
| Unicode | Png | ZXing-C++ | 8334.22 | 9874.28 | 6469.89–10530.88 | 6206804 |
| Unicode | Png | ZXing.Net | 10147.20 | 12840.17 | 7592.56–13203.18 | 6798298 |
| Url | Modules | CodeGlyphX | 891.63 | 1147.96 | 174.03–1218.80 | 5063 |
| Url | Modules | QRCoder | 1045.89 | 1265.93 | 861.97–1356.06 | 7413 |
| Url | Modules | ZXing-C++ | 146.56 | 221.48 | 72.17–267.21 | 2709 |
| Url | Modules | ZXing.Net | 788.39 | 962.34 | 192.29–1062.31 | 71141 |
| Url | Png | CodeGlyphX | 246.87 | 327.47 | 181.04–343.51 | 60291 |
| Url | Png | QRCoder | 563.99 | 671.50 | 492.07–696.56 | 8302 |
| Url | Png | ZXing-C++ | 1722.17 | 1931.61 | 1410.99–1931.85 | 1309111 |
| Url | Png | ZXing.Net | 1648.78 | 1833.16 | 1446.06–1861.01 | 1377546 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1519.11 | 1609.64 | 1403.17–1621.17 | 86.67 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 22.26 | 33.50 | 21.25–34.53 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 170.57 | 200.82 | 151.03–202.95 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1461.47 | 1562.08 | 1432.70–1571.41 | 86.97 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 21.36 | 27.48 | 20.41–31.64 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 163.37 | 211.77 | 148.36–226.42 | 13.26 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Unsupported MaxiCode, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2118.97 | 2378.47 | 1930.37–2520.40 | 86.99 | 18–18 | 0–0 | 0–0 | 0.989 |
| ccd0 | Candidate | 2096.28 | 2340.74 | 1972.73–2370.17 | 86.98 | 18–18 | 0–0 | 0–0 | 0.989 |
| ccd1 | Baseline | 2107.17 | 2197.68 | 1932.77–2200.96 | 87.14 | 18–18 | 0–0 | 0–0 | 0.985 |
| ccd1 | Candidate | 2074.73 | 2587.19 | 1932.53–2894.07 | 86.77 | 17–18 | 0–0 | 0–1 | 0.985 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
