# CodeGlyphX

No-deps QR + barcode toolkit with a super-simple API.

- QR (encode + robust decode, incl. Micro QR, ECI, FNC1, Kanji)
- 1D barcodes (encode + decode): Code128/GS1-128, Code39, Code93, EAN-8/13, UPC-A/UPC-E, ITF-14
- Renderers: SVG / HTML (incl. email-safe table) / PNG / JPEG + optional labels
- Image decode: PNG / JPEG / GIF / BMP / PPM / TGA (no deps, JPEG baseline + progressive, EXIF orientation)
- Payload helpers incl. `otpauth://totp` builder + Base32
- WPF controls + demos

## Support matrix

| Symbology | Encode | Decode | Notes |
| --- | --- | --- | --- |
| QR | ✅ | ✅ | ECI, FNC1/GS1, Kanji, structured append |
| Micro QR | ✅ | ✅ | Versions M1–M4 |
| Code128 | ✅ | ✅ | Set B/C |
| GS1-128 | ✅ | ✅ | FNC1 + AI helpers |
| Code39 | ✅ | ✅ | Optional checksum |
| Code93 | ✅ | ✅ | Optional checksum |
| EAN-8 / EAN-13 | ✅ | ✅ | Checksum validation |
| UPC-A / UPC-E | ✅ | ✅ | Checksum validation |
| ITF-14 | ✅ | ✅ | Checksum validation |
| Data Matrix | ✅ | ✅ | Encode: ASCII/C40/Text/X12/EDIFACT/Base256 |
| PDF417 | ✅ | ✅ | Full encode/decode |

## Quick usage (3 lines)

```csharp
using CodeGlyphX;

QR.Save("https://example.com", "qr.png");
QR.Save("https://example.com", "qr.svg");
QR.Save("https://example.com", "qr.jpg");
```

```csharp
using CodeGlyphX;

// Auto-detect payloads (email, phone, URL, Wi-Fi, OTP, etc.)
QR.SaveAuto("user@example.com", "auto-email.png");
QR.SaveAuto("+1 202 555 0144", "auto-phone.png");
QR.SaveAuto("example.com", "auto-url.png");
```

```csharp
using CodeGlyphX;

Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.png");
Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.svg");
```

```csharp
using CodeGlyphX;

// GS1-128 (AI string)
Barcode.Save(BarcodeType.GS1_128, "(01)09506000134352(10)ABC123", "gs1-128.png");
```

```csharp
using CodeGlyphX;

// GS1 helper (element string)
var elementString = Gs1.ElementString("(01)09506000134352(10)ABC123(17)240101");
Barcode.Save(BarcodeType.GS1_128, elementString, "gs1-128.png");
```

```csharp
using CodeGlyphX;

DataMatrixCode.Save("DataMatrix-12345", "datamatrix.png");
Pdf417Code.Save("PDF417-12345", "pdf417.png");
```

## Payloads (3 lines each)

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

QR.Save(QrPayloads.Url("https://example.com"), "url.png");
QR.Save(QrPayloads.Text("Hello world"), "text.png");
QR.Save(QrPayloads.Wifi("MyWiFi", "p@ssw0rd"), "wifi.png");
QR.Save(QrPayloads.Email("hello@example.com", "Hi", "How are you?"), "email.png");
QR.Save(QrPayloads.Phone("+1-202-555-0144"), "phone.png");
QR.Save(QrPayloads.Sms("+1-202-555-0144", "Ping"), "sms.png");
QR.Save(QrPayloads.Geo("52.2297", "21.0122"), "location.png");
QR.Save(QrPayloads.Contact(QrContactOutputType.MeCard, "Ada", "Lovelace", email: "ada@example.com"), "contact.png");
QR.Save(QrPayloads.CalendarEvent("Meeting", "Sync", "Office", System.DateTime.UtcNow, System.DateTime.UtcNow.AddHours(1), allDayEvent: false), "calendar.png");
QR.Save(QrPayloads.OneTimePassword(OtpAuthType.Totp, "JBSWY3DPEHPK3PXP", label: "user@example.com", issuer: "AuthIMO"), "otp.png");
QR.Save(QrPayloads.AppStore("1234567890"), "appstore.png");
QR.Save(QrPayloads.Facebook("evotec"), "facebook.png");
QR.Save(QrPayloads.Twitter("evotecit"), "twitter.png");
QR.Save(QrPayloads.TikTok("evotec"), "tiktok.png");
QR.Save(QrPayloads.LinkedIn("evotec"), "linkedin.png");
QR.Save(QrPayloads.Upi("merchant@upi", "Evotec", amount: 12.34m), "upi.png");
```

## Presets (safe defaults)

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

QR.Save(QrPayloads.OneTimePassword(OtpAuthType.Totp, "JBSWY3DPEHPK3PXP", label: "user@example.com", issuer: "AuthIMO"),
        "otp.png",
        QrPresets.Otp());

QR.Save(QrPayloads.Wifi("MyWiFi", "p@ssw0rd"), "wifi.png", QrPresets.Wifi());
QR.Save(QrPayloads.Contact(QrContactOutputType.MeCard, "Ada", "Lovelace", email: "ada@example.com"), "contact.png", QrPresets.Contact());
```

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var logo = RenderIO.ReadBinary("logo.png");
QR.Save("https://example.com", "qr-logo.png", QrPresets.Logo(logo));
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
  .WithLabel("CODE128-12345")
  .Save("code128.png");
```

```csharp
using CodeGlyphX;

DataMatrixCode.Create("DataMatrix-12345")
  .WithModuleSize(6)
  .WithQuietZone(4)
  .Save("datamatrix.png");
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

## Decode (images)

```csharp
using CodeGlyphX;

if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("code.bmp"), out var decoded)) {
    Console.WriteLine(decoded.Text);
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

## Decode (auto-detect)

```csharp
using CodeGlyphX;

var decoded = CodeGlyph.DecodePng(File.ReadAllBytes("unknown.png"));
Console.WriteLine($"{decoded.Kind}: {decoded.Text}");
```

```csharp
using CodeGlyphX;

// Prefer barcode when you know the symbol kind, and optionally hint the type.
if (CodeGlyph.TryDecodePng(File.ReadAllBytes("code128.png"), out var decoded, expectedBarcode: BarcodeType.Code128, preferBarcode: true)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

// Raw pixels (e.g. screen capture).
if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

// Decode all (multi-QR + optional barcode).
if (CodeGlyph.TryDecodeAllPng(File.ReadAllBytes("unknown.png"), out var results)) {
    foreach (var item in results) Console.WriteLine($"{item.Kind}: {item.Text}");
}
```

```csharp
using CodeGlyphX;

// Decode all from raw pixels.
if (CodeGlyph.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var results)) {
    foreach (var item in results) Console.WriteLine($"{item.Kind}: {item.Text}");
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

## License

Apache-2.0.

Commercial support and custom licensing are available. Contact: contact@evotec.pl.
