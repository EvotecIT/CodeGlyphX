# Benchmarks

<!-- BENCHMARK:WINDOWS:START -->
## WINDOWS

Updated: 2026-01-22 12:24:44 UTC
Framework: net8.0
Configuration: Release
Artifacts: C:\Support\GitHub\CodeMatrix\Build\BenchmarkResults\windows-20260122-132337
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
| Aztec (Encode) | Aztec PNG | CodeGlyphX 325.7 μs | 1 x | 325.7 μs | 452.22 KB |
| Code 128 (Encode) | Code128 PNG | CodeGlyphX 484.4 μs | 1 x | 484.4 μs | 756.09 KB |
| Code 39 (Encode) | Code39 PNG | CodeGlyphX 312.9 μs | 1 x | 312.9 μs | 414.38 KB |
| Code 93 (Encode) | Code93 PNG | CodeGlyphX 252.8 μs | 1 x | 252.8 μs | 367.65 KB |
| Data Matrix (Encode) | Data Matrix PNG (medium) | CodeGlyphX 369.8 μs | 1 x | 369.8 μs | 421.62 KB |
| EAN-13 (Encode) | EAN-13 PNG | CodeGlyphX 227.7 μs | 1 x | 227.7 μs | 338.55 KB |
| PDF417 (Encode) | PDF417 PNG | CodeGlyphX 2.135 ms | 1 x | 2.135 ms | 3154.45 KB |
| QR (Encode) | QR PNG (medium) | QRCoder 933.1 μs | 1.86 x | 1,734.4 μs | 837.5 KB |
| QR Decode (Clean) | QR Decode (clean) | ZXing.Net 1.713 ms | 1.63 x | 2.800 ms | 103.9 KB |
| QR Decode (Noisy) | QR Decode (noisy) | ZXing.Net 3.375 ms | 53.75 x | 181.417 ms | 8507.11 KB |
| UPC-A (Encode) | UPC-A PNG | CodeGlyphX 217.8 μs | 1 x | 217.8 μs | 338.85 KB |

### Baseline

#### 1D Barcodes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Code 128 PNG | 497.87 μs | 756.17 KB |
| Code 128 SVG | 18.60 μs | 17.61 KB |
| EAN PNG | 211.00 μs | 338.63 KB |
| Code 39 PNG | 323.17 μs | 414.45 KB |
| Code 93 PNG | 240.77 μs | 367.73 KB |
| UPC-A PNG | 213.22 μs | 338.93 KB |

#### 2D Matrix Codes (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 409.60 μs | 447.69 KB |
| Data Matrix PNG (long) | 1,058.27 μs | 1508.91 KB |
| Data Matrix SVG | 54.03 μs | 12.29 KB |
| PDF417 PNG | 1,870.73 μs | 3154.54 KB |
| PDF417 SVG | 2,266.07 μs | 64.53 KB |
| Aztec PNG | 347.57 μs | 452.27 KB |
| Aztec SVG | 96.53 μs | 59.88 KB |

#### QR (Encode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 999.3 μs | 431.91 KB |
| QR PNG (medium text) | 1,503.4 μs | 837.67 KB |
| QR PNG (long text) | 4,740.3 μs | 3040.78 KB |
| QR SVG (medium text) | 809.9 μs | 20.04 KB |
| QR PNG High Error Correction | 2,332.3 μs | 1535.74 KB |
| QR HTML (medium text) | 891.1 μs | 137.44 KB |

#### QR (Decode)

| Scenario | Mean | Allocated |
| --- | --- | --- |
| QR Decode (clean, fast) | 2.520 ms | 103.9 KB |
| QR Decode (clean, balanced) | 3.137 ms | 103.9 KB |
| QR Decode (clean, robust) | 2.394 ms | 103.9 KB |
| QR Decode (noisy, robust) | 156.596 ms | 8507.11 KB |

### Comparisons

#### Aztec (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Aztec PNG | 325.7 μs<br>452.22 KB | 1,376.4 μs<br>61.42 KB |  | 6,517.8 μs<br>642.63 KB |

#### Code 128 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code128 PNG | 484.4 μs<br>756.09 KB | 1,699.2 μs<br>15.74 KB |  | 56,985.5 μs<br>2035.16 KB |

#### Code 39 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code39 PNG | 312.9 μs<br>414.38 KB | 1,461.3 μs<br>12.28 KB |  | 41,788.3 μs<br>1448.61 KB |

#### Code 93 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Code93 PNG | 252.8 μs<br>367.65 KB | 1,397.3 μs<br>11.7 KB |  | 32,773.0 μs<br>957.41 KB |

#### Data Matrix (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| Data Matrix PNG (medium) | 369.8 μs<br>421.62 KB | 2,413.7 μs<br>22.31 KB |  | 9,783.3 μs<br>645.05 KB |

#### EAN-13 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| EAN-13 PNG | 227.7 μs<br>338.55 KB | 1,068.9 μs<br>11.68 KB |  | 26,643.6 μs<br>850.83 KB |

#### PDF417 (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| PDF417 PNG | 2.135 ms<br>3154.45 KB | 7.500 ms<br>207.63 KB |  | 53.913 ms<br>5003.86 KB |

#### QR (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR PNG (medium) | 1,734.4 μs<br>837.5 KB | 3,936.7 μs<br>79.41 KB | 933.1 μs<br>7.31 KB | 14,103.8 μs<br>1547.36 KB |

#### QR Decode (Clean)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (clean) | 2.800 ms<br>103.9 KB | 1.713 ms<br>127.67 KB |  |  |

#### QR Decode (Noisy)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| QR Decode (noisy) | 181.417 ms<br>8507.11 KB | 3.375 ms<br>706.89 KB |  |  |

#### UPC-A (Encode)

| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |
| --- | --- | --- | --- | --- |
| UPC-A PNG | 217.8 μs<br>338.85 KB | 1,162.9 μs<br>11.75 KB |  | 19,783.0 μs<br>765.89 KB |
<!-- BENCHMARK:WINDOWS:END -->

<!-- BENCHMARK:LINUX:START -->
_no results yet_
<!-- BENCHMARK:LINUX:END -->

<!-- BENCHMARK:MACOS:START -->
_no results yet_
<!-- BENCHMARK:MACOS:END -->
