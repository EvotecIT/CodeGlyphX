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

This unchanged 38-image set comes from the same upstream collection as the ranking. Five images have informed permanent regression fixtures across the remediation rounds; it is supplemental regression evidence, not an unseen holdout. Four newly recovered framing-damage cases fail the previous binary in direct scans and focused contract checks.

| Engine | Previous pass | Current pass | Unexpected-output images |
| --- | ---: | ---: | ---: |
| CodeGlyphX | 34/38 | 38/38 | 0 |
| ZXing.Net | 38/38 | 38/38 | 0 |
| ZXing-C++ | 35/38 | 35/38 | 0 |

CodeGlyphX gains: pdf417-2/25.png, pdf417-3/13.png, pdf417-3/14.png, pdf417-3/15.png. Lost previous successes: none.

The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set.

The four damaged-framing regression images change from 0/4 to 4/4 exact payloads. The previous binary also fails 10 of 18 selected contract checks; the candidate passes all 26 recovery checks and 294 focused owner/budget/scanner checks.

A separate PDF417-hinted check completes all 31 distinct negative images with no unexpected output for each reader. This diagnostic does not replace all-format negative completion. Its [unchanged inputs](evidence/pdf417-framing-negative-manifest.json), [outcomes](evidence/pdf417-framing-negative-outcomes.json), [four regression inputs](evidence/pdf417-framing-regression-manifest.json), [previous results](evidence/pdf417-framing-previous-misses.json) and [current results](evidence/pdf417-framing-current-misses.json) are retained.

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
| ccd0 | 305.89 | 324.40 | 1.06x | 21.38 | 21.39 | 1.00x | 9/9 → 9/9 |
| ccd1 | 546.33 | 662.30 | 1.21x | 21.39 | 21.37 | 1.00x | 9/9 → 9/9 |

Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.

[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json).

This follow-up records 219/267 correct positive images versus 219/267 previously; gained: none; previously correct positive images lost: none. Paired negative completion is [29, 29, 30, 28]/31 currently and [29, 30, 28, 29]/31 previously. PDF417 framing recovery changes recognition hypotheses within the original allowance. Paired nineteen-image costs and nine-request retail costs remain separate, with every sample and recognition failure retained. No recognition allowance, hint, oracle or scoring change. Supplemental PDF417 images are regression evidence informed by this remediation, not an unseen holdout. Invalid GS1 AI (3922) oracles and undeclared QR encoding policies remain unchanged. Shared-host contention and budgets affect results. Stacked DataBar/Expanded Stacked multiple-symbol localization and arbitrary perspective remain unproved; MaxiCode image scanning is unsupported. These finite workloads do not establish overall superiority.

Host CPU samples before the main corpus pass: 94.1%, 96.0%, 89.1%. Before the supplementary negative pass: 45.1%, 58.8%, 74.7%. These are separate passes; overlapping images can have different completion outcomes.

## Supplementary negative corpus

The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 23/25 | 0 | 2 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes 23/25 supplementary negatives versus 24/25. Previously completed images now unfinished: falsepositives-2/02.png

## Paired negative completion

The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.

| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| ccd0 | 1 | previous | 29/31 | 2 | 0 | 51.4% |
| ccd0 | 2 | current | 29/31 | 2 | 0 | 43.2% |
| ccd0 | 3 | current | 29/31 | 2 | 0 | 59.2% |
| ccd0 | 4 | previous | 30/31 | 1 | 0 | 52.4% |
| ccd1 | 1 | current | 30/31 | 1 | 0 | 69.8% |
| ccd1 | 2 | previous | 28/31 | 3 | 0 | 50.5% |
| ccd1 | 3 | previous | 29/31 | 2 | 0 | 86.0% |
| ccd1 | 4 | current | 28/31 | 3 | 0 | 82.8% |

## Initial host load

Three samples before each pass retain overall and per-logical-processor CPU use, with means for both verified affinity domains. They show initial placement/load only; they do not measure contention throughout the pass or establish an idle machine.

| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |
| --- | ---: | ---: | ---: |
| corpus-ccd0 | 93.0% | 96.7% | 89.4% |
| decoding-ccd0 | 87.7% | 91.1% | 84.4% |
| decoding-ccd1 | 68.2% | 77.3% | 59.2% |
| diagnostic-final-retail-negative | 59.5% | 73.0% | 46.0% |
| diagnostic-final-retail-positive | 53.3% | 67.6% | 39.0% |
| generation-ccd0 | 82.7% | 89.4% | 75.9% |
| generation-ccd1 | 51.0% | 64.1% | 37.8% |
| holdout-current | 61.1% | 74.0% | 48.2% |
| negative-rotation-ccd0-0-previous | 51.4% | 66.8% | 36.1% |
| negative-rotation-ccd0-1-current | 43.2% | 60.1% | 26.3% |
| negative-rotation-ccd0-2-current | 59.2% | 70.7% | 47.9% |
| negative-rotation-ccd0-3-previous | 52.4% | 65.3% | 39.5% |
| negative-rotation-ccd1-0-current | 69.8% | 79.3% | 60.3% |
| negative-rotation-ccd1-1-previous | 50.5% | 62.6% | 38.3% |
| negative-rotation-ccd1-2-previous | 86.0% | 90.6% | 81.3% |
| negative-rotation-ccd1-3-current | 82.8% | 89.2% | 76.4% |
| negatives-ccd0 | 59.5% | 71.1% | 47.9% |

## Generation on ccd0

Source `02f8cfa12c93c0f01ec889d2e0bd6ad1d9b0d272`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 111.74 | 147.34 | 90.72–153.21 | 3832 |
| Numeric | Modules | QRCoder | 174.43 | 242.69 | 126.23–252.74 | 5569 |
| Numeric | Modules | ZXing-C++ | 51.70 | 112.03 | 38.74–132.58 | 1696 |
| Numeric | Modules | ZXing.Net | 75.47 | 237.52 | 48.72–358.08 | 31304 |
| Numeric | Png | CodeGlyphX | 136.24 | 264.31 | 101.16–391.42 | 40340 |
| Numeric | Png | QRCoder | 248.86 | 456.37 | 148.05–523.96 | 6737 |
| Numeric | Png | ZXing-C++ | 1057.11 | 2134.69 | 818.02–2562.74 | 848473 |
| Numeric | Png | ZXing.Net | 1053.05 | 2991.94 | 897.43–4330.97 | 877752 |
| Unicode | Modules | CodeGlyphX | 2081.21 | 4329.69 | 1918.71–5785.52 | 23872 |
| Unicode | Modules | QRCoder | 3185.00 | 6860.07 | 2941.15–7921.76 | 23221 |
| Unicode | Modules | ZXing-C++ | 1381.79 | 2717.80 | 1230.30–3125.30 | 14648 |
| Unicode | Modules | ZXing.Net | 2057.52 | 3488.47 | 1860.42–4329.38 | 606584 |
| Unicode | Png | CodeGlyphX | 2183.72 | 2951.38 | 2028.22–3594.54 | 274942 |
| Unicode | Png | QRCoder | 3435.85 | 3820.70 | 3228.42–3869.35 | 26799 |
| Unicode | Png | ZXing-C++ | 8754.37 | 9776.75 | 8222.95–9990.02 | 6207163 |
| Unicode | Png | ZXing.Net | 10647.56 | 11795.55 | 9837.03–11831.26 | 6798276 |
| Url | Modules | CodeGlyphX | 1142.19 | 1626.21 | 233.92–2056.41 | 5057 |
| Url | Modules | QRCoder | 1263.19 | 2057.40 | 410.17–2240.24 | 7409 |
| Url | Modules | ZXing-C++ | 195.15 | 411.58 | 99.96–481.81 | 2705 |
| Url | Modules | ZXing.Net | 1147.08 | 3610.08 | 317.64–3970.35 | 71130 |
| Url | Png | CodeGlyphX | 248.91 | 310.36 | 194.35–319.23 | 60319 |
| Url | Png | QRCoder | 529.15 | 613.78 | 487.97–615.40 | 8388 |
| Url | Png | ZXing-C++ | 1724.65 | 1816.05 | 1599.23–1861.95 | 1309142 |
| Url | Png | ZXing.Net | 1797.69 | 1901.03 | 1587.18–1901.31 | 1377659 |

## Generation on ccd1

