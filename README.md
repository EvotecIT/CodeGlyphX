# CodeMatrix

No-deps QR + barcode building blocks for AuthIMO:

- QR (encode + basic decode)
- Code 128 (Set B/C) encoder
- Renderers: SVG / HTML (incl. email-safe table) / PNG (minimal writer)
- Payload helpers incl. `otpauth://totp` builder + Base32
- WPF controls + demos

## Quick usage (core)

```csharp
using CodeMatrix;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Rendering.Svg;

var qr = QrCodeEncoder.EncodeText("Hello from CodeMatrix", QrErrorCorrectionLevel.M);
var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { ModuleSize = 8, QuietZone = 4 });
var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });
```

```csharp
using CodeMatrix;
using CodeMatrix.Rendering.Svg;

var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CODE128-12345");
var svg = SvgBarcodeRenderer.Render(barcode, new BarcodeSvgRenderOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 40 });
```

## OTP (AuthIMO)

```csharp
using CodeMatrix;

var secret = OtpAuthSecret.FromBase32("JBSWY3DPEHPK3PXP");
var uri = OtpAuthTotp.Create("AuthIMO", "user@example.com", secret);

// Render URI as QR:
var qr = QrCodeEncoder.EncodeText(uri);
```

## WPF controls

Add a reference to `CodeMatrix.Wpf` and use in XAML:

```xml
xmlns:wpf="clr-namespace:CodeMatrix.Wpf;assembly=CodeMatrix.Wpf"
```

```xml
<wpf:QrCodeControl Text="{Binding QrText}" Ecc="M" ModuleSize="6" QuietZone="4" />
<wpf:Barcode128Control Value="{Binding BarcodeValue}" ModuleSize="2" QuietZone="10" />
```

## Demos

- `CodeMatrix.Demo.Wpf`: generate QR + Code 128, export SVG/HTML/PNG, and build + render an OTP `otpauth://` URI
- `CodeMatrix.ScreenScan.Wpf`: prototype that captures a screen region and tries to decode a clean, high-contrast QR

