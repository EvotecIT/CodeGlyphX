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

Previously completed main-corpus negatives now inconclusive: none in this run.

## Supplemental PDF417 images

All 38 PNG images with UTF-8 text oracles in pdf417-1, pdf417-2 and pdf417-3, excluding the 18 frozen comparison images. The selection was fixed before the first supplemental outcomes; its image bytes, oracles and format hints remain unchanged.

This unchanged 38-image set comes from the same upstream collection as the ranking. Five images have informed permanent regression fixtures across the remediation rounds; it is supplemental regression evidence, not an unseen holdout. All four framing-damage recoveries remain correct for both previous and current source in this cost round.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 38/38 | 38/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: none. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

## PDF417 search cost

Thirty-one fixed negative images, each with an explicit PDF417 hint. These diagnostics isolate PDF417 search and do not replace the unchanged all-format corpus. PowerForge owns timing, allocation and validation: three warmups and twelve grouped rotated measurements per binary on each verified affinity domain, with no outlier removal. Separate assembly contexts use identical preloaded RGBA32 and options; loading and outcome evaluation are excluded.

The supplemental 38-image PDF417 set remains 38/38 for both previous and current source and includes all four previously recovered framing cases. These inputs inform implementation and are regression evidence, not unseen accuracy evidence. Shared-host contention prevents portable performance claims. An unfinished search can change time and allocation and does not count as a successful rejection. Managed allocation excludes native memory.

| Domain | Previous ms | Current ms | Current / previous | Previous completed /31 | Current completed /31 | Previous unfinished batches /12 | Current unfinished batches /12 | Previous managed MiB | Current managed MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CCD0 | 2935.88 | 2793.85 | 0.952 | 31 | 31 | 0 | 0 | 242.66 | 242.65 |
| CCD1 | 2659.96 | 2359.58 | 0.887 | 31 | 30–31 | 0 | 1 | 242.65 | 242.63 |

Completed counts show the range across measured batches; neither binary returns an unexpected payload. An unfinished negative search remains inconclusive.

The actual old and new guard matchers return identical decisions across 401,400 deterministic cases, including 93,382 accepted fits. Existing focused PDF417 and budget checks pass; no public API or production dependency changes.

Raw [CCD0 samples](evidence/pdf417-cost-ccd0-result.json), [CCD1 samples](evidence/pdf417-cost-ccd1-result.json), [unchanged negative inputs](evidence/pdf417-cost-negative-manifest.json), [separate diagnostic outcomes](evidence/pdf417-cost-negative-outcomes.json) and [guard equivalence evidence](evidence/pdf417-cost-guard-equivalence.json) are retained.

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
| ccd0 | 278.32 | 279.05 | 1.00x | 21.35 | 21.35 | 1.00x | 9/9 → 9/9 |
| ccd1 | 260.95 | 257.94 | 0.99x | 21.36 | 21.36 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [19, 27, 30, 29]/31 currently and [29, 29, 30, 30]/31 previously. PDF417 guard matching and deadline lookup avoid work while preserving recognition hypotheses within the original allowance. Paired nineteen-image costs and nine-request retail costs remain separate, with every sample and recognition failure retained. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by this remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 98.0%, 99.4%, 100.0%. Before the supplementary negative pass: 32.5%, 41.7%, 51.1%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 24/25 | 0 | 1 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 24/25 supplementary negatives versus 23/25. Previously completed images now unfinished: none in this run.

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 64.8% |
| ccd0 | 2 | current | 19/31 | 12 | 0 | 23.3% |
| ccd0 | 3 | current | 27/31 | 4 | 0 | 70.7% |
| ccd0 | 4 | previous | 29/31 | 2 | 0 | 58.9% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 50.9% |
| ccd1 | 2 | previous | 30/31 | 1 | 0 | 58.3% |
| ccd1 | 3 | previous | 30/31 | 1 | 0 | 30.2% |
| ccd1 | 4 | current | 29/31 | 2 | 0 | 42.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 99.1% | 99.4% | 98.8% |
| decoding-ccd0 | 81.0% | 89.0% | 73.1% |
| decoding-ccd1 | 31.0% | 52.7% | 9.6% |
| diagnostic-final-retail-negative | 61.4% | 79.2% | 43.5% |
| diagnostic-final-retail-positive | 73.5% | 83.7% | 63.2% |
| generation-ccd0 | 51.3% | 63.1% | 39.4% |
| generation-ccd1 | 34.6% | 48.9% | 20.3% |
| holdout-current | 56.8% | 70.0% | 43.2% |
| negative-rotation-ccd0-0-previous | 64.8% | 71.4% | 58.1% |
| negative-rotation-ccd0-1-current | 23.3% | 39.2% | 7.7% |
| negative-rotation-ccd0-2-current | 70.7% | 76.7% | 64.8% |
| negative-rotation-ccd0-3-previous | 58.9% | 74.1% | 43.7% |
| negative-rotation-ccd1-0-current | 50.9% | 64.3% | 37.4% |
| negative-rotation-ccd1-1-previous | 58.3% | 69.5% | 47.2% |
| negative-rotation-ccd1-2-previous | 30.2% | 49.5% | 11.0% |
| negative-rotation-ccd1-3-current | 42.8% | 56.8% | 28.7% |
| negatives-ccd0 | 41.8% | 53.5% | 30.0% |

