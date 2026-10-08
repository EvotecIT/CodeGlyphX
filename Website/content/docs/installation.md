---
title: Installation - CodeGlyphX
description: Install CodeGlyphX via NuGet and configure targets.
slug: installation
collection: docs
layout: docs
---

{{< edit-link >}}

# Installation

CodeGlyphX is available as a NuGet package and can be installed in several ways.

## .NET CLI

```bash
dotnet add package CodeGlyphX
```

## Package Manager Console

```powershell
Install-Package CodeGlyphX
```

## PackageReference

Add the following to your `.csproj` file:

```xml
<PackageReference Include="CodeGlyphX" Version="3.0.0" />
```

## Supported Frameworks

- **.NET 8.0 and .NET 10.0** - Full QR pixel pipeline, no runtime package dependencies
- **.NET Standard 2.0** - Legacy-compatible libraries with managed QR image fallback
- **.NET Framework 4.7.2+** - Legacy applications with managed QR image fallback

NuGet resolves `System.Memory` and `System.Text.Encoding.CodePages` automatically for the legacy targets. You do not need to install those dependencies separately. Every target uses managed code without a native graphics library.

## Feature Availability

Most features are available across all targets. Modern targets have the full QR pixel pipeline; legacy targets use a less capable fallback intended for clean, generated QR images.

| Feature | net8.0+ | net472 / netstandard2.0 |
| --- | --- | --- |
| Encode (QR/Micro QR + 1D/2D) | Yes | Yes |
| Decode from module grids (BitMatrix) | Yes | Yes |
| Renderers + image file codecs | Yes | Yes |
| 1D/2D pixel decode (Barcode/DataMatrix/PDF417/Aztec) | Yes | Yes |
| QR image decode | Full pixel pipeline | Clean-image fallback |
| QR pixel debug rendering | Yes | No |
| Span-based encoded-image overloads | Yes | Yes |
| Fast Span-based pixel pipeline | Yes | No |

`QrImageDecoder.TryDecodeImage(...)` is available on every target. Its recognition capability depends on the selected target; the legacy fallback is not intended for screenshots, distorted images or styled QR artwork.

Check capabilities at runtime with `CodeGlyphXFeatures.SupportsQrPixelDecode`, `SupportsQrPixelDecodeFallback`, `SupportsQrPixelDebug` and `SupportsSpanPixelPipeline`. `SupportsSpanPixelPipeline` describes the fast pixel pipeline, rather than every API that accepts a Span.

**Choosing a target:** use `net8.0` or newer for screenshots, styled QR images, pixel debug tools and maximum throughput. Use `net472` or `netstandard2.0` for legacy applications, and qualify their clean-image fallback against the images your application receives.
