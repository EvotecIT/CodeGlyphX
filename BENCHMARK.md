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

Updated: 2026-01-23 18:57:42 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260123-195637
How to read:
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x (fastest) means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the allocation of the fastest-time vendor for that scenario. Lower than 1 x means fewer allocations than the fastest-time vendor.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
- Benchmarks run under controlled, ideal conditions on a single machine; treat results as directional, not definitive.
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

| Benchmark | Scenario | Fastest | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Aztec (Encode) | Aztec PNG | CodeGlyphX 294.2 μs | 294.2 μs<br>61.34 KB | 2,089.4 μs<br>61.42 KB<br>Δ 7.1 x / 1 x |  | 6,735.0 μs<br>642.58 KB<br>Δ 22.89 x / 10.48 x | 1 x (fastest) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 117.7 μs | 117.7 μs<br>14.43 KB | 1,540.7 μs<br>15.74 KB<br>Δ 13.09 x / 1.09 x |  | 53,838.6 μs<br>2035.16 KB<br>Δ 457.42 x / 141.04 x | 1 x (fastest) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 122.4 μs | 122.4 μs<br>10.02 KB | 1,596.1 μs<br>12.28 KB<br>Δ 13.04 x / 1.23 x |  | 38,646.2 μs<br>1448.61 KB<br>Δ 315.74 x / 144.57 x | 1 x (fastest) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 163.4 μs | 163.4 μs<br>8.13 KB | 1,163.0 μs<br>11.7 KB<br>Δ 7.12 x / 1.44 x |  | 28,295.0 μs<br>957.26 KB<br>Δ 173.16 x / 117.74 x | 1 x (fastest) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 178.5 μs | 178.5 μs<br>15.52 KB | 1,729.0 μs<br>22.31 KB<br>Δ 9.69 x / 1.44 x |  | 8,132.1 μs<br>645.05 KB<br>Δ 45.56 x / 41.56 x | 1 x (fastest) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 200.0 μs | 200.0 μs<br>6.84 KB | 1,119.5 μs<br>11.68 KB<br>Δ 5.6 x / 1.71 x |  | 23,877.1 μs<br>850.67 KB<br>Δ 119.39 x / 124.37 x | 1 x (fastest) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.084 ms | 1.084 ms<br>49.9 KB | 5.787 ms<br>207.63 KB<br>Δ 5.34 x / 4.16 x |  | 49.655 ms<br>5003.84 KB<br>Δ 45.81 x / 100.28 x | 1 x (fastest) | 1 x | good |
| QR (Encode) | QR PNG (medium) | QRCoder 783.3 μs | 954.9 μs<br>14.86 KB | 3,712.8 μs<br>79.41 KB<br>Δ 3.89 x / 5.34 x | 783.3 μs<br>7.31 KB<br>Δ 0.82 x / 0.49 x | 13,039.5 μs<br>1547.36 KB<br>Δ 13.66 x / 104.13 x | 1.22 x | 2.03 x | bad |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.882 ms | 2.491 ms<br>5.27 KB | 1.882 ms<br>127.67 KB<br>Δ 0.76 x / 24.23 x |  |  | 1.32 x | 0.04 x | ok |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.679 ms | 5.942 ms<br>5.45 KB | 3.679 ms<br>706.89 KB<br>Δ 0.62 x / 129.7 x |  |  | 1.62 x | 0.01 x | bad |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 138.3 μs | 138.3 μs<br>7.15 KB | 948.9 μs<br>11.75 KB<br>Δ 6.86 x / 1.64 x |  | 19,106.6 μs<br>765.73 KB<br>Δ 138.15 x / 107.1 x | 1 x (fastest) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 129.50 μs | 14.51 KB |
| Code 128 SVG | 19.47 μs | 17.61 KB |
| EAN PNG | 132.47 μs | 6.92 KB |
| Code 39 PNG | 150.30 μs | 10.09 KB |
| Code 93 PNG | 130.70 μs | 8.21 KB |
| UPC-A PNG | 149.03 μs | 7.23 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 255.73 μs | 10.16 KB |
| Data Matrix PNG (long) | 324.67 μs | 20.34 KB |
| Data Matrix SVG | 47.93 μs | 12.29 KB |
| PDF417 PNG | 1,035.00 μs | 49.99 KB |
| PDF417 SVG | 2,280.07 μs | 64.53 KB |
| Aztec PNG | 212.50 μs | 61.38 KB |
| Aztec SVG | 69.97 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 503.2 μs | 9.66 KB |
| QR PNG (medium text) | 818.9 μs | 15.03 KB |
| QR PNG (long text) | 2,804.6 μs | 38.8 KB |
| QR SVG (medium text) | 741.6 μs | 19.05 KB |
| QR PNG High Error Correction | 1,146.4 μs | 21.62 KB |
| QR HTML (medium text) | 791.2 μs | 136.45 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 2,204.1 μs | 5.27 KB |
| QR Decode (clean, balanced) | 2,263.4 μs | 5.27 KB |
| QR Decode (clean, robust) | 2,296.5 μs | 5.27 KB |
| QR Decode (noisy, robust) | 5,295.6 μs | 5.45 KB |
| QR Decode (screenshot, balanced) | 10,646.9 μs | 5.27 KB |
| QR Decode (antialias, robust) | 443.2 μs | 1.32 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 387.97 μs | 1.22 KB |
| QR Encode (medium text) | 900.60 μs | 1.83 KB |
| QR Encode (long text) | 2,027.30 μs | 4.58 KB |
| QR Render PNG (short, pre-encoded) | 137.90 μs | 8.37 KB |
| QR Render PNG (medium, pre-encoded) | 42.53 μs | 13.13 KB |
| QR Render PNG (long, pre-encoded) | 559.90 μs | 34.15 KB |
| QR Render Pixels (short, pre-encoded) | 26.42 μs | 118.29 KB |
| QR Render Pixels (medium, pre-encoded) | 35.53 μs | 192.54 KB |
| QR Render Pixels (long, pre-encoded) | 668.50 μs | 523.29 KB |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 294.2 μs<br>61.34 KB | 2,089.4 μs<br>61.42 KB<br>Δ 7.1 x / 1 x |  | 6,735.0 μs<br>642.58 KB<br>Δ 22.89 x / 10.48 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 117.7 μs<br>14.43 KB | 1,540.7 μs<br>15.74 KB<br>Δ 13.09 x / 1.09 x |  | 53,838.6 μs<br>2035.16 KB<br>Δ 457.42 x / 141.04 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 122.4 μs<br>10.02 KB | 1,596.1 μs<br>12.28 KB<br>Δ 13.04 x / 1.23 x |  | 38,646.2 μs<br>1448.61 KB<br>Δ 315.74 x / 144.57 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 163.4 μs<br>8.13 KB | 1,163.0 μs<br>11.7 KB<br>Δ 7.12 x / 1.44 x |  | 28,295.0 μs<br>957.26 KB<br>Δ 173.16 x / 117.74 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 178.5 μs<br>15.52 KB | 1,729.0 μs<br>22.31 KB<br>Δ 9.69 x / 1.44 x |  | 8,132.1 μs<br>645.05 KB<br>Δ 45.56 x / 41.56 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 200.0 μs<br>6.84 KB | 1,119.5 μs<br>11.68 KB<br>Δ 5.6 x / 1.71 x |  | 23,877.1 μs<br>850.67 KB<br>Δ 119.39 x / 124.37 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.084 ms<br>49.9 KB | 5.787 ms<br>207.63 KB<br>Δ 5.34 x / 4.16 x |  | 49.655 ms<br>5003.84 KB<br>Δ 45.81 x / 100.28 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 954.9 μs<br>14.86 KB | 3,712.8 μs<br>79.41 KB<br>Δ 3.89 x / 5.34 x | 783.3 μs<br>7.31 KB<br>Δ 0.82 x / 0.49 x | 13,039.5 μs<br>1547.36 KB<br>Δ 13.66 x / 104.13 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.491 ms<br>5.27 KB | 1.882 ms<br>127.67 KB<br>Δ 0.76 x / 24.23 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 5.942 ms<br>5.45 KB | 3.679 ms<br>706.89 KB<br>Δ 0.62 x / 129.7 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 138.3 μs<br>7.15 KB | 948.9 μs<br>11.75 KB<br>Δ 6.86 x / 1.64 x |  | 19,106.6 μs<br>765.73 KB<br>Δ 138.15 x / 107.1 x |
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
