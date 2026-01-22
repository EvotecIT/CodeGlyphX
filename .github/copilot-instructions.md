# CodeGlyphX - Copilot Instructions

CodeGlyphX is a zero-dependency .NET library for generating and decoding QR codes, barcodes, and 2D matrix codes.

## Quick Patterns

### QR Code
```csharp
using CodeGlyphX;
QR.Save("data", "output.png");           // File
byte[] png = QR.ToPng("data");           // Bytes
string svg = QR.ToSvg("data");           // SVG string
```

### Barcode
```csharp
using CodeGlyphX;
Barcode.Save(BarcodeType.Code128, "data", "barcode.png");
byte[] png = Barcode.Png(BarcodeType.Code128, "data");
```

### 2D Matrix
```csharp
using CodeGlyphX;
DataMatrixCode.Save("data", "dm.png");
Pdf417Code.Save("data", "pdf417.png");
AztecCode.Save("data", "aztec.png");
```

### Decode
```csharp
using CodeGlyphX;
if (QrImageDecoder.TryDecodeImage(bytes, out var result))
    Console.WriteLine(result.Text);
```

### Payloads
```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;
QR.Save(QrPayloads.Wifi("SSID", "password"), "wifi.png");
QR.Save(QrPayloads.VCard(firstName: "John", lastName: "Doe"), "contact.png");
QR.Save(QrPayloads.OneTimePassword(OtpAuthType.Totp, "SECRET", label: "user@example.com", issuer: "App"), "otp.png");
```

## Barcode Types
`Code128`, `Gs1128`, `Code39`, `Code93`, `Code11`, `Codabar`, `Ean13`, `Ean8`, `UpcA`, `UpcE`, `Itf14`, `Itf`, `Msi`, `Plessey`, `Telepen`

## Output Formats
Auto-detected by extension: `.png`, `.svg`, `.pdf`, `.jpg`, `.bmp`, `.html`, `.eps`

## Key Namespaces
- `CodeGlyphX` - Main classes (QR, Barcode, DataMatrixCode, etc.)
- `CodeGlyphX.Payloads` - QrPayloads helper
- `CodeGlyphX.Rendering` - Renderers and options
