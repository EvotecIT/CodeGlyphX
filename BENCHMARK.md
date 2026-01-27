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

Updated: 2026-01-24 11:41:46 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260124-124033
### How to read
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. If CodeGlyphX is fastest, the text shows the lead vs the runner-up; otherwise it shows the lag vs the fastest vendor.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the allocation of the fastest-time vendor for that scenario. Lower than 1 x means fewer allocations than the fastest-time vendor.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
- Benchmarks run under controlled, ideal conditions on a single machine; treat results as directional, not definitive.

### Notes
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 218.2 μs | 218.2 μs<br>61.34 KB | 1,424.1 μs<br>61.42 KB<br>Δ 6.53 x / 1 x |  | 5,655.5 μs<br>642.6 KB<br>Δ 25.92 x / 10.48 x | 1 x (fastest, lead 6.53 x vs ZXing.Net) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 145.7 μs | 145.7 μs<br>14.43 KB | 1,392.4 μs<br>15.74 KB<br>Δ 9.56 x / 1.09 x |  | 42,576.1 μs<br>2035.01 KB<br>Δ 292.22 x / 141.03 x | 1 x (fastest, lead 9.56 x vs ZXing.Net) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 124.9 μs | 124.9 μs<br>10.02 KB | 1,302.8 μs<br>12.28 KB<br>Δ 10.43 x / 1.23 x |  | 30,094.8 μs<br>1448.45 KB<br>Δ 240.95 x / 144.56 x | 1 x (fastest, lead 10.43 x vs ZXing.Net) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 124.9 μs | 124.9 μs<br>8.13 KB | 1,057.5 μs<br>11.7 KB<br>Δ 8.47 x / 1.44 x |  | 20,290.2 μs<br>957.26 KB<br>Δ 162.45 x / 117.74 x | 1 x (fastest, lead 8.47 x vs ZXing.Net) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 209.4 μs | 209.4 μs<br>15.52 KB | 1,514.8 μs<br>22.31 KB<br>Δ 7.23 x / 1.44 x |  | 6,275.2 μs<br>645.05 KB<br>Δ 29.97 x / 41.56 x | 1 x (fastest, lead 7.23 x vs ZXing.Net) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 118.6 μs | 118.6 μs<br>6.84 KB | 901.5 μs<br>11.68 KB<br>Δ 7.6 x / 1.71 x |  | 18,340.9 μs<br>850.83 KB<br>Δ 154.65 x / 124.39 x | 1 x (fastest, lead 7.6 x vs ZXing.Net) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.040 ms | 1.040 ms<br>49.9 KB | 5.040 ms<br>207.63 KB<br>Δ 4.85 x / 4.16 x |  | 39.927 ms<br>5004.03 KB<br>Δ 38.39 x / 100.28 x | 1 x (fastest, lead 4.85 x vs ZXing.Net) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 527.1 μs | 527.1 μs<br>14.19 KB | 3,065.0 μs<br>79.41 KB<br>Δ 5.81 x / 5.6 x | 823.6 μs<br>7.31 KB<br>Δ 1.56 x / 0.52 x | 11,560.2 μs<br>1547.36 KB<br>Δ 21.93 x / 109.05 x | 1 x (fastest, lead 1.56 x vs QRCoder) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 1.244 ms | 1.244 ms<br>4.87 KB | 1.513 ms<br>127.67 KB<br>Δ 1.22 x / 26.22 x |  |  | 1 x (fastest, lead 1.22 x vs ZXing.Net) | 1 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 2.921 ms | 2.921 ms<br>5.05 KB | 3.237 ms<br>706.89 KB<br>Δ 1.11 x / 139.98 x |  |  | 1 x (fastest, lead 1.11 x vs ZXing.Net) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 128.8 μs | 128.8 μs<br>7.15 KB | 880.7 μs<br>11.75 KB<br>Δ 6.84 x / 1.64 x |  | 16,407.3 μs<br>765.73 KB<br>Δ 127.39 x / 107.1 x | 1 x (fastest, lead 6.84 x vs ZXing.Net) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 142.40 μs | 14.51 KB |
| Code 128 SVG | 36.07 μs | 17.61 KB |
| EAN PNG | 143.90 μs | 6.92 KB |
| Code 39 PNG | 172.50 μs | 10.09 KB |
| Code 93 PNG | 224.43 μs | 8.21 KB |
| UPC-A PNG | 157.27 μs | 7.23 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 176.93 μs | 10.16 KB |
| Data Matrix PNG (long) | 469.67 μs | 20.34 KB |
| Data Matrix SVG | 62.50 μs | 12.29 KB |
| PDF417 PNG | 1,219.77 μs | 49.99 KB |
| PDF417 SVG | 2,418.80 μs | 64.53 KB |
| Aztec PNG | 363.53 μs | 61.38 KB |
| Aztec SVG | 126.73 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 490.2 μs | 9.25 KB |
| QR PNG (medium text) | 618.5 μs | 14.36 KB |
| QR PNG (long text) | 2,628.7 μs | 36.8 KB |
| QR SVG (medium text) | 730.7 μs | 18.38 KB |
| QR PNG High Error Correction | 1,047.3 μs | 20.48 KB |
| QR HTML (medium text) | 725.2 μs | 135.78 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 1,278.0 μs | 4.87 KB |
| QR Decode (clean, balanced) | 1,594.6 μs | 4.87 KB |
| QR Decode (clean, robust) | 1,603.9 μs | 4.87 KB |
| QR Decode (noisy, robust) | 3,233.2 μs | 5.05 KB |
| QR Decode (screenshot, balanced) | 6,821.4 μs | 4.87 KB |
| QR Decode (antialias, robust) | 370.9 μs | 1.09 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 327.70 μs | 832 B |
| QR Encode (medium text) | 769.55 μs | 1184 B |
| QR Encode (long text) | 2,032.33 μs | 2648 B |
| QR Render PNG (short, pre-encoded) | 157.80 μs | 8568 B |
| QR Render PNG (medium, pre-encoded) | 56.10 μs | 13448 B |
| QR Render PNG (long, pre-encoded) | 964.43 μs | 34968 B |
| QR Render Pixels (short, pre-encoded) | 34.67 μs | 121128 B |
| QR Render Pixels (medium, pre-encoded) | 34.50 μs | 197160 B |
| QR Render Pixels (long, pre-encoded) | 967.72 μs | 535848 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 218.2 μs<br>61.34 KB | 1,424.1 μs<br>61.42 KB<br>Δ 6.53 x / 1 x |  | 5,655.5 μs<br>642.6 KB<br>Δ 25.92 x / 10.48 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 145.7 μs<br>14.43 KB | 1,392.4 μs<br>15.74 KB<br>Δ 9.56 x / 1.09 x |  | 42,576.1 μs<br>2035.01 KB<br>Δ 292.22 x / 141.03 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 124.9 μs<br>10.02 KB | 1,302.8 μs<br>12.28 KB<br>Δ 10.43 x / 1.23 x |  | 30,094.8 μs<br>1448.45 KB<br>Δ 240.95 x / 144.56 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 124.9 μs<br>8.13 KB | 1,057.5 μs<br>11.7 KB<br>Δ 8.47 x / 1.44 x |  | 20,290.2 μs<br>957.26 KB<br>Δ 162.45 x / 117.74 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 209.4 μs<br>15.52 KB | 1,514.8 μs<br>22.31 KB<br>Δ 7.23 x / 1.44 x |  | 6,275.2 μs<br>645.05 KB<br>Δ 29.97 x / 41.56 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 118.6 μs<br>6.84 KB | 901.5 μs<br>11.68 KB<br>Δ 7.6 x / 1.71 x |  | 18,340.9 μs<br>850.83 KB<br>Δ 154.65 x / 124.39 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.040 ms<br>49.9 KB | 5.040 ms<br>207.63 KB<br>Δ 4.85 x / 4.16 x |  | 39.927 ms<br>5004.03 KB<br>Δ 38.39 x / 100.28 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 527.1 μs<br>14.19 KB | 3,065.0 μs<br>79.41 KB<br>Δ 5.81 x / 5.6 x | 823.6 μs<br>7.31 KB<br>Δ 1.56 x / 0.52 x | 11,560.2 μs<br>1547.36 KB<br>Δ 21.93 x / 109.05 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 1.244 ms<br>4.87 KB | 1.513 ms<br>127.67 KB<br>Δ 1.22 x / 26.22 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 2.921 ms<br>5.05 KB | 3.237 ms<br>706.89 KB<br>Δ 1.11 x / 139.98 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 128.8 μs<br>7.15 KB | 880.7 μs<br>11.75 KB<br>Δ 6.84 x / 1.64 x |  | 16,407.3 μs<br>765.73 KB<br>Δ 127.39 x / 107.1 x |
<!-- BENCHMARK:WINDOWS:QUICK:END -->

