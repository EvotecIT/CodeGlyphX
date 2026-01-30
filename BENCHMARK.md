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

Updated: 2026-01-30 09:22:51 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260130-101914
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
- QR pack runner (quick): CodeGlyphX expected=98% (misses: art-jess3-grid); ZXing.Net expected=62% (misses: art-dots-variants, art-jess3-grid, art-jess3-splash, art-jess3-splash-variant)
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 235.3 μs | 235.3 μs<br>61.39 KB | 1,489.9 μs<br>61.42 KB<br>Δ 6.33 x / 1 x |  | 6,423.4 μs<br>642.64 KB<br>Δ 27.3 x / 10.47 x | 1 x (fastest, lead 6.33 x vs ZXing.Net) | 1 x | good |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 136.0 μs | 136.0 μs<br>14.48 KB | 1,542.0 μs<br>15.74 KB<br>Δ 11.34 x / 1.09 x |  | 54,673.4 μs<br>2035.16 KB<br>Δ 402.01 x / 140.55 x | 1 x (fastest, lead 11.34 x vs ZXing.Net) | 1 x | good |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 146.0 μs | 146.0 μs<br>10.07 KB | 1,624.1 μs<br>12.28 KB<br>Δ 11.12 x / 1.22 x |  | 41,847.8 μs<br>1448.45 KB<br>Δ 286.63 x / 143.84 x | 1 x (fastest, lead 11.12 x vs ZXing.Net) | 1 x | good |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 137.0 μs | 137.0 μs<br>8.19 KB | 1,342.3 μs<br>11.7 KB<br>Δ 9.8 x / 1.43 x |  | 23,990.6 μs<br>957.41 KB<br>Δ 175.11 x / 116.9 x | 1 x (fastest, lead 9.8 x vs ZXing.Net) | 1 x | good |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 179.0 μs | 179.0 μs<br>15.58 KB | 1,742.2 μs<br>22.31 KB<br>Δ 9.73 x / 1.43 x |  | 7,767.3 μs<br>645.2 KB<br>Δ 43.39 x / 41.41 x | 1 x (fastest, lead 9.73 x vs ZXing.Net) | 1 x | good |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 141.8 μs | 141.8 μs<br>6.9 KB | 1,259.9 μs<br>11.68 KB<br>Δ 8.89 x / 1.69 x |  | 19,383.1 μs<br>850.83 KB<br>Δ 136.69 x / 123.31 x | 1 x (fastest, lead 8.89 x vs ZXing.Net) | 1 x | good |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.071 ms | 1.071 ms<br>49.95 KB | 6.837 ms<br>207.63 KB<br>Δ 6.38 x / 4.16 x |  | 40.422 ms<br>5004.02 KB<br>Δ 37.74 x / 100.18 x | 1 x (fastest, lead 6.38 x vs ZXing.Net) | 1 x | good |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 541.5 μs | 541.5 μs<br>14.88 KB | 4,175.6 μs<br>79.41 KB<br>Δ 7.71 x / 5.34 x | 898.6 μs<br>7.31 KB<br>Δ 1.66 x / 0.49 x | 13,638.9 μs<br>1547.52 KB<br>Δ 25.19 x / 104 x | 1 x (fastest, lead 1.66 x vs QRCoder) | 1 x | good |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 1.256 ms | 1.256 ms<br>3.91 KB | 1.502 ms<br>127.67 KB<br>Δ 1.2 x / 32.65 x |  |  | 1 x (fastest, lead 1.2 x vs ZXing.Net) | 1 x | good |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.394 ms | 48.186 ms<br>160.77 KB | 3.394 ms<br>706.89 KB<br>Δ 0.07 x / 4.4 x |  |  | 14.2 x (lag vs ZXing.Net) | 0.23 x | bad |
| QR Decode (Stress) | QR Decode (fancy) | CodeGlyphX 349.8 μs | 349.8 μs<br>2.68 KB | 673.2 μs<br>63.25 KB<br>Δ 1.92 x / 23.6 x |  |  | 1 x (fastest, lead 1.92 x vs ZXing.Net) | 1 x | good |
| QR Decode (Stress) | QR Decode (no quiet zone) | ZXing.Net 954.0 μs | 11,920.2 μs<br>45.09 KB | 954.0 μs<br>65.78 KB<br>Δ 0.08 x / 1.46 x |  |  | 12.49 x (lag vs ZXing.Net) | 0.69 x | bad |
| QR Decode (Stress) | QR Decode (resampled) | CodeGlyphX 643.9 μs | 643.9 μs<br>1.88 KB | 729.0 μs<br>135.23 KB<br>Δ 1.13 x / 71.93 x |  |  | 1 x (fastest, lead 1.13 x vs ZXing.Net) | 1 x | good |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 130.3 μs | 130.3 μs<br>7.2 KB | 1,166.1 μs<br>11.75 KB<br>Δ 8.95 x / 1.63 x |  | 19,200.6 μs<br>765.91 KB<br>Δ 147.36 x / 106.38 x | 1 x (fastest, lead 8.95 x vs ZXing.Net) | 1 x | good |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 138.83 μs | 14.57 KB |
| Code 128 SVG | 26.57 μs | 20.56 KB |
| EAN PNG | 143.73 μs | 6.98 KB |
| Code 39 PNG | 147.97 μs | 10.16 KB |
| Code 93 PNG | 147.60 μs | 8.27 KB |
| UPC-A PNG | 158.67 μs | 7.29 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 167.77 μs | 10.23 KB |
| Data Matrix PNG (long) | 377.72 μs | 20.4 KB |
| Data Matrix SVG | 58.37 μs | 14.38 KB |
| PDF417 PNG | 928.12 μs | 50.05 KB |
| PDF417 SVG | 2,967.70 μs | 74.66 KB |
| Aztec PNG | 200.83 μs | 61.45 KB |
| Aztec SVG | 93.60 μs | 61.47 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 462.3 μs | 9.77 KB |
| QR PNG (medium text) | 601.4 μs | 14.88 KB |
| QR PNG (long text) | 2,521.8 μs | 37.32 KB |
| QR SVG (medium text) | 697.1 μs | 21.83 KB |
| QR PNG High Error Correction | 825.8 μs | 21.27 KB |
| QR PNG (medium text, logo) | 5,000.1 μs | 1099.84 KB |
| QR PNG (medium text, fancy) | 1,857.3 μs | 388.58 KB |
| QR HTML (medium text) | 695.7 μs | 169.2 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 957.1 μs | 3.91 KB |
| QR Decode (clean, balanced) | 1,013.3 μs | 3.91 KB |
| QR Decode (clean, robust) | 124,875.7 μs | 737.06 KB |
| QR Decode (noisy, robust) | 41,662.5 μs | 160.77 KB |
| QR Decode (screenshot, balanced) | 7,076.9 μs | 3.91 KB |
| QR Decode (antialias, robust) | 1,714.4 μs | 173.73 KB |
| QR Decode (fancy, robust) | 563.2 μs | 2.49 KB |
| QR Decode (resampled, balanced) | 679.7 μs | 1.88 KB |
| QR Decode (no quiet zone, robust) | 122,674.2 μs | 787.41 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 320.20 μs | 936 B |
| QR Encode (medium text) | 793.20 μs | 1288 B |
| QR Encode (long text) | 1,350.70 μs | 2752 B |
| QR Render PNG (short, pre-encoded) | 140.20 μs | 8568 B |
| QR Render PNG (medium, pre-encoded) | 57.13 μs | 13448 B |
| QR Render PNG (long, pre-encoded) | 676.63 μs | 34968 B |
| QR Render Pixels (short, pre-encoded) | 23.47 μs | 121128 B |
| QR Render Pixels (medium, pre-encoded) | 36.85 μs | 197160 B |
| QR Render Pixels (long, pre-encoded) | 813.60 μs | 535848 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 235.3 μs<br>61.39 KB | 1,489.9 μs<br>61.42 KB<br>Δ 6.33 x / 1 x |  | 6,423.4 μs<br>642.64 KB<br>Δ 27.3 x / 10.47 x |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 136.0 μs<br>14.48 KB | 1,542.0 μs<br>15.74 KB<br>Δ 11.34 x / 1.09 x |  | 54,673.4 μs<br>2035.16 KB<br>Δ 402.01 x / 140.55 x |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 146.0 μs<br>10.07 KB | 1,624.1 μs<br>12.28 KB<br>Δ 11.12 x / 1.22 x |  | 41,847.8 μs<br>1448.45 KB<br>Δ 286.63 x / 143.84 x |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 137.0 μs<br>8.19 KB | 1,342.3 μs<br>11.7 KB<br>Δ 9.8 x / 1.43 x |  | 23,990.6 μs<br>957.41 KB<br>Δ 175.11 x / 116.9 x |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 179.0 μs<br>15.58 KB | 1,742.2 μs<br>22.31 KB<br>Δ 9.73 x / 1.43 x |  | 7,767.3 μs<br>645.2 KB<br>Δ 43.39 x / 41.41 x |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 141.8 μs<br>6.9 KB | 1,259.9 μs<br>11.68 KB<br>Δ 8.89 x / 1.69 x |  | 19,383.1 μs<br>850.83 KB<br>Δ 136.69 x / 123.31 x |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.071 ms<br>49.95 KB | 6.837 ms<br>207.63 KB<br>Δ 6.38 x / 4.16 x |  | 40.422 ms<br>5004.02 KB<br>Δ 37.74 x / 100.18 x |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 541.5 μs<br>14.88 KB | 4,175.6 μs<br>79.41 KB<br>Δ 7.71 x / 5.34 x | 898.6 μs<br>7.31 KB<br>Δ 1.66 x / 0.49 x | 13,638.9 μs<br>1547.52 KB<br>Δ 25.19 x / 104 x |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 1.256 ms<br>3.91 KB | 1.502 ms<br>127.67 KB<br>Δ 1.2 x / 32.65 x |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 48.186 ms<br>160.77 KB | 3.394 ms<br>706.89 KB<br>Δ 0.07 x / 4.4 x |  |  |

