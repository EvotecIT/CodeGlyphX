# Getting Started with CodeGlyphX

CodeGlyphX is a zero-dependency .NET library for generating and decoding QR codes, barcodes, and other 2D matrix codes.

## Installation

Install via NuGet:

```bash
dotnet add package CodeGlyphX
```

## Quick Example

```csharp
using CodeGlyphX;

// Generate a QR code
QR.Save("https://evotec.xyz", "website.png");

// Generate a barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");
```

## Supported Frameworks

- .NET 8.0+ (no dependencies)
- .NET Standard 2.0 (requires System.Memory)
- .NET Framework 4.7.2+ (requires System.Memory)

## Next Steps

- Learn about [QR Code generation](qr-codes.md)
- Explore [1D Barcode support](barcodes.md)
- Use [Payload Helpers](payloads.md) for WiFi, contacts, and payments