## Generation on ccd0

Source `d10503fa973942b31b1e79e293de71a34eba5858`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 94.86 | 127.67 | 69.90–129.60 | 3832 |
| Numeric | Modules | QRCoder | 116.77 | 159.60 | 86.53–180.46 | 5568 |
| Numeric | Modules | ZXing-C++ | 47.96 | 87.46 | 39.57–90.79 | 1696 |
| Numeric | Modules | ZXing.Net | 56.13 | 99.02 | 38.16–102.78 | 31304 |
| Numeric | Png | CodeGlyphX | 125.67 | 151.07 | 105.36–152.83 | 40353 |
| Numeric | Png | QRCoder | 223.34 | 256.82 | 175.04–267.69 | 6751 |
| Numeric | Png | ZXing-C++ | 923.98 | 988.70 | 810.54–994.79 | 848486 |
| Numeric | Png | ZXing.Net | 971.58 | 1002.79 | 845.11–1011.35 | 877713 |
| Unicode | Modules | CodeGlyphX | 2112.09 | 2273.83 | 1954.94–2308.85 | 23872 |
| Unicode | Modules | QRCoder | 3168.81 | 3604.75 | 2935.42–3650.61 | 23220 |
| Unicode | Modules | ZXing-C++ | 1319.59 | 1468.64 | 1211.68–1468.86 | 14648 |
| Unicode | Modules | ZXing.Net | 2035.92 | 2268.59 | 1931.27–2271.09 | 606585 |
| Unicode | Png | CodeGlyphX | 2141.67 | 2915.56 | 1934.88–2962.25 | 274887 |
| Unicode | Png | QRCoder | 3583.81 | 5020.06 | 3188.88–5537.63 | 26459 |
| Unicode | Png | ZXing-C++ | 8898.00 | 10144.05 | 8285.23–10415.17 | 6207185 |
| Unicode | Png | ZXing.Net | 10144.07 | 15897.38 | 8990.97–19026.99 | 6798567 |
| Url | Modules | CodeGlyphX | 928.91 | 1227.22 | 131.88–1371.78 | 5057 |
| Url | Modules | QRCoder | 1143.40 | 1542.18 | 387.54–1548.87 | 7418 |
| Url | Modules | ZXing-C++ | 156.10 | 308.37 | 82.52–319.91 | 2707 |
| Url | Modules | ZXing.Net | 934.53 | 1095.67 | 265.52–1148.20 | 71135 |
| Url | Png | CodeGlyphX | 224.98 | 368.71 | 177.96–454.39 | 60319 |
| Url | Png | QRCoder | 534.79 | 655.03 | 467.05–657.69 | 8375 |
| Url | Png | ZXing-C++ | 1704.62 | 1878.38 | 1434.99–1896.89 | 1309169 |
| Url | Png | ZXing.Net | 1825.63 | 2018.09 | 1539.62–2021.27 | 1377622 |

## Generation on ccd1

