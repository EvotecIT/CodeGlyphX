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

```csharp
dotnet add package CodeGlyphX
```

## Package Manager Console

```csharp
Install-Package CodeGlyphX
```

## PackageReference

Add the following to your `.csproj` file:

```xml
<PackageReference Include="CodeGlyphX" Version="*" />
```

## Supported Frameworks

- **.NET 8.0+** - Full support, no additional dependencies

- **.NET Standard 2.0** - Requires System.Memory 4.5.5

- **.NET Framework 4.7.2+** - Requires System.Memory 4.5.5

## Feature Availability

Most features are available across all targets, but the QR pixel pipeline and Span-based APIs are net8+ only.

| Feature | net8.0+ | net472 / netstandard2.0 |
| --- | --- | --- |
| Encode (QR/Micro QR + 1D/2D) | Yes | Yes |
| Decode from module grids (BitMatrix) | Yes | Yes |
| Renderers + image file codecs | Yes | Yes |
| 1D/2D pixel decode (Barcode/DataMatrix/PDF417/Aztec) | Yes | Yes |
| QR pixel decode from raw pixels / screenshots | Yes | No (returns false) |
| QR pixel debug rendering | Yes | No |
| Span-based overloads | Yes | No (byte[] only) |

QR pixel decode APIs are net8+ only (e.g., `QrImageDecoder.TryDecodeImage(...)` and `QrDecoder.TryDecode(...)` from pixels).

You can check capabilities at runtime via `CodeGlyphXFeatures` (for example, `SupportsQrPixelDecode` and `SupportsQrPixelDebug`).

**Choosing a target:** pick `net8.0+` for QR image decoding, pixel debug tools, Span APIs, and maximum throughput. Pick `net472`/`netstandard2.0` for legacy apps that only need encoding, rendering, and module-grid decode.
