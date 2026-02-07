using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrScreenshotWalkthroughExample {
    private const int DefaultMaxMilliseconds = 600;
    private const int DefaultMaxDimension = 1400;
    private const int DefaultTileGrid = 3;

    public static void Run(string outputDir) {
        var samplesDir = ExamplePaths.ResolveSamplesDir(Path.Combine("Assets", "DecodingSamples"));
        var screenshotPath = Path.Combine(samplesDir, "qr-screenshot-2.png");
        var imageBytes = File.ReadAllBytes(screenshotPath);

        var maxMs = ReadEnvInt("CODEGLYPHX_SCREENSHOT_MAX_MS", DefaultMaxMilliseconds);
        var maxDimension = ReadEnvInt("CODEGLYPHX_SCREENSHOT_MAX_DIM", DefaultMaxDimension);
        var tileGrid = Math.Max(1, ReadEnvInt("CODEGLYPHX_SCREENSHOT_TILE_GRID", DefaultTileGrid));

        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: maxMs, maxDimension: maxDimension);
        var qrOptions = QrPixelDecodeOptions.Screen(maxMilliseconds: maxMs, maxDimension: maxDimension);
        qrOptions.EnableTileScan = true;
        qrOptions.TileGrid = tileGrid;

        Console.WriteLine("Screenshot decode walkthrough:");
        Console.WriteLine("1) Use ImageDecodeOptions.Screen for safe image limits.");
        Console.WriteLine("2) Use QrPixelDecodeOptions.Screen + AutoCrop for UI captures.");
        Console.WriteLine("3) Enable tile scan if the screenshot may contain multiple codes.");
        Console.WriteLine($"   (maxMs={maxMs}, maxDimension={maxDimension}, tileGrid={tileGrid})");
        Console.WriteLine();

        if (QrImageDecoder.TryDecodeImage(imageBytes, imageOptions, out var decoded, out var info, qrOptions)) {
            decoded.Text.WriteText(outputDir, "qr-screenshot-decode.txt");
            QrDiagnosticsDump.WriteText(outputDir, "qr-screenshot-decode-info.txt", info, label: "Decode success", source: screenshotPath);
            Console.WriteLine($"Decoded payload: {decoded.Text}");
            return;
        }

        QrDiagnosticsDump.WriteText(outputDir, "qr-screenshot-decode-info.txt", info, label: "Decode failed", source: screenshotPath);
        Console.WriteLine("Initial screen preset failed; retrying with robust options.");

        var robust = QrPixelDecodeOptions.Robust();
        robust.AutoCrop = true;
        robust.AggressiveSampling = true;
        robust.EnableTileScan = true;
        robust.TileGrid = 3;

        if (QrImageDecoder.TryDecodeImage(imageBytes, imageOptions, out decoded, out info, robust)) {
            decoded.Text.WriteText(outputDir, "qr-screenshot-decode-robust.txt");
            QrDiagnosticsDump.WriteText(outputDir, "qr-screenshot-decode-robust-info.txt", info, label: "Decode success (robust)", source: screenshotPath);
            Console.WriteLine($"Decoded payload (robust): {decoded.Text}");
        } else {
            QrDiagnosticsDump.WriteText(outputDir, "qr-screenshot-decode-robust-info.txt", info, label: "Decode failed (robust)", source: screenshotPath);
            Console.WriteLine("Robust decode failed; see diagnostics output for details.");
        }
    }

    private static int ReadEnvInt(string name, int fallback) {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var value) && value >= 0 ? value : fallback;
    }
}
