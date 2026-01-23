# Benchmarks

**Data locations**
- Generated files are overwritten on each run (do not edit by hand).
- Human-readable report: `BENCHMARK.md`
- Website JSON: `Assets/Data/benchmark.json`
- Summary JSON: `Assets/Data/benchmark-summary.json`
- Index JSON: `Assets/Data/benchmark-index.json`

**Publish flag**
- Quick runs default to publish=false (draft).
- Full runs default to publish=true.
- Override with -Publish or -NoPublish on the report generator.

<!-- BENCHMARK:WINDOWS:QUICK:START -->
## WINDOWS

Updated: 2026-01-23 10:40:46 UTC
Framework: net8.0
Configuration: Release
Artifacts: Build\BenchmarkResults\windows-20260123-111626
How to read:
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
Notes:
- Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1).
- Comparisons target PNG output and include encode+render (not encode-only).
- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.
- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).
- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).
- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).
- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).
- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).

### Summary (Comparisons)

| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating | CodeGlyphX Mean | CodeGlyphX Alloc |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Aztec (Encode) | Aztec PNG | CodeGlyphX 493.8 μs | 1 x | 1 x | good | 493.8 μs | 72.79 KB |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 335.5 μs | 1 x | 1 x | good | 335.5 μs | 32.6 KB |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 619.2 μs | 1 x | 1 x | good | 619.2 μs | 21.53 KB |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 610.5 μs | 1 x | 1 x | good | 610.5 μs | 18.24 KB |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 359.7 μs | 1 x | 1 x | good | 359.7 μs | 27.23 KB |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 536.5 μs | 1 x | 1 x | good | 536.5 μs | 16.02 KB |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.246 ms | 1 x | 1 x | good | 1.246 ms | 119.24 KB |
| QR (Encode) | QR PNG (medium) | QRCoder 901.5 μs | 1.25 x | 2.26 x | bad | 1,131.3 μs | 36.91 KB |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.559 ms | 1.64 x | 1.05 x | bad | 2.556 ms | 134.04 KB |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.761 ms | 1.48 x | 1.46 x | ok | 5.573 ms | 1030.23 KB |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 569.2 μs | 1 x | 1 x | good | 569.2 μs | 16.32 KB |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 265.75 μs | 32.68 KB |
| Code 128 SVG | 30.67 μs | 18.13 KB |
| EAN PNG | 451.03 μs | 16.09 KB |
| Code 39 PNG | 757.57 μs | 21.61 KB |
| Code 93 PNG | 554.73 μs | 18.32 KB |
| UPC-A PNG | 515.77 μs | 16.4 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 391.77 μs | 27.09 KB |
| Data Matrix PNG (long) | 561.70 μs | 54.35 KB |
| Data Matrix SVG | 53.23 μs | 12.81 KB |
| PDF417 PNG | 1,146.37 μs | 119.34 KB |
| PDF417 SVG | 3,266.07 μs | 65.05 KB |
| Aztec PNG | 478.40 μs | 72.84 KB |
| Aztec SVG | 108.27 μs | 60.41 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 834.5 μs | 22.63 KB |
| QR PNG (medium text) | 1,060.4 μs | 37.08 KB |
| QR PNG (long text) | 3,231.6 μs | 107.28 KB |
| QR SVG (medium text) | 998.8 μs | 20.56 KB |
| QR PNG High Error Correction | 1,788.2 μs | 57.93 KB |
| QR HTML (medium text) | 1,021.5 μs | 137.96 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 2,719.9 μs | 134.04 KB |
| QR Decode (clean, balanced) | 2,642.4 μs | 134.04 KB |
| QR Decode (clean, robust) | 3,207.4 μs | 134.04 KB |
| QR Decode (noisy, robust) | 5,003.2 μs | 1030.23 KB |
| QR Decode (screenshot, balanced) | 8,223.5 μs | 2054.04 KB |
| QR Decode (antialias, robust) | 595.6 μs | 146.33 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 459.9 μs | 1.88 KB |
| QR Encode (medium text) | 967.1 μs | 2.81 KB |
| QR Encode (long text) | 2,505.6 μs | 6.97 KB |
| QR Render PNG (short, pre-encoded) | 388.5 μs | 20.68 KB |
| QR Render PNG (medium, pre-encoded) | 141.3 μs | 34.2 KB |
| QR Render PNG (long, pre-encoded) | 355.0 μs | 100.24 KB |
| QR Render Pixels (short, pre-encoded) | 278.7 μs | 236.81 KB |
| QR Render Pixels (medium, pre-encoded) | 368.2 μs | 385.36 KB |
| QR Render Pixels (long, pre-encoded) | 1,440.5 μs | 1047 KB |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 493.8 μs<br>72.79 KB | 1,505.9 μs<br>289.63 KB |  | 6,706.6 μs<br>998.78 KB |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 335.5 μs<br>32.6 KB | 1,919.3 μs<br>371.95 KB |  | 56,215.7 μs<br>2263.41 KB |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 619.2 μs<br>21.53 KB | 1,578.2 μs<br>240.48 KB |  | 45,060.0 μs<br>1676.85 KB |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 610.5 μs<br>18.24 KB | 1,188.9 μs<br>239.91 KB |  | 27,360.8 μs<br>1185.66 KB |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 359.7 μs<br>27.23 KB | 2,460.4 μs<br>250.52 KB |  | 8,743.7 μs<br>1001.25 KB |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 536.5 μs<br>16.02 KB | 1,047.9 μs<br>239.88 KB |  | 22,269.9 μs<br>951.05 KB |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.246 ms<br>119.24 KB | 5.331 ms<br>1335.86 KB |  | 60.440 ms<br>5616.06 KB |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 1,131.3 μs<br>36.91 KB | 3,488.7 μs<br>435.62 KB | 901.5 μs<br>16.34 KB | 16,080.4 μs<br>1903.72 KB |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.556 ms<br>134.04 KB | 1.559 ms<br>127.67 KB |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 5.573 ms<br>1030.23 KB | 3.761 ms<br>706.89 KB |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 569.2 μs<br>16.32 KB | 1,202.1 μs<br>239.95 KB |  | 21,614.8 μs<br>865.95 KB |
<!-- BENCHMARK:WINDOWS:QUICK:END -->

