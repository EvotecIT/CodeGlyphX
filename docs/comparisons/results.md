# Current barcode comparisons

Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.

See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.

## Recognition

The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.

| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| CodeGlyphX | 73/267 | 121/267 | 18 | 1 | 0 | 12 |
| ZXing.Net | 220/267 | 220/267 | 0 | 6 | 2 | 0 |
| ZXing-C++ | 224/267 | 224/267 | 0 | 3 | 0 | 0 |

Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.

| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |
| --- | ---: | ---: | ---: | ---: | ---: |
| All | 12 | 0 | 0 | 10 | 12 |
| Aztec | 12 | 4 | 4 | 5 | 12 |
| Codabar | 6 | 4 | 4 | 6 | 6 |
| Code128 | 14 | 5 | 10 | 13 | 13 |
| Code39 | 12 | 1 | 9 | 12 | 12 |
| Code93 | 3 | 2 | 2 | 3 | 3 |
| DataMatrix | 18 | 8 | 12 | 18 | 18 |
| Ean | 30 | 4 | 15 | 19 | 19 |
| Ean8 | 6 | 4 | 6 | 6 | 6 |
| Gs1DataBarExpanded | 18 | 0 | 0 | 18 | 18 |
| Gs1DataBarExpandedStacked | 12 | 0 | 0 | 12 | 11 |
| Gs1DataBarOmnidirectional | 12 | 4 | 4 | 8 | 8 |
| Itf | 12 | 10 | 10 | 12 | 12 |
| MaxiCode | 6 | 0 | 0 | 6 | 6 |
| Pdf417 | 18 | 2 | 2 | 18 | 17 |
| QrCode | 37 | 22 | 22 | 34 | 34 |
| UpcA | 36 | 0 | 11 | 19 | 19 |
| UpcE | 15 | 3 | 10 | 11 | 10 |

Previously correct images now missed: none in this run.

## Supplementary negative corpus

All 25 images in the two upstream false-positive directories are checked, including the twelve in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.

| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |
| --- | ---: | ---: | ---: | ---: |
| CodeGlyphX | 0/25 | 0/25 | 0 | 25 |
| ZXing.Net | 21/25 | 21/25 | 4 | 0 |
| ZXing-C++ | 25/25 | 25/25 | 0 | 0 |

## Generation on ccd0

Source `34bd19ec0d55313fdc178807c34534be09bf966a`, affinity `0xFFFF`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 106.74 | 145.30 | 91.46–151.17 | 3832 |
| Numeric | Modules | QRCoder | 130.00 | 155.91 | 119.89–161.81 | 5568 |
| Numeric | Modules | ZXing-C++ | 48.65 | 85.32 | 41.82–88.00 | 1696 |
| Numeric | Modules | ZXing.Net | 78.52 | 162.80 | 63.78–221.66 | 31304 |
| Numeric | Png | CodeGlyphX | 176.51 | 388.91 | 124.64–524.08 | 40340 |
| Numeric | Png | QRCoder | 274.35 | 530.61 | 225.49–723.92 | 6737 |
| Numeric | Png | ZXing-C++ | 1228.61 | 2970.95 | 1019.65–2998.59 | 848372 |
| Numeric | Png | ZXing.Net | 1616.84 | 3472.70 | 1081.43–4394.64 | 877618 |
| Unicode | Modules | CodeGlyphX | 2406.34 | 4658.56 | 2020.79–6249.21 | 23872 |
| Unicode | Modules | QRCoder | 4442.03 | 6792.58 | 3629.08–6993.91 | 44788 |
| Unicode | Modules | ZXing-C++ | 1749.51 | 3364.40 | 1380.79–3766.07 | 14648 |
| Unicode | Modules | ZXing.Net | 2481.62 | 3910.07 | 1917.57–4487.96 | 606591 |
| Unicode | Png | CodeGlyphX | 2413.09 | 3129.71 | 2126.26–3342.50 | 274942 |
| Unicode | Png | QRCoder | 4200.95 | 6869.66 | 3461.34–8035.22 | 26681 |
| Unicode | Png | ZXing-C++ | 9702.67 | 15857.87 | 9251.72–16095.76 | 6228264 |
| Unicode | Png | ZXing.Net | 11989.38 | 16826.58 | 10915.97–18919.53 | 6798139 |
| Url | Modules | CodeGlyphX | 1062.22 | 2570.55 | 385.05–3443.89 | 5057 |
| Url | Modules | QRCoder | 1323.47 | 2675.95 | 490.74–2733.63 | 7411 |
| Url | Modules | ZXing-C++ | 241.10 | 1198.15 | 137.89–1274.45 | 2709 |
| Url | Modules | ZXing.Net | 884.59 | 1551.49 | 285.04–1876.57 | 71135 |
| Url | Png | CodeGlyphX | 272.86 | 452.59 | 211.82–467.56 | 60305 |
| Url | Png | QRCoder | 577.58 | 807.20 | 498.53–807.77 | 8375 |
| Url | Png | ZXing-C++ | 1972.70 | 2378.37 | 1669.66–2378.94 | 1313880 |
| Url | Png | ZXing.Net | 2120.33 | 2801.06 | 1859.46–3121.71 | 1377772 |

