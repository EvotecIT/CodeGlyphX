# Benchmarks

<!-- BENCHMARK:WINDOWS:START -->
## WINDOWS

Updated: 2026-01-22 12:20:39 UTC
Framework: net8.0
Configuration: Release
Artifacts: /mnt/c/Support/GitHub/CodeMatrix/Build/BenchmarkResults/windows-20260122-124936
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

| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Mean | CodeGlyphX Alloc |
| --- | --- | --- | --- | --- | --- |
| Aztec (Encode) | Aztec PNG | CodeGlyphX 324.2 μs | 1 x | 324.2 μs | 452.22 KB |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 435.2 μs | 1 x | 435.2 μs | 756.09 KB |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 288.3 μs | 1 x | 288.3 μs | 414.38 KB |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 221.8 μs | 1 x | 221.8 μs | 367.65 KB |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 343.5 μs | 1 x | 343.5 μs | 421.62 KB |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 213.9 μs | 1 x | 213.9 μs | 338.55 KB |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 1.828 ms | 1 x | 1.828 ms | 3154.45 KB |
| QR (Encode) | QR PNG (medium) | QRCoder 831.5 μs | 1.78 x | 1,476.0 μs | 837.5 KB |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.672 ms | 1.41 x | 2.350 ms | 103.9 KB |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.322 ms | 46.15 x | 153.310 ms | 8507.11 KB |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 197.3 μs | 1 x | 197.3 μs | 338.85 KB |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 437.37 μs | 756.17 KB |
| Code 128 SVG | 19.10 μs | 17.61 KB |
| EAN PNG | 204.40 μs | 338.63 KB |
| Code 39 PNG | 287.63 μs | 414.45 KB |
| Code 93 PNG | 223.78 μs | 367.73 KB |
| UPC-A PNG | 205.43 μs | 338.93 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 338.98 μs | 447.69 KB |
| Data Matrix PNG (long) | 1,145.13 μs | 1508.91 KB |
| Data Matrix SVG | 54.43 μs | 12.29 KB |
| PDF417 PNG | 1,799.23 μs | 3154.54 KB |
| PDF417 SVG | 2,186.35 μs | 64.53 KB |
| Aztec PNG | 322.38 μs | 452.27 KB |
| Aztec SVG | 85.07 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 799.6 μs | 431.91 KB |
| QR PNG (medium text) | 1,430.7 μs | 837.67 KB |
| QR PNG (long text) | 4,951.7 μs | 3040.78 KB |
| QR SVG (medium text) | 748.2 μs | 20.04 KB |
| QR PNG High Error Correction | 2,194.6 μs | 1535.74 KB |
| QR HTML (medium text) | 779.2 μs | 137.44 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 2.233 ms | 103.9 KB |
| QR Decode (clean, balanced) | 2.249 ms | 103.9 KB |
| QR Decode (clean, robust) | 2.363 ms | 103.9 KB |
| QR Decode (noisy, robust) | 157.051 ms | 8507.11 KB |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 324.2 μs<br>452.22 KB | 1,348.2 μs<br>61.42 KB |  | 6,036.6 μs<br>642.58 KB |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 435.2 μs<br>756.09 KB | 1,312.9 μs<br>15.74 KB |  | 42,068.9 μs<br>2035.01 KB |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 288.3 μs<br>414.38 KB | 1,112.7 μs<br>12.28 KB |  | 30,737.3 μs<br>1448.61 KB |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 221.8 μs<br>367.65 KB | 969.9 μs<br>11.7 KB |  | 21,576.4 μs<br>957.46 KB |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 343.5 μs<br>421.62 KB | 1,405.2 μs<br>22.31 KB |  | 6,186.8 μs<br>645.2 KB |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 213.9 μs<br>338.55 KB | 884.4 μs<br>11.68 KB |  | 17,852.8 μs<br>850.83 KB |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 1.828 ms<br>3154.45 KB | 5.209 ms<br>207.63 KB |  | 39.022 ms<br>5004.04 KB |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 1,476.0 μs<br>837.5 KB | 3,004.0 μs<br>79.41 KB | 831.5 μs<br>7.31 KB | 11,558.5 μs<br>1547.36 KB |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.350 ms<br>103.9 KB | 1.672 ms<br>127.67 KB |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 153.310 ms<br>8507.11 KB | 3.322 ms<br>706.89 KB |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 197.3 μs<br>338.85 KB | 943.5 μs<br>11.75 KB |  | 17,499.4 μs<br>765.89 KB |
<!-- BENCHMARK:WINDOWS:END -->

<!-- BENCHMARK:LINUX:START -->
_no results yet_
<!-- BENCHMARK:LINUX:END -->

<!-- BENCHMARK:MACOS:START -->
_no results yet_
<!-- BENCHMARK:MACOS:END -->