Source `d10503fa973942b31b1e79e293de71a34eba5858`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 84.36 | 109.89 | 60.28–110.00 | 3832 |
| Numeric | Modules | QRCoder | 110.03 | 181.20 | 73.91–217.43 | 5568 |
| Numeric | Modules | ZXing-C++ | 59.17 | 83.83 | 30.09–91.41 | 1696 |
| Numeric | Modules | ZXing.Net | 66.31 | 99.99 | 40.68–104.47 | 31304 |
| Numeric | Png | CodeGlyphX | 99.60 | 132.37 | 77.69–141.45 | 40312 |
| Numeric | Png | QRCoder | 185.24 | 218.11 | 144.53–227.08 | 6568 |
| Numeric | Png | ZXing-C++ | 851.64 | 1021.12 | 731.87–1111.51 | 848333 |
| Numeric | Png | ZXing.Net | 826.79 | 996.16 | 733.50–1124.89 | 877588 |
| Unicode | Modules | CodeGlyphX | 1846.87 | 2617.06 | 1584.17–2666.53 | 23872 |
| Unicode | Modules | QRCoder | 2914.11 | 3582.68 | 2443.76–3584.76 | 23216 |
| Unicode | Modules | ZXing-C++ | 1159.77 | 1504.32 | 1054.35–1714.98 | 14648 |
| Unicode | Modules | ZXing.Net | 1861.80 | 2531.83 | 1582.50–2871.68 | 606586 |
| Unicode | Png | CodeGlyphX | 2140.75 | 2680.92 | 1815.56–2752.88 | 274887 |
| Unicode | Png | QRCoder | 3774.07 | 5116.77 | 2955.54–5295.10 | 26278 |
| Unicode | Png | ZXing-C++ | 8513.04 | 14914.44 | 7100.53–17769.10 | 6206857 |
| Unicode | Png | ZXing.Net | 10962.62 | 13834.85 | 8217.49–14186.42 | 6798390 |
| Url | Modules | CodeGlyphX | 696.51 | 935.50 | 623.34–938.56 | 5057 |
| Url | Modules | QRCoder | 879.59 | 1064.33 | 768.62–1143.19 | 7409 |
| Url | Modules | ZXing-C++ | 125.84 | 208.12 | 57.70–251.18 | 2709 |
| Url | Modules | ZXing.Net | 736.71 | 892.55 | 437.43–921.34 | 71128 |
| Url | Png | CodeGlyphX | 169.79 | 250.64 | 151.32–276.91 | 60283 |
| Url | Png | QRCoder | 437.30 | 606.28 | 396.74–670.91 | 8200 |
| Url | Png | ZXing-C++ | 1299.68 | 1849.41 | 1199.64–2175.51 | 1309050 |
| Url | Png | ZXing.Net | 1356.26 | 2238.41 | 1272.68–2292.96 | 1377499 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1709.68 | 1829.96 | 1602.26–1873.48 | 88.59 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 27.78 | 29.73 | 23.79–30.11 | 2.22 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 204.66 | 223.40 | 184.13–231.81 | 12.98 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1536.99 | 1615.36 | 1473.14–1651.63 | 86.83 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 22.96 | 23.95 | 20.83–24.27 | 2.22 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 171.75 | 198.32 | 162.71–199.31 | 12.98 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Unsupported MaxiCode, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

In this round, current median batch time is 1.4% lower on CCD0, 4.6% lower on CCD1.

The current binary has an unfinished image search in 6/24 measured batches, versus 9/24 for the previous binary. All samples and outcomes remain available; a faster unfinished search does not count as a successful rejection.

The separate PDF417-hinted diagnostic isolates the changed owner; whole-image and unchanged retail costs also retain effects of shared-host contention. None establishes a portable performance change.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2402.59 | 2934.11 | 2301.10–3013.06 | 82.89 | 17–18 | 0–0 | 0–1 | 0.986 |
| ccd0 | Candidate | 2368.14 | 2577.03 | 2268.68–2648.47 | 88.66 | 17–18 | 0–0 | 0–1 | 0.986 |
| ccd1 | Baseline | 2432.30 | 3067.53 | 2003.61–3112.54 | 82.52 | 17–18 | 0–0 | 0–1 | 0.954 |
| ccd1 | Candidate | 2319.98 | 2834.16 | 2026.75–2863.09 | 85.13 | 17–18 | 0–0 | 0–1 | 0.954 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
