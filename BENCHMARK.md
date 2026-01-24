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

Updated: 2026-01-24 10:49:32 UTC
Framework: net8.0
Configuration: Release
Artifacts: Build/BenchmarkResults/windows-20260124-111753
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 222.1 μs | 222.1 μs<br>61.34 KB | 1,533.6 μs<br>61.42 KB<br>Δ 6.9 x / 1 x |  | 7,519.3 μs<br>642.58 KB<br>Δ 33.86 x / 10.48 x | 1 x (fastest, lead 6.9 x vs ZXing.Net) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 173.3 μs | 173.3 μs<br>14.43 KB | 1,794.0 μs<br>15.74 KB<br>Δ 10.35 x / 1.09 x |  | 50,197.1 μs<br>2035.16 KB<br>Δ 289.65 x / 141.04 x | 1 x (fastest, lead 10.35 x vs ZXing.Net) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 172.6 μs | 172.6 μs<br>10.02 KB | 1,667.8 μs<br>12.28 KB<br>Δ 9.66 x / 1.23 x |  | 38,262.5 μs<br>1448.61 KB<br>Δ 221.68 x / 144.57 x | 1 x (fastest, lead 9.66 x vs ZXing.Net) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 159.2 μs | 159.2 μs<br>8.13 KB | 1,450.8 μs<br>11.7 KB<br>Δ 9.11 x / 1.44 x |  | 22,938.5 μs<br>957.41 KB<br>Δ 144.09 x / 117.76 x | 1 x (fastest, lead 9.11 x vs ZXing.Net) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 226.8 μs | 226.8 μs<br>15.52 KB | 1,703.4 μs<br>22.31 KB<br>Δ 7.51 x / 1.44 x |  | 8,912.7 μs<br>645.05 KB<br>Δ 39.3 x / 41.56 x | 1 x (fastest, lead 7.51 x vs ZXing.Net) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 199.0 μs | 199.0 μs<br>6.84 KB | 1,030.8 μs<br>11.68 KB<br>Δ 5.18 x / 1.71 x |  | 21,580.0 μs<br>850.67 KB<br>Δ 108.44 x / 124.37 x | 1 x (fastest, lead 5.18 x vs ZXing.Net) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.150 ms | 1.150 ms<br>49.9 KB | 6.801 ms<br>207.63 KB<br>Δ 5.91 x / 4.16 x |  | 57.925 ms<br>5003.86 KB<br>Δ 50.37 x / 100.28 x | 1 x (fastest, lead 5.91 x vs ZXing.Net) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 687.7 μs | 687.7 μs<br>14.19 KB | 4,108.0 μs<br>79.41 KB<br>Δ 5.97 x / 5.6 x | 971.9 μs<br>7.31 KB<br>Δ 1.41 x / 0.52 x | 14,996.5 μs<br>1547.36 KB<br>Δ 21.81 x / 109.05 x | 1 x (fastest, lead 1.41 x vs QRCoder) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.993 ms | 1.998 ms<br>5.27 KB | 1.993 ms<br>127.67 KB<br>Δ 1 x / 24.23 x |  |  | 1 x (lag vs ZXing.Net) | 0.04 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | CodeGlyphX 4.189 ms | 4.189 ms<br>5.45 KB | 4.413 ms<br>706.89 KB<br>Δ 1.05 x / 129.7 x |  |  | 1 x (fastest, lead 1.05 x vs ZXing.Net) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 167.1 μs | 167.1 μs<br>7.15 KB | 1,330.5 μs<br>11.75 KB<br>Δ 7.96 x / 1.64 x |  | 22,742.3 μs<br>765.73 KB<br>Δ 136.1 x / 107.1 x | 1 x (fastest, lead 7.96 x vs ZXing.Net) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 136.27 μs | 14.51 KB |
| Code 128 SVG | 27.80 μs | 17.61 KB |
| EAN PNG | 135.10 μs | 6.92 KB |
| Code 39 PNG | 169.07 μs | 10.09 KB |
| Code 93 PNG | 147.00 μs | 8.21 KB |
| UPC-A PNG | 162.63 μs | 7.23 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 184.37 μs | 10.16 KB |
| Data Matrix PNG (long) | 415.73 μs | 20.34 KB |
| Data Matrix SVG | 48.70 μs | 12.29 KB |
| PDF417 PNG | 1,149.90 μs | 49.99 KB |
| PDF417 SVG | 2,896.63 μs | 64.53 KB |
| Aztec PNG | 237.67 μs | 61.38 KB |
| Aztec SVG | 92.63 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 526.3 μs | 9.25 KB |
| QR PNG (medium text) | 636.7 μs | 14.36 KB |
| QR PNG (long text) | 2,723.6 μs | 36.8 KB |
| QR SVG (medium text) | 721.7 μs | 18.38 KB |
| QR PNG High Error Correction | 874.9 μs | 20.48 KB |
| QR HTML (medium text) | 643.0 μs | 135.78 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 1,527.7 μs | 5.27 KB |
| QR Decode (clean, balanced) | 1,560.8 μs | 5.27 KB |
| QR Decode (clean, robust) | 1,619.0 μs | 5.27 KB |
| QR Decode (noisy, robust) | 3,512.5 μs | 5.45 KB |
| QR Decode (screenshot, balanced) | 5,802.7 μs | 5.27 KB |
| QR Decode (antialias, robust) | 307.3 μs | 1.32 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 375.63 μs | 832 B |
| QR Encode (medium text) | 687.40 μs | 1184 B |
| QR Encode (long text) | 1,655.50 μs | 2648 B |
| QR Render PNG (short, pre-encoded) | 146.67 μs | 8568 B |
| QR Render PNG (medium, pre-encoded) | 52.33 μs | 13448 B |
| QR Render PNG (long, pre-encoded) | 798.27 μs | 34968 B |
| QR Render Pixels (short, pre-encoded) | 29.23 μs | 121128 B |
| QR Render Pixels (medium, pre-encoded) | 44.50 μs | 197160 B |
| QR Render Pixels (long, pre-encoded) | 967.80 μs | 535848 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 222.1 μs<br>61.34 KB | 1,533.6 μs<br>61.42 KB<br>Δ 6.9 x / 1 x |  | 7,519.3 μs<br>642.58 KB<br>Δ 33.86 x / 10.48 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 173.3 μs<br>14.43 KB | 1,794.0 μs<br>15.74 KB<br>Δ 10.35 x / 1.09 x |  | 50,197.1 μs<br>2035.16 KB<br>Δ 289.65 x / 141.04 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 172.6 μs<br>10.02 KB | 1,667.8 μs<br>12.28 KB<br>Δ 9.66 x / 1.23 x |  | 38,262.5 μs<br>1448.61 KB<br>Δ 221.68 x / 144.57 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 159.2 μs<br>8.13 KB | 1,450.8 μs<br>11.7 KB<br>Δ 9.11 x / 1.44 x |  | 22,938.5 μs<br>957.41 KB<br>Δ 144.09 x / 117.76 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 226.8 μs<br>15.52 KB | 1,703.4 μs<br>22.31 KB<br>Δ 7.51 x / 1.44 x |  | 8,912.7 μs<br>645.05 KB<br>Δ 39.3 x / 41.56 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 199.0 μs<br>6.84 KB | 1,030.8 μs<br>11.68 KB<br>Δ 5.18 x / 1.71 x |  | 21,580.0 μs<br>850.67 KB<br>Δ 108.44 x / 124.37 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.150 ms<br>49.9 KB | 6.801 ms<br>207.63 KB<br>Δ 5.91 x / 4.16 x |  | 57.925 ms<br>5003.86 KB<br>Δ 50.37 x / 100.28 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 687.7 μs<br>14.19 KB | 4,108.0 μs<br>79.41 KB<br>Δ 5.97 x / 5.6 x | 971.9 μs<br>7.31 KB<br>Δ 1.41 x / 0.52 x | 14,996.5 μs<br>1547.36 KB<br>Δ 21.81 x / 109.05 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 1.998 ms<br>5.27 KB | 1.993 ms<br>127.67 KB<br>Δ 1 x / 24.23 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 4.189 ms<br>5.45 KB | 4.413 ms<br>706.89 KB<br>Δ 1.05 x / 129.7 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 167.1 μs<br>7.15 KB | 1,330.5 μs<br>11.75 KB<br>Δ 7.96 x / 1.64 x |  | 22,742.3 μs<br>765.73 KB<br>Δ 136.1 x / 107.1 x |
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