<!-- BENCHMARK:WINDOWS:FULL:START -->
## WINDOWS (Full)

Updated: 2026-01-24 10:49:40 UTC
Framework: net8.0
Configuration: Release
Artifacts: Build/BenchmarkResults/windows-20260124-091830
### How to read
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. If CodeGlyphX is fastest, the text shows the lead vs the runner-up; otherwise it shows the lag vs the fastest vendor.
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 21.18 μs | 21.18 μs<br>61.34 KB | 556.01 μs<br>61.38 KB<br>Δ 26.25 x / 1 x |  | 1,337.96 μs<br>642.4 KB<br>Δ 63.17 x / 10.47 x | 1 x (fastest, lead 26.25 x vs ZXing.Net) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 10.76 μs | 10.76 μs<br>14.43 KB | 698.57 μs<br>15.7 KB<br>Δ 64.92 x / 1.09 x |  | 8,099.55 μs<br>2034.97 KB<br>Δ 752.75 x / 141.02 x | 1 x (fastest, lead 64.92 x vs ZXing.Net) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 8.167 μs | 8.167 μs<br>10.02 KB | 492.240 μs<br>12.23 KB<br>Δ 60.27 x / 1.22 x |  | 5,682.405 μs<br>1448.41 KB<br>Δ 695.78 x / 144.55 x | 1 x (fastest, lead 60.27 x vs ZXing.Net) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 6.548 μs | 6.548 μs<br>8.13 KB | 369.068 μs<br>11.66 KB<br>Δ 56.36 x / 1.43 x |  | 3,626.355 μs<br>957.22 KB<br>Δ 553.81 x / 117.74 x | 1 x (fastest, lead 56.36 x vs ZXing.Net) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 21.02 μs | 21.02 μs<br>15.52 KB | 532.46 μs<br>21.2 KB<br>Δ 25.33 x / 1.37 x |  | 1,414.52 μs<br>644.93 KB<br>Δ 67.29 x / 41.55 x | 1 x (fastest, lead 25.33 x vs ZXing.Net) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 5.243 μs | 5.243 μs<br>6.75 KB | 296.403 μs<br>11.63 KB<br>Δ 56.53 x / 1.72 x |  | 3,583.869 μs<br>850.63 KB<br>Δ 683.55 x / 126.02 x | 1 x (fastest, lead 56.53 x vs ZXing.Net) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 52.67 μs | 52.67 μs<br>49.9 KB | 3,079.46 μs<br>207.59 KB<br>Δ 58.47 x / 4.16 x |  | 10,174.70 μs<br>5003.67 KB<br>Δ 193.18 x / 100.27 x | 1 x (fastest, lead 58.47 x vs ZXing.Net) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 97.86 μs | 97.86 μs<br>14.19 KB | 1,202.59 μs<br>79.37 KB<br>Δ 12.29 x / 5.59 x | 244.56 μs<br>7.31 KB<br>Δ 2.5 x / 0.52 x | 3,024.08 μs<br>1545.71 KB<br>Δ 30.9 x / 108.93 x | 1 x (fastest, lead 2.5 x vs QRCoder) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 322.1 μs | 322.1 μs<br>5.27 KB | 469.7 μs<br>127.69 KB<br>Δ 1.46 x / 24.23 x |  |  | 1 x (fastest, lead 1.46 x vs ZXing.Net) | 1 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 1.595 ms | 1.595 ms<br>5.45 KB | 1.943 ms<br>706.98 KB<br>Δ 1.22 x / 129.72 x |  |  | 1 x (fastest, lead 1.22 x vs ZXing.Net) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 5.766 μs | 5.766 μs<br>7.05 KB | 321.472 μs<br>11.7 KB<br>Δ 55.75 x / 1.66 x |  | 3,002.774 μs<br>765.7 KB<br>Δ 520.77 x / 108.61 x | 1 x (fastest, lead 55.75 x vs ZXing.Net) | 1 x | good |

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
## LINUX (Quick)