#### QR Decode (Stress)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (fancy) | 349.8 μs<br>2.68 KB | 673.2 μs<br>63.25 KB<br>Δ 1.92 x / 23.6 x |  |  |
| QR Decode (no quiet zone) | 11,920.2 μs<br>45.09 KB | 954.0 μs<br>65.78 KB<br>Δ 0.08 x / 1.46 x |  |  |
| QR Decode (resampled) | 643.9 μs<br>1.88 KB | 729.0 μs<br>135.23 KB<br>Δ 1.13 x / 71.93 x |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 130.3 μs<br>7.2 KB | 1,166.1 μs<br>11.75 KB<br>Δ 8.95 x / 1.63 x |  | 19,200.6 μs<br>765.91 KB<br>Δ 147.36 x / 106.38 x |
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

Updated: 2026-01-30 09:17:05 UTC
Framework: net8.0
Configuration: Release
Artifacts: /mnt/c/Support/GitHub/CodeMatrix/Build/BenchmarkResults/linux-20260130-101027
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 219.9 μs | 1.0 x | 1.0 x | good | 219.9 μs | 61.39 KB |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 136.9 μs | 1.0 x | 1.0 x | good | 136.9 μs | 14.48 KB |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 167.4 μs | 1.0 x | 1.0 x | good | 167.4 μs | 10.07 KB |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 144.8 μs | 1.0 x | 1.0 x | good | 144.8 μs | 8.19 KB |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 219.7 μs | 1.0 x | 1.0 x | good | 219.7 μs | 15.58 KB |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 146.7 μs | 1.0 x | 1.0 x | good | 146.7 μs | 6.8 KB |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.000 ms | 1.0 x | 1.0 x | good | 1.000 ms | 49.95 KB |
| QR (Encode) | QR PNG (medium) | CodeGlyphX 565.8 μs | 1.0 x | 1.0 x | good | 565.8 μs | 14.88 KB |
| QR Decode (Clean) | QR Decode (clean) | CodeGlyphX 974.3 μs | 1.0 x | 1.0 x | good | 974.3 μs | 3.91 KB |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.164 ms | 12.63 x | 0.23 x | bad | 39.963 ms | 160.77 KB |
| QR Decode (Stress) | QR Decode (fancy) | CodeGlyphX 367.9 μs | 1.0 x | 1.0 x | good | 367.9 μs | 2.68 KB |
| QR Decode (Stress) | QR Decode (no quiet zone) | ZXing.Net 921.7 μs | 11.01 x | 0.69 x | bad | 10,148.0 μs | 45.09 KB |
| QR Decode (Stress) | QR Decode (resampled) | ZXing.Net 564.9 μs | 1.08 x | 0.01 x | good | 608.5 μs | 1.88 KB |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 143.8 μs | 1.0 x | 1.0 x | good | 143.8 μs | 7.11 KB |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 143.48 μs | 14.57 KB |
| Code 128 SVG | 28.96 μs | 20.56 KB |
| EAN PNG | 152.03 μs | 6.89 KB |
| Code 39 PNG | 157.63 μs | 10.16 KB |
| Code 93 PNG | 145.56 μs | 8.27 KB |
| UPC-A PNG | 211.86 μs | 7.2 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 168.33 μs | 10.23 KB |
| Data Matrix PNG (long) | 329.57 μs | 20.4 KB |
| Data Matrix SVG | 50.86 μs | 14.38 KB |
| PDF417 PNG | 995.57 μs | 50.05 KB |
| PDF417 SVG | 2,764.04 μs | 74.66 KB |
| Aztec PNG | 210.25 μs | 61.45 KB |
| Aztec SVG | 86.61 μs | 61.47 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 440.5 μs | 9.77 KB |
| QR PNG (medium text) | 570.8 μs | 14.88 KB |
| QR PNG (long text) | 2,159.9 μs | 37.32 KB |
| QR SVG (medium text) | 528.7 μs | 21.83 KB |
| QR PNG High Error Correction | 844.6 μs | 21.27 KB |
| QR PNG (medium text, logo) | 4,504.4 μs | 1099.84 KB |
| QR PNG (medium text, fancy) | 1,948.5 μs | 388.58 KB |
| QR HTML (medium text) | 586.3 μs | 169.2 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 960.7 μs | 3.91 KB |
| QR Decode (clean, balanced) | 978.2 μs | 3.91 KB |
| QR Decode (clean, robust) | 96,554.4 μs | 737.06 KB |
| QR Decode (noisy, robust) | 39,465.2 μs | 160.77 KB |
| QR Decode (screenshot, balanced) | 5,248.6 μs | 3.91 KB |
| QR Decode (antialias, robust) | 1,515.8 μs | 173.73 KB |
| QR Decode (fancy, robust) | 429.7 μs | 2.49 KB |
| QR Decode (resampled, balanced) | 530.8 μs | 1.88 KB |
| QR Decode (no quiet zone, robust) | 110,445.1 μs | 787.41 KB |