## Generation on ccd1

Source `34bd19ec0d55313fdc178807c34534be09bf966a`, affinity `0xFFFF0000`, priority `Normal`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.

| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Numeric | Modules | CodeGlyphX | 147.16 | 236.79 | 106.04–237.77 | 3832 |
| Numeric | Modules | QRCoder | 209.96 | 257.73 | 155.15–260.18 | 11956 |
| Numeric | Modules | ZXing-C++ | 77.19 | 152.26 | 45.77–173.82 | 1696 |
| Numeric | Modules | ZXing.Net | 110.60 | 174.49 | 74.72–179.52 | 31304 |
| Numeric | Png | CodeGlyphX | 164.88 | 325.55 | 127.52–341.87 | 40340 |
| Numeric | Png | QRCoder | 300.30 | 603.59 | 240.07–604.63 | 6743 |
| Numeric | Png | ZXing-C++ | 1852.15 | 2630.94 | 1234.87–2879.57 | 848338 |
| Numeric | Png | ZXing.Net | 1701.30 | 2835.72 | 1068.48–3299.67 | 877701 |
| Unicode | Modules | CodeGlyphX | 2144.09 | 2629.74 | 1792.34–2855.45 | 23872 |
| Unicode | Modules | QRCoder | 3820.03 | 4748.42 | 2996.28–5270.31 | 44786 |
| Unicode | Modules | ZXing-C++ | 1490.50 | 2575.92 | 1211.55–2750.11 | 14648 |
| Unicode | Modules | ZXing.Net | 2365.46 | 3758.70 | 1872.31–4634.78 | 606588 |
| Unicode | Png | CodeGlyphX | 1995.83 | 2593.11 | 1651.27–2645.56 | 275105 |
| Unicode | Png | QRCoder | 3243.36 | 4175.83 | 2758.79–4361.38 | 48361 |
| Unicode | Png | ZXing-C++ | 8320.51 | 10717.08 | 6352.18–10755.10 | 6206870 |
| Unicode | Png | ZXing.Net | 9631.99 | 12079.56 | 7854.97–12966.00 | 6819863 |
| Url | Modules | CodeGlyphX | 1013.47 | 1367.11 | 331.45–1512.84 | 5057 |
| Url | Modules | QRCoder | 1324.99 | 1508.98 | 419.32–1547.50 | 7421 |
| Url | Modules | ZXing-C++ | 216.23 | 320.18 | 93.37–325.31 | 2709 |
| Url | Modules | ZXing.Net | 954.18 | 1357.14 | 211.51–1407.35 | 71136 |
| Url | Png | CodeGlyphX | 315.16 | 415.56 | 186.78–453.30 | 60306 |
| Url | Png | QRCoder | 552.25 | 741.61 | 440.83–818.68 | 8375 |
| Url | Png | ZXing-C++ | 1630.98 | 1850.23 | 1170.26–1893.69 | 1309262 |
| Url | Png | ZXing.Net | 1838.96 | 2324.10 | 1343.42–2357.06 | 1377723 |

## Decode latency

The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.

| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ccd0 | CodeGlyphX | 4115.37 | 4257.41 | 4068.86–4278.72 | 13–13 | 0–0 |
| ccd0 | ZXing-C++ | 31.32 | 58.95 | 21.49–63.43 | 19–19 | 0–0 |
| ccd0 | ZXing.Net | 218.50 | 353.76 | 153.69–372.82 | 19–19 | 0–0 |
| ccd1 | CodeGlyphX | 4094.72 | 4234.32 | 4084.75–4343.43 | 13–13 | 0–0 |
| ccd1 | ZXing-C++ | 29.90 | 42.58 | 23.51–44.01 | 19–19 | 0–0 |
| ccd1 | ZXing.Net | 196.05 | 327.93 | 176.16–339.04 | 19–19 | 0–0 |

All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.