Updated: 2026-01-27 14:16:09 UTC
Framework: net8.0
Configuration: Release
Artifacts: /tmp/cgx-psquick-smoke/linux-20260127-151049
### How to read
- Mean: average time per operation. Lower is better.
- Allocated: managed memory allocated per operation. Lower is better.
- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. If CodeGlyphX is fastest, the text shows the lead vs the runner-up; otherwise it shows the lag vs the fastest vendor.
- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the allocation of the fastest-time vendor for that scenario. Lower than 1 x means fewer allocations than the fastest-time vendor.
- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).
- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).
- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.
- Quick and Full runs include the same scenario list; only the iteration settings differ.
- Benchmarks run under controlled, ideal conditions on a single machine; treat results as directional, not definitive.

### Notes
- Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1).
- QR pack runner (quick): CodeGlyphX expected=98 % (misses: art-jess3-grid)
- Quick runs include the same scenario set as Full runs; run time is driven by iteration counts.
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 270.8 μs | 270.8 μs<br>61.34 KB |  |  |  | 1 x (fastest) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 150.3 μs | 150.3 μs<br>14.44 KB |  |  |  | 1 x (fastest) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 128.0 μs | 128.0 μs<br>10.02 KB |  |  |  | 1 x (fastest) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 144.5 μs | 144.5 μs<br>8.14 KB |  |  |  | 1 x (fastest) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 200.9 μs | 200.9 μs<br>15.53 KB |  |  |  | 1 x (fastest) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 168.9 μs | 168.9 μs<br>6.85 KB |  |  |  | 1 x (fastest) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 907.9 μs | 907.9 μs<br>49.91 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 614.1 μs | 614.1 μs<br>14.27 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 956.5 μs | 956.5 μs<br>3.91 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 41.20 ms | 41.20 ms<br>160.77 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Stress) | QR Decode (fancy) | CodeGlyphX 353.4 μs | 353.4 μs<br>2.68 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Stress) | QR Decode (no quiet zone) | CodeGlyphX 11,185.1 μs | 11,185.1 μs<br>45.09 KB |  |  |  | 1 x (fastest) | 1 x | good |
| QR Decode (Stress) | QR Decode (resampled) | CodeGlyphX 557.4 μs | 557.4 μs<br>1.88 KB |  |  |  | 1 x (fastest) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 197.3 μs | 197.3 μs<br>7.16 KB |  |  |  | 1 x (fastest) | 1 x | good |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 270.8 μs<br>61.34 KB |  |  |  |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 150.3 μs<br>14.44 KB |  |  |  |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 128.0 μs<br>10.02 KB |  |  |  |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 144.5 μs<br>8.14 KB |  |  |  |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 200.9 μs<br>15.53 KB |  |  |  |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 168.9 μs<br>6.85 KB |  |  |  |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 907.9 μs<br>49.91 KB |  |  |  |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 614.1 μs<br>14.27 KB |  |  |  |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 956.5 μs<br>3.91 KB |  |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 41.20 ms<br>160.77 KB |  |  |  |

#### QR Decode (Stress)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (fancy) | 353.4 μs<br>2.68 KB |  |  |  |
| QR Decode (no quiet zone) | 11,185.1 μs<br>45.09 KB |  |  |  |
| QR Decode (resampled) | 557.4 μs<br>1.88 KB |  |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 197.3 μs<br>7.16 KB |  |  |  |
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
