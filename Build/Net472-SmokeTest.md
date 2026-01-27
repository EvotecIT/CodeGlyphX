# net472 QR Decode Smoke Test (Windows)

Goal: quickly confirm the legacy QR image fallback is healthy without committing large images.

## 1) Build the net472 target

From the repo root:

```powershell
dotnet build .\CodeGlyphX\CodeGlyphX.csproj -c Release -f net472
```

## 2) Sanity-check the fallback via tests (fast)

These run on net8 but force the fallback path:

```powershell
dotnet test .\CodeGlyphX.Tests\CodeGlyphX.Tests.csproj -c Release -f net8.0 --filter "FullyQualifiedName~QrImageDecoderFallbackTests"
```

## 3) Local round-trip console check on net472

Create a temporary console:

```powershell
mkdir .\artifacts\net472-smoke -Force | Out-Null
cd .\artifacts\net472-smoke
dotnet new console -f net472
dotnet add reference ..\..\CodeGlyphX\CodeGlyphX.csproj
```

Replace `Program.cs` with:

```csharp
using System;
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

var payload = $"NET472-SMOKE-{DateTime.UtcNow:yyyyMMddHHmmss}";
var qr = QrCodeEncoder.EncodeText(payload);

var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
    ModuleSize = 16,
    QuietZone = 6
});

var ok = QrImageDecoder.TryDecodeImage(png, out var decoded, out var info, new QrPixelDecodeOptions {
    MaxDimension = 2048
});

Console.WriteLine($"OK: {ok}");
Console.WriteLine(decoded?.Text);
Console.WriteLine($"Dimension: {info.Dimension}, Threshold: {info.Threshold}");
Environment.ExitCode = ok && decoded.Text == payload ? 0 : 1;
```

Run it:

```powershell
dotnet run -c Release
```

## 4) Real-world screenshot spot-checks (optional but recommended)

Add a few local images (not committed), then try:

```csharp
var bytes = System.IO.File.ReadAllBytes(@"C:\path\to\sample.png");
if (QrImageDecoder.TryDecodeImage(bytes, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

Notes:
- Expect best results on clean/generated images.
- Multi-code and heavily stylized/artistic inputs remain best-effort on net472.
