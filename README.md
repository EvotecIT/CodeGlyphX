# CodeGlyphX

No-deps QR + barcode toolkit with a super-simple API.

- QR (encode + basic decode)
- Code 128 (Set B/C) encoder
- Renderers: SVG / HTML (incl. email-safe table) / PNG (minimal writer)
- Payload helpers incl. `otpauth://totp` builder + Base32
- WPF controls + demos

## Quick usage (3 lines)

```csharp
using CodeGlyphX;

QR.SavePng("https://example.com", "qr.png");
QR.SaveSvg("https://example.com", "qr.svg");
QR.SaveJpeg("https://example.com", "qr.jpg");
```

```csharp
using CodeGlyphX;

Barcode.SavePng(BarcodeType.Code128, "CODE128-12345", "code128.png");
Barcode.SaveSvg(BarcodeType.Code128, "CODE128-12345", "code128.svg");
```

## Fluent (still simple)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

QR.Create("https://example.com")
  .WithColors(new Rgba32(10, 22, 40), Rgba32.White)
  .WithLogoFile("logo.png")
  .SavePng("qr-logo.png");
```

```csharp
using CodeGlyphX;

Barcode.Create(BarcodeType.Code128, "CODE128-12345")
  .WithModuleSize(2)
  .WithQuietZone(10)
  .SavePng("code128.png");
```

## OTP (AuthIMO)

```csharp
using CodeGlyphX;

Otp.SaveTotpPng("AuthIMO", "user@example.com", "JBSWY3DPEHPK3PXP", "totp.png");
Otp.SaveHotpPng("AuthIMO", "user@example.com", "JBSWY3DPEHPK3PXP", counter: 42, "hotp.png");
```

## Advanced (low-level)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

var qr = QrCodeEncoder.EncodeText("Hello from CodeGlyphX", QrErrorCorrectionLevel.M);
var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { ModuleSize = 8, QuietZone = 4 });
var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });
```

## WPF controls

Add a reference to `CodeGlyphX.Wpf` and use in XAML:

```xml
xmlns:wpf="clr-namespace:CodeGlyphX.Wpf;assembly=CodeGlyphX.Wpf"
```

```xml
<wpf:QrCodeControl Text="{Binding QrText}" Ecc="M" ModuleSize="6" QuietZone="4" />
<wpf:Barcode128Control Value="{Binding BarcodeValue}" ModuleSize="2" QuietZone="10" />
```

## Demos

- `CodeGlyphX.Demo.Wpf`: generate QR + Code 128, export SVG/HTML/PNG, and build + render an OTP `otpauth://` URI
- `CodeGlyphX.ScreenScan.Wpf`: prototype that captures a screen region and tries to decode a clean, high-contrast QR
