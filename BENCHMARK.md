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
## WINDOWS (Quick)

Updated: 2026-01-24 08:30:07 UTC
Framework: net8.0
Configuration: Release
Artifacts: Build\BenchmarkResults\windows-20260123-225838
**How to read**
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x (fastest) means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the allocation of the fastest-time vendor for that scenario. Lower than 1 x means fewer allocations than the fastest-time vendor.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
- Benchmarks run under controlled, ideal conditions on a single machine; treat results as directional, not definitive.

**Notes**
- Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1).
- Comparisons target PNG output and include encode+render (not encode-only).
- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.
- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).
- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).
- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).
- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).
- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).

### Summary (Comparisons) - Quick

| Benchmark | Scenario | Fastest | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Aztec (Encode) | Aztec PNG | CodeGlyphX 220.1 μs | 220.1 μs<br>65.41 KB | 1,939.3 μs<br>289.63 KB<br>Δ 8.81 x / 4.43 x |  | 8,071.5 μs<br>998.83 KB<br>Δ 36.67 x / 15.27 x | 1 x (fastest) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 164.1 μs | 164.1 μs<br>22.54 KB | 2,211.9 μs<br>371.95 KB<br>Δ 13.48 x / 16.5 x |  | 54,378.4 μs<br>2263.41 KB<br>Δ 331.37 x / 100.42 x | 1 x (fastest) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 180.3 μs | 180.3 μs<br>14.13 KB | 1,771.1 μs<br>240.48 KB<br>Δ 9.82 x / 17.02 x |  | 43,291.1 μs<br>1676.85 KB<br>Δ 240.11 x / 118.67 x | 1 x (fastest) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 174.5 μs | 174.5 μs<br>12.24 KB | 1,295.9 μs<br>239.91 KB<br>Δ 7.43 x / 19.6 x |  | 28,977.2 μs<br>1185.5 KB<br>Δ 166.06 x / 96.85 x | 1 x (fastest) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 230.9 μs | 230.9 μs<br>19.6 KB | 2,211.2 μs<br>250.52 KB<br>Δ 9.58 x / 12.78 x |  | 10,329.6 μs<br>1001.25 KB<br>Δ 44.74 x / 51.08 x | 1 x (fastest) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 193.4 μs | 193.4 μs<br>10.92 KB | 1,114.3 μs<br>239.88 KB<br>Δ 5.76 x / 21.97 x |  | 26,486.2 μs<br>951.05 KB<br>Δ 136.95 x / 87.09 x | 1 x (fastest) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.491 ms | 1.491 ms<br>82.07 KB | 7.206 ms<br>1335.86 KB<br>Δ 4.83 x / 16.28 x |  | 56.081 ms<br>5616.22 KB<br>Δ 37.61 x / 68.43 x | 1 x (fastest) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 750.7 μs | 750.7 μs<br>22.27 KB | 4,424.8 μs<br>435.62 KB<br>Δ 5.89 x / 19.56 x | 967.5 μs<br>16.34 KB<br>Δ 1.29 x / 0.73 x | 16,056.4 μs<br>1903.56 KB<br>Δ 21.39 x / 85.48 x | 1 x (fastest) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.951 ms | 2.044 ms<br>134.05 KB | 1.951 ms<br>127.67 KB<br>Δ 0.95 x / 0.95 x |  |  | 1.05 x | 1.05 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 4.308 ms | 4.308 ms<br>1030.23 KB | 4.582 ms<br>706.89 KB<br>Δ 1.06 x / 0.69 x |  |  | 1 x (fastest) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 247.0 μs | 247.0 μs<br>11.23 KB | 1,406.6 μs<br>239.95 KB<br>Δ 5.69 x / 21.37 x |  | 24,452.9 μs<br>866.11 KB<br>Δ 99 x / 77.12 x | 1 x (fastest) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 211.70 μs | 22.62 KB |
| Code 128 SVG | 49.67 μs | 18.13 KB |
| EAN PNG | 232.73 μs | 11 KB |
| Code 39 PNG | 211.73 μs | 14.2 KB |
| Code 93 PNG | 237.32 μs | 12.32 KB |
| UPC-A PNG | 165.57 μs | 11.3 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 205.20 μs | 18.24 KB |
| Data Matrix PNG (long) | 417.93 μs | 36.45 KB |
| Data Matrix SVG | 66.73 μs | 12.81 KB |
| PDF417 PNG | 1,323.63 μs | 82.16 KB |
| PDF417 SVG | 3,434.90 μs | 65.05 KB |
| Aztec PNG | 299.97 μs | 65.46 KB |
| Aztec SVG | 116.77 μs | 60.41 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 682.1 μs | 13.33 KB |
| QR PNG (medium text) | 683.9 μs | 22.44 KB |
| QR PNG (long text) | 3,107.3 μs | 68.91 KB |
| QR SVG (medium text) | 768.4 μs | 18.91 KB |
| QR PNG High Error Correction | 1,080.5 μs | 36.59 KB |
| QR HTML (medium text) | 659.4 μs | 136.3 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 2,435.1 μs | 134.05 KB |
| QR Decode (clean, balanced) | 2,365.1 μs | 134.05 KB |
| QR Decode (clean, robust) | 2,176.2 μs | 134.05 KB |
| QR Decode (noisy, robust) | 4,844.7 μs | 1030.23 KB |
| QR Decode (screenshot, balanced) | 8,139.2 μs | 2054.05 KB |
| QR Decode (antialias, robust) | 558.0 μs | 146.34 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 353.33 μs | 832 B |
| QR Encode (medium text) | 911.53 μs | 1184 B |
| QR Encode (long text) | 1,909.57 μs | 2648 B |
| QR Render PNG (short, pre-encoded) | 193.93 μs | 12744 B |
| QR Render PNG (medium, pre-encoded) | 62.03 μs | 21720 B |
| QR Render PNG (long, pre-encoded) | 908.43 μs | 67848 B |
| QR Render Pixels (short, pre-encoded) | 37.40 μs | 122176 B |
| QR Render Pixels (medium, pre-encoded) | 56.47 μs | 198208 B |
| QR Render Pixels (long, pre-encoded) | 1,040.40 μs | 537920 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 220.1 μs<br>65.41 KB | 1,939.3 μs<br>289.63 KB<br>Δ 8.81 x / 4.43 x |  | 8,071.5 μs<br>998.83 KB<br>Δ 36.67 x / 15.27 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 164.1 μs<br>22.54 KB | 2,211.9 μs<br>371.95 KB<br>Δ 13.48 x / 16.5 x |  | 54,378.4 μs<br>2263.41 KB<br>Δ 331.37 x / 100.42 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 180.3 μs<br>14.13 KB | 1,771.1 μs<br>240.48 KB<br>Δ 9.82 x / 17.02 x |  | 43,291.1 μs<br>1676.85 KB<br>Δ 240.11 x / 118.67 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 174.5 μs<br>12.24 KB | 1,295.9 μs<br>239.91 KB<br>Δ 7.43 x / 19.6 x |  | 28,977.2 μs<br>1185.5 KB<br>Δ 166.06 x / 96.85 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 230.9 μs<br>19.6 KB | 2,211.2 μs<br>250.52 KB<br>Δ 9.58 x / 12.78 x |  | 10,329.6 μs<br>1001.25 KB<br>Δ 44.74 x / 51.08 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 193.4 μs<br>10.92 KB | 1,114.3 μs<br>239.88 KB<br>Δ 5.76 x / 21.97 x |  | 26,486.2 μs<br>951.05 KB<br>Δ 136.95 x / 87.09 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.491 ms<br>82.07 KB | 7.206 ms<br>1335.86 KB<br>Δ 4.83 x / 16.28 x |  | 56.081 ms<br>5616.22 KB<br>Δ 37.61 x / 68.43 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 750.7 μs<br>22.27 KB | 4,424.8 μs<br>435.62 KB<br>Δ 5.89 x / 19.56 x | 967.5 μs<br>16.34 KB<br>Δ 1.29 x / 0.73 x | 16,056.4 μs<br>1903.56 KB<br>Δ 21.39 x / 85.48 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.044 ms<br>134.05 KB | 1.951 ms<br>127.67 KB<br>Δ 0.95 x / 0.95 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 4.308 ms<br>1030.23 KB | 4.582 ms<br>706.89 KB<br>Δ 1.06 x / 0.69 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 247.0 μs<br>11.23 KB | 1,406.6 μs<br>239.95 KB<br>Δ 5.69 x / 21.37 x |  | 24,452.9 μs<br>866.11 KB<br>Δ 99 x / 77.12 x |
<!-- BENCHMARK:WINDOWS:QUICK:END -->

