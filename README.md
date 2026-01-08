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

QR.Save("https://example.com", "qr.png");
QR.Save("https://example.com", "qr.svg");
QR.Save("https://example.com", "qr.jpg");
```

```csharp
using CodeGlyphX;

Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.png");
Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.svg");
```

## Fluent (still simple)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

QR.Create("https://example.com")
  .WithColors(new Rgba32(10, 22, 40), Rgba32.White)
  .WithLogoFile("logo.png")
  .Save("qr-logo.png");
```

```csharp
using CodeGlyphX;

Barcode.Create(BarcodeType.Code128, "CODE128-12345")
  .WithModuleSize(2)
  .WithQuietZone(10)
  .Save("code128.png");
```

## OTP (AuthIMO)

```csharp
using CodeGlyphX;

Otp.SaveTotp("AuthIMO", "user@example.com", "JBSWY3DPEHPK3PXP", "totp.png");
Otp.SaveHotp("AuthIMO", "user@example.com", "JBSWY3DPEHPK3PXP", counter: 42, "hotp.png");
```

## Decode (pixels)

```csharp
using CodeGlyphX;

if (QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded)) {
    Console.WriteLine(decoded.Text);
}

if (QrImageDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var all)) {
    foreach (var item in all) Console.WriteLine(item.Text);
}
```

## Decode (typed payloads)

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

var decoded = QR.DecodePng(File.ReadAllBytes("qr.png"));
if (decoded.Parsed.TryGet<QrParsedData.Wifi>(out var wifi)) {
    Console.WriteLine($"{wifi.Ssid} / {wifi.AuthType}");
}
```

## Decode (barcode PNG)

```csharp
using CodeGlyphX;

if (Barcode.TryDecodePng(File.ReadAllBytes("code128.png"), out var decoded)) {
    Console.WriteLine($"{decoded.Type}: {decoded.Text}");
}
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

## Styling (example)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

var logo = File.ReadAllBytes("logo.png");
var options = new QrEasyOptions {
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
    ModuleSize = 8,
    ModuleShape = QrPngModuleShape.Rounded,
    ModuleScale = 0.88,
    Foreground = new Rgba32(18, 44, 78),
    Background = new Rgba32(250, 252, 255),
    LogoPng = logo,
    LogoScale = 0.22,
    LogoPaddingPx = 6
};

QR.Save("https://example.com", "qr-styled.png", options);
```

## Multi-QR decode (pixels)

```csharp
using CodeGlyphX;

if (QrImageDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var all)) {
    foreach (var item in all) Console.WriteLine(item.Text);
}
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