#### QrPipelineBenchmarks

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Encode (short text) | 306.52 μs | 936 B |
| QR Encode (medium text) | 651.95 μs | 1288 B |
| QR Encode (long text) | 1,351.67 μs | 2752 B |
| QR Render PNG (short, pre-encoded) | 147.45 μs | 8568 B |
| QR Render PNG (medium, pre-encoded) | 47.98 μs | 13448 B |
| QR Render PNG (long, pre-encoded) | 618.66 μs | 34968 B |
| QR Render Pixels (short, pre-encoded) | 21.90 μs | 121128 B |
| QR Render Pixels (medium, pre-encoded) | 36.69 μs | 197160 B |
| QR Render Pixels (long, pre-encoded) | 724.61 μs | 535848 B |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 219.9 μs<br>61.39 KB | 1,439.5 μs<br>61.42 KB |  | 6,916.9 μs<br>642.46 KB |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 136.9 μs<br>14.48 KB | 1,526.3 μs<br>15.74 KB |  | 52,086.4 μs<br>2035.13 KB |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 167.4 μs<br>10.07 KB | 1,210.3 μs<br>12.28 KB |  | 37,127.1 μs<br>1448.58 KB |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 144.8 μs<br>8.19 KB | 1,137.8 μs<br>11.7 KB |  | 24,648.6 μs<br>957.38 KB |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 219.7 μs<br>15.58 KB | 1,528.7 μs<br>22.31 KB |  | 7,404.9 μs<br>644.89 KB |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 146.7 μs<br>6.8 KB | 948.6 μs<br>11.68 KB |  | 22,110.9 μs<br>850.8 KB |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.000 ms<br>49.95 KB | 5.396 ms<br>207.63 KB |  | 48.017 ms<br>5003.72 KB |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 565.8 μs<br>14.88 KB | 3,240.4 μs<br>79.41 KB | 817.4 μs<br>7.28 KB | 13,579.9 μs<br>1547.24 KB |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 974.3 μs<br>3.91 KB | 1,567.8 μs<br>127.67 KB |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 39.963 ms<br>160.77 KB | 3.164 ms<br>706.89 KB |  |  |

#### QR Decode (Stress)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (fancy) | 367.9 μs<br>2.68 KB | 598.0 μs<br>63.25 KB |  |  |
| QR Decode (no quiet zone) | 10,148.0 μs<br>45.09 KB | 921.7 μs<br>65.78 KB |  |  |
| QR Decode (resampled) | 608.5 μs<br>1.88 KB | 564.9 μs<br>135.23 KB |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 143.8 μs<br>7.11 KB | 1,070.7 μs<br>11.75 KB |  | 19,374.7 μs<br>765.8 KB |
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