<!-- BENCHMARK:WINDOWS:FULL:START -->
## WINDOWS

Updated: 2026-01-22 15:35:36 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260122-155044
How to read:
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
Notes:
- Run mode: Full (BenchmarkDotNet default job settings).
- Comparisons target PNG output and include encode+render (not encode-only).
- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.
- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).
- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).
- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).
- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).
- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).

### Summary (Comparisons)

| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating | CodeGlyphX Mean | CodeGlyphX Alloc |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.639 ms | 1 x | 1 x | good | 1.639 ms | 87.22 KB |
| QR (Encode) | QR PNG (medium) | QRCoder 1.007 ms | 1.22 x | 3.95 x | bad | 1.224 ms | 28.88 KB |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 2.107 ms | 1.34 x | 0.05 x | ok | 2.833 ms | 6.23 KB |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 4.133 ms | 56.92 x | 11.83 x | bad | 235.230 ms | 8362.01 KB |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 343.67 μs | 24.66 KB |
| Code 128 SVG | 33.90 μs | 17.61 KB |
| EAN PNG | 584.80 μs | 12.07 KB |
| Code 39 PNG | 788.70 μs | 17.59 KB |
| Code 93 PNG | 615.60 μs | 14.3 KB |
| UPC-A PNG | 639.33 μs | 12.38 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 376.47 μs | 19.06 KB |
| Data Matrix PNG (long) | 575.03 μs | 38.33 KB |
| Data Matrix SVG | 58.83 μs | 12.29 KB |
| PDF417 PNG | 1,556.70 μs | 87.31 KB |
| PDF417 SVG | 3,002.73 μs | 64.53 KB |
| Aztec PNG | 634.60 μs | 68.81 KB |
| Aztec SVG | 103.37 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 860.8 μs | 18.6 KB |
| QR PNG (medium text) | 1,071.6 μs | 29.05 KB |
| QR PNG (long text) | 3,477.7 μs | 75.26 KB |
| QR SVG (medium text) | 1,000.6 μs | 20.04 KB |
| QR PNG High Error Correction | 1,562.7 μs | 41.91 KB |
| QR HTML (medium text) | 1,014.7 μs | 137.44 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 3.154 ms | 6.23 KB |
| QR Decode (clean, balanced) | 3.200 ms | 6.23 KB |
| QR Decode (clean, robust) | 3.237 ms | 6.23 KB |
| QR Decode (noisy, robust) | 229.989 ms | 8362.01 KB |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 451.7 μs<br>68.77 KB | 2,190.1 μs<br>61.42 KB |  | 6,925.2 μs<br>642.58 KB |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 324.7 μs<br>24.58 KB | 2,068.7 μs<br>15.74 KB |  | 63,145.3 μs<br>2035.16 KB |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 694.1 μs<br>17.51 KB | 1,908.6 μs<br>12.28 KB |  | 44,118.6 μs<br>1448.45 KB |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 705.1 μs<br>14.22 KB | 1,327.7 μs<br>11.7 KB |  | 31,054.3 μs<br>957.41 KB |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 536.9 μs<br>23.2 KB | 2,353.1 μs<br>22.31 KB |  | 9,218.3 μs<br>645.05 KB |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 568.5 μs<br>11.99 KB | 1,603.5 μs<br>11.68 KB |  | 27,992.5 μs<br>850.83 KB |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.639 ms<br>87.22 KB | 7.730 ms<br>207.63 KB |  | 60.647 ms<br>5003.99 KB |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 1.224 ms<br>28.88 KB | 4.251 ms<br>79.41 KB | 1.007 ms<br>7.31 KB | 16.059 ms<br>1547.36 KB |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.833 ms<br>6.23 KB | 2.107 ms<br>127.67 KB |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 235.230 ms<br>8362.01 KB | 4.133 ms<br>706.89 KB |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 593.7 μs<br>12.3 KB | 1,355.9 μs<br>11.75 KB |  | 24,708.1 μs<br>765.89 KB |
<!-- BENCHMARK:WINDOWS:FULL:END -->