<!-- BENCHMARK:WINDOWS:FULL:START -->
## WINDOWS (Full)

Updated: 2026-01-24 09:37:48 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260124-091830
### How to read
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x (fastest) means CodeGlyphX is fastest; 1.5 x means ~50% slower.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the allocation of the fastest-time vendor for that scenario. Lower than 1 x means fewer allocations than the fastest-time vendor.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
- Benchmarks run under controlled, ideal conditions on a single machine; treat results as directional, not definitive.

### Notes
- Run mode: Full (BenchmarkDotNet default job settings).
- Comparisons target PNG output and include encode+render (not encode-only).
- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.
- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).
- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).
- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).
- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).
- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).

### Summary (Comparisons) - Full

| Benchmark | Scenario | Fastest | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Aztec (Encode) | Aztec PNG | CodeGlyphX 21.18 μs | 21.18 μs<br>61.34 KB | 556.01 μs<br>61.38 KB<br>Δ 26.25 x / 1 x |  | 1,337.96 μs<br>642.4 KB<br>Δ 63.17 x / 10.47 x | 1 x (fastest) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 10.76 μs | 10.76 μs<br>14.43 KB | 698.57 μs<br>15.7 KB<br>Δ 64.92 x / 1.09 x |  | 8,099.55 μs<br>2034.97 KB<br>Δ 752.75 x / 141.02 x | 1 x (fastest) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 8.167 μs | 8.167 μs<br>10.02 KB | 492.240 μs<br>12.23 KB<br>Δ 60.27 x / 1.22 x |  | 5,682.405 μs<br>1448.41 KB<br>Δ 695.78 x / 144.55 x | 1 x (fastest) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 6.548 μs | 6.548 μs<br>8.13 KB | 369.068 μs<br>11.66 KB<br>Δ 56.36 x / 1.43 x |  | 3,626.355 μs<br>957.22 KB<br>Δ 553.81 x / 117.74 x | 1 x (fastest) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 21.02 μs | 21.02 μs<br>15.52 KB | 532.46 μs<br>21.2 KB<br>Δ 25.33 x / 1.37 x |  | 1,414.52 μs<br>644.93 KB<br>Δ 67.29 x / 41.55 x | 1 x (fastest) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 5.243 μs | 5.243 μs<br>6.75 KB | 296.403 μs<br>11.63 KB<br>Δ 56.53 x / 1.72 x |  | 3,583.869 μs<br>850.63 KB<br>Δ 683.55 x / 126.02 x | 1 x (fastest) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 52.67 μs | 52.67 μs<br>49.9 KB | 3,079.46 μs<br>207.59 KB<br>Δ 58.47 x / 4.16 x |  | 10,174.70 μs<br>5003.67 KB<br>Δ 193.18 x / 100.27 x | 1 x (fastest) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 97.86 μs | 97.86 μs<br>14.19 KB | 1,202.59 μs<br>79.37 KB<br>Δ 12.29 x / 5.59 x | 244.56 μs<br>7.31 KB<br>Δ 2.5 x / 0.52 x | 3,024.08 μs<br>1545.71 KB<br>Δ 30.9 x / 108.93 x | 1 x (fastest) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 322.1 μs | 322.1 μs<br>5.27 KB | 469.7 μs<br>127.69 KB<br>Δ 1.46 x / 24.23 x |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 1.595 ms | 1.595 ms<br>5.45 KB | 1.943 ms<br>706.98 KB<br>Δ 1.22 x / 129.72 x |  |  | 1 x (fastest) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 5.766 μs | 5.766 μs<br>7.05 KB | 321.472 μs<br>11.7 KB<br>Δ 55.75 x / 1.66 x |  | 3,002.774 μs<br>765.7 KB<br>Δ 520.77 x / 108.61 x | 1 x (fastest) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 10.106 μs | 14.51 KB |
| Code 128 SVG | 2.411 μs | 17.61 KB |
| EAN PNG | 5.020 μs | 6.83 KB |
| Code 39 PNG | 8.402 μs | 10.09 KB |
| Code 93 PNG | 6.829 μs | 8.21 KB |
| UPC-A PNG | 5.720 μs | 7.13 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 12.155 μs | 10.16 KB |
| Data Matrix PNG (long) | 28.789 μs | 20.34 KB |
| Data Matrix SVG | 7.212 μs | 12.29 KB |
| PDF417 PNG | 49.370 μs | 49.99 KB |
| PDF417 SVG | 34.681 μs | 64.53 KB |
| Aztec PNG | 17.919 μs | 61.38 KB |
| Aztec SVG | 10.742 μs | 59.74 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 33.11 μs | 9.25 KB |
| QR PNG (medium text) | 69.80 μs | 14.36 KB |
| QR PNG (long text) | 550.71 μs | 36.8 KB |
| QR SVG (medium text) | 63.81 μs | 18.38 KB |
| QR PNG High Error Correction | 288.85 μs | 20.48 KB |
| QR HTML (medium text) | 109.28 μs | 135.78 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 325.0 μs | 5.27 KB |
| QR Decode (clean, balanced) | 380.2 μs | 5.27 KB |
| QR Decode (clean, robust) | 428.0 μs | 5.27 KB |
| QR Decode (noisy, robust) | 2,026.9 μs | 5.45 KB |
| QR Decode (screenshot, balanced) | 5,056.2 μs | 5.27 KB |
| QR Decode (antialias, robust) | 231.9 μs | 1.32 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 40.836 μs | 832 B |
| QR Encode (medium text) | 95.667 μs | 1184 B |
| QR Encode (long text) | 685.241 μs | 2648 B |
| QR Render PNG (short, pre-encoded) | 9.010 μs | 8568 B |
| QR Render PNG (medium, pre-encoded) | 14.474 μs | 13448 B |
| QR Render PNG (long, pre-encoded) | 35.875 μs | 34968 B |
| QR Render Pixels (short, pre-encoded) | 30.960 μs | 121154 B |
| QR Render Pixels (medium, pre-encoded) | 48.740 μs | 197202 B |
| QR Render Pixels (long, pre-encoded) | 150.844 μs | 535960 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 21.18 μs<br>61.34 KB | 556.01 μs<br>61.38 KB<br>Δ 26.25 x / 1 x |  | 1,337.96 μs<br>642.4 KB<br>Δ 63.17 x / 10.47 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 10.76 μs<br>14.43 KB | 698.57 μs<br>15.7 KB<br>Δ 64.92 x / 1.09 x |  | 8,099.55 μs<br>2034.97 KB<br>Δ 752.75 x / 141.02 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 8.167 μs<br>10.02 KB | 492.240 μs<br>12.23 KB<br>Δ 60.27 x / 1.22 x |  | 5,682.405 μs<br>1448.41 KB<br>Δ 695.78 x / 144.55 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 6.548 μs<br>8.13 KB | 369.068 μs<br>11.66 KB<br>Δ 56.36 x / 1.43 x |  | 3,626.355 μs<br>957.22 KB<br>Δ 553.81 x / 117.74 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 21.02 μs<br>15.52 KB | 532.46 μs<br>21.2 KB<br>Δ 25.33 x / 1.37 x |  | 1,414.52 μs<br>644.93 KB<br>Δ 67.29 x / 41.55 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 5.243 μs<br>6.75 KB | 296.403 μs<br>11.63 KB<br>Δ 56.53 x / 1.72 x |  | 3,583.869 μs<br>850.63 KB<br>Δ 683.55 x / 126.02 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 52.67 μs<br>49.9 KB | 3,079.46 μs<br>207.59 KB<br>Δ 58.47 x / 4.16 x |  | 10,174.70 μs<br>5003.67 KB<br>Δ 193.18 x / 100.27 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 97.86 μs<br>14.19 KB | 1,202.59 μs<br>79.37 KB<br>Δ 12.29 x / 5.59 x | 244.56 μs<br>7.31 KB<br>Δ 2.5 x / 0.52 x | 3,024.08 μs<br>1545.71 KB<br>Δ 30.9 x / 108.93 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 322.1 μs<br>5.27 KB | 469.7 μs<br>127.69 KB<br>Δ 1.46 x / 24.23 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 1.595 ms<br>5.45 KB | 1.943 ms<br>706.98 KB<br>Δ 1.22 x / 129.72 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 5.766 μs<br>7.05 KB | 321.472 μs<br>11.7 KB<br>Δ 55.75 x / 1.66 x |  | 3,002.774 μs<br>765.7 KB<br>Δ 520.77 x / 108.61 x |
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