Source `02f8cfa12c93c0f01ec889d2e0bd6ad1d9b0d272`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 73.66 | 148.97 | 59.04–173.74 | 3832 |
| Numeric | Modules | QRCoder | 76.58 | 135.65 | 70.56–146.41 | 5568 |
| Numeric | Modules | ZXing-C++ | 29.83 | 58.95 | 28.58–61.26 | 1696 |
| Numeric | Modules | ZXing.Net | 51.01 | 70.88 | 37.43–75.00 | 31304 |
| Numeric | Png | CodeGlyphX | 102.69 | 140.25 | 76.23–157.50 | 40340 |
| Numeric | Png | QRCoder | 145.81 | 176.87 | 141.30–194.57 | 6737 |
| Numeric | Png | ZXing-C++ | 679.83 | 904.43 | 617.38–1054.46 | 848424 |
| Numeric | Png | ZXing.Net | 769.82 | 1013.90 | 644.43–1046.50 | 877744 |
| Unicode | Modules | CodeGlyphX | 1966.73 | 2964.20 | 1490.30–3411.95 | 23872 |
| Unicode | Modules | QRCoder | 2694.48 | 3377.86 | 2412.94–3525.82 | 23221 |
| Unicode | Modules | ZXing-C++ | 1192.74 | 1868.77 | 1053.79–2475.52 | 14648 |
| Unicode | Modules | ZXing.Net | 1929.26 | 2645.68 | 1517.56–3100.62 | 606585 |
| Unicode | Png | CodeGlyphX | 2098.79 | 3412.50 | 1925.71–3846.41 | 274942 |
| Unicode | Png | QRCoder | 3772.67 | 4425.41 | 3156.76–4874.37 | 26805 |
| Unicode | Png | ZXing-C++ | 9765.85 | 11395.14 | 9079.55–11426.34 | 6207323 |
| Unicode | Png | ZXing.Net | 11715.27 | 14193.67 | 10178.99–14289.70 | 6798355 |
| Url | Modules | CodeGlyphX | 1120.11 | 1329.84 | 143.66–1463.73 | 5057 |
| Url | Modules | QRCoder | 1177.37 | 1494.78 | 367.11–1550.02 | 7411 |
| Url | Modules | ZXing-C++ | 182.64 | 362.83 | 97.32–434.85 | 2705 |
| Url | Modules | ZXing.Net | 1001.06 | 1312.61 | 262.10–1352.44 | 71135 |
| Url | Png | CodeGlyphX | 215.02 | 363.68 | 143.47–411.64 | 60305 |
| Url | Png | QRCoder | 547.18 | 716.33 | 424.66–732.01 | 8375 |
| Url | Png | ZXing-C++ | 1367.78 | 1701.62 | 1252.90–1727.15 | 1309246 |
| Url | Png | ZXing.Net | 1650.85 | 1927.53 | 1340.63–1980.13 | 1377603 |

## Decode latency

The nineteen-image subset includes a negative, an unsupported MaxiCode image and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 1724.48 | 1884.52 | 1521.62–1905.46 | 91.71 | 18–18 | 0–0 |
| ccd0 | ZXing-C++ | 28.81 | 41.23 | 22.09–41.52 | 2.23 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 215.21 | 268.32 | 170.84–303.11 | 13.05 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 1766.23 | 1834.27 | 1504.87–1856.55 | 90.84 | 18–18 | 0–0 |
| ccd1 | ZXing-C++ | 28.28 | 36.93 | 23.38–42.80 | 2.24 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 233.49 | 301.58 | 167.11–322.84 | 13.01 | 19–19 | 0–0 |

## Paired whole-image decoding cost

Same nineteen preloaded RGBA32 images, options and 1500 ms allowance as the decoding comparison. Separate assembly contexts compare the previous and current binary in grouped rotated order: 3 warmups and 12 measurements per engine on each verified affinity domain. Input loading and outcome evaluation are excluded. Unsupported MaxiCode, negative-search completion and four-QR collection remain in the workload. Shared-host contention prevents portable latency claims; raw packets retain all samples and correctness metrics.

In this round, current median batch time is 5.4% higher on CCD0, 3.4% higher on CCD1.

The current binary has an unfinished image search in 12/24 measured batches, versus 9/24 for the previous binary. All samples and outcomes remain available; a faster unfinished search does not count as a successful rejection.

The unchanged strict-retail workload also slows in this shared-host run. These results retain the observed costs and completion losses without establishing a portable performance change or its cause.

Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.

| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ccd0 | Baseline | 2492.83 | 2873.88 | 2354.14–2976.91 | 85.44 | 17–18 | 0–0 | 0–1 | 1.054 |
| ccd0 | Candidate | 2627.68 | 3445.05 | 2436.37–3574.80 | 81.86 | 17–18 | 0–0 | 0–1 | 1.054 |
| ccd1 | Baseline | 2579.09 | 2883.30 | 2191.62–2958.84 | 83.71 | 17–18 | 0–0 | 0–1 | 1.034 |
| ccd1 | Candidate | 2665.91 | 3016.65 | 2284.31–3080.95 | 83.34 | 17–18 | 0–0 | 0–1 | 1.034 |

[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json).

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