<!-- BENCHMARK:LINUX:QUICK:START -->
## LINUX

Updated: 2026-01-22 22:04:32 UTC
Framework: net8.0
Configuration: Release
Artifacts: /mnt/c/Support/GitHub/CodeMatrix/Build/BenchmarkResults/linux-20260122-230352
How to read:
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
Notes:
- Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1).
- Comparisons target PNG output and include encode+render (not encode-only).
- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.
- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).
- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).
- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).
- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).
- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).
Warnings:
- Missing compare results: Aztec (Encode), Code 128 (Encode), Code 39 (Encode), Code 93 (Encode), Data Matrix (Encode), EAN-13 (Encode), PDF417 (Encode), QR (Encode), UPC-A (Encode).

### Baseline

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 3,076.8 μs | 5.26 KB |
| QR Decode (clean, balanced) | 3,052.8 μs | 5.26 KB |
| QR Decode (clean, robust) | 2,435.8 μs | 5.26 KB |
| QR Decode (noisy, robust) | 4,481.1 μs | 5.45 KB |
| QR Decode (screenshot, balanced) | 7,166.1 μs | 5.26 KB |
| QR Decode (antialias, robust) | 477.0 μs | 1.31 KB |

### Comparisons

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.215 ms<br>5.26 KB |  |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 4.090 ms<br>5.45 KB |  |  |  |
<!-- BENCHMARK:LINUX:QUICK:END -->

<!-- BENCHMARK:LINUX:FULL:START -->
_no results yet_
<!-- BENCHMARK:LINUX:FULL:END -->

<!-- BENCHMARK:MACOS:QUICK:START -->
_no results yet_
<!-- BENCHMARK:MACOS:QUICK:END -->

<!-- BENCHMARK:MACOS:FULL:START -->
_no results yet_
<!-- BENCHMARK:MACOS:FULL:END -->
