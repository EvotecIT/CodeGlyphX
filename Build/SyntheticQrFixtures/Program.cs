using System.Security.Cryptography;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Tests;

// From the repository root:
// dotnet run --project Build/SyntheticQrFixtures -- Assets/DecodingSamples
// Only synthetic pixels and the shared public test payloads enter these fixtures.
if (args.Length != 1) {
    Console.Error.WriteLine("Usage: SyntheticQrFixtures <output-directory>");
    return 1;
}

Directory.CreateDirectory(args[0]);
WriteFixture("qr-clean-small.png", SyntheticQrFixtureData.Clean, noisy: false);
WriteFixture("qr-noisy-ui.png", SyntheticQrFixtureData.Noisy, noisy: true);
return 0;

void WriteFixture(string fileName, string payload, bool noisy) {
    var width = noisy ? 740 : 359;
    var height = noisy ? 805 : 279;
    var canvas = new byte[width * height * 4];
    FillRectangle(canvas, width, 0, 0, width, height, noisy ? (byte)238 : (byte)255);

    if (noisy) {
        // Neutral UI chrome and controls create competing edges outside the QR card.
        FillRectangle(canvas, width, 0, 0, width, 62, 208);
        FillRectangle(canvas, width, 24, 20, 126, 18, 100);
        FillRectangle(canvas, width, 0, 62, 116, height - 62, 218);
        for (var row = 0; row < 8; row++) {
            FillRectangle(canvas, width, 18, 96 + row * 48, 78, 10, 155);
        }
        FillRectangle(canvas, width, 142, 110, 564, 610, 255);
        FillRectangle(canvas, width, 174, 134, 270, 15, 85);
        FillRectangle(canvas, width, 174, 165, 454, 8, 185);
        FillRectangle(canvas, width, 174, 179, 392, 8, 185);
        FillRectangle(canvas, width, 257, 478, 240, 36, 110);
    }

    var code = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.M);
    var qrPixels = QrPngRenderer.RenderPixels(code.Modules,
        new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 },
        out var sourceWidth, out _, out _);
    var size = sourceWidth;
    // Both active symbols stay near the original 196px footprint and source offsets.
    var left = noisy ? 246 : 80;
    var top = noisy ? 188 : 34;
    BlitQr(canvas, width, qrPixels, size, left, top);

    if (noisy) {
        // A small erased data patch plus fixed UI salt/pepper and luminance noise.
        // The patch is away from the finder patterns, timing lines and quiet zone.
        FillRectangle(canvas, width, left + size / 2 + 18, top + size / 2 + 12, 3, 3, 244);
        for (var i = 0; i < width * height; i++) {
            var noise = unchecked((uint)i * 747796405u + 2891336453u);
            noise = ((noise >> (int)((noise >> 28) + 4)) ^ noise) * 277803737u;
            noise = (noise >> 22) ^ noise;
            var inQr = i % width >= left && i % width < left + size &&
                i / width >= top && i / width < top + size;
            var value = canvas[i * 4];
            value = (byte)Math.Clamp(value + (int)(noise % (inQr ? 7u : 19u)) - (inQr ? 3 : 9), 0, 255);
            if (!inQr && noise % 997 == 0) value = (noise & 1) == 0 ? (byte)0 : (byte)255;
            canvas[i * 4] = canvas[i * 4 + 1] = canvas[i * 4 + 2] = value;
        }
    }

    var png = PngImageEncoder.EncodeRgba32(canvas, width, height);
    File.WriteAllBytes(Path.Combine(args[0], fileName), png);
    // Verify the real PNG through the public scanner without printing its payload.
    var result = SymbolScanner.Scan(png, new ScanOptions {
        Formats = new[] { SymbolFormat.QrCode },
        Profile = ScanProfile.Robust,
        TimeoutMilliseconds = noisy ? 5000 : 2000,
        MaxSymbols = 1
    });
    if (result.Symbols.Count != 1 || result.Symbols[0].Text != payload) {
        throw new InvalidOperationException($"Synthetic fixture did not decode: {fileName}");
    }
    Console.WriteLine($"{fileName}: {width}x{height}, QR version {code.Version}, mask {code.Mask}, " +
        $"symbol {size}px, public scan matched, SHA256 {Convert.ToHexString(SHA256.HashData(png))}");
}

static void FillRectangle(byte[] canvas, int width, int left, int top, int rectangleWidth, int rectangleHeight, byte value) {
    for (var y = top; y < top + rectangleHeight; y++) {
        for (var x = left; x < left + rectangleWidth; x++) {
            var offset = (y * width + x) * 4;
            canvas[offset] = canvas[offset + 1] = canvas[offset + 2] = value;
            canvas[offset + 3] = 255;
        }
    }
}

static void BlitQr(byte[] canvas, int width, byte[] source, int size, int left, int top) {
    for (var y = 0; y < size; y++) {
        Buffer.BlockCopy(source, y * size * 4, canvas, ((top + y) * width + left) * 4, size * 4);
    }
}
