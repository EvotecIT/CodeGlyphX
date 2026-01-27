using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrHardArtDiagnosticsExample {
    private static readonly string[] HardArtFiles = {
        "qr-art-facebook-splash-grid.png",
        "qr-art-montage-grid.png",
        "qr-art-stripe-eye-grid.png",
        "qr-art-drip-variants.png",
        "qr-art-solid-bg-grid.png",
        "qr-art-gear-illustration-grid.png"
    };

    public static void Run(string outputDir) {
        var sampleDir = ResolveSamplesDir("Assets/DecodingSamples");
        var debugDir = Path.Combine(outputDir, "qr-hard-art-debug");
        Directory.CreateDirectory(debugDir);

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            BudgetMilliseconds = 12000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 4
        };

        var lines = new List<string> {
            $"SampleDir: {sampleDir}",
            $"DebugDir: {debugDir}",
            $"Options: Profile={options.Profile}, MaxDimension={options.MaxDimension}, BudgetMilliseconds={options.BudgetMilliseconds}, AutoCrop={options.AutoCrop}, AggressiveSampling={options.AggressiveSampling}, StylizedSampling={options.StylizedSampling}, EnableTileScan={options.EnableTileScan}, TileGrid={options.TileGrid}",
            string.Empty
        };

        foreach (var fileName in HardArtFiles) {
            var path = Path.Combine(sampleDir, fileName);
            if (!File.Exists(path)) {
                lines.Add($"{fileName}: SKIP (missing)");
                continue;
            }

            var bytes = File.ReadAllBytes(path);
            if (!ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height)) {
                lines.Add($"{fileName}: FAIL (image decode)");
                continue;
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var okAll = QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var results, out var infoAll, options);
            if (!okAll || results.Length == 0) {
                var okSingle = QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out var single, out var infoSingle, options);
                if (okSingle) {
                    results = new[] { single };
                    lines.Add($"{fileName}: OK (single) {single.Text}");
                } else {
                    lines.Add($"{fileName}: FAIL {infoAll} | {infoSingle}");
                }
            } else {
                var payloads = results
                    .Select(r => r.Text)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                var payloadSummary = payloads.Length == 0 ? "(no text payloads)" : string.Join(" | ", payloads);
                lines.Add($"{fileName}: OK ({results.Length}) {payloadSummary}");
            }

            // Always emit debug renders for these hard-art samples.
            var debugOpts = new QrPixelDebugOptions();
            QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin.png", debugOpts);

            var adaptive = new QrPixelDebugOptions {
                AdaptiveThreshold = true,
                AdaptiveWindowSize = 15,
                AdaptiveOffset = 8
            };
            QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin-adaptive.png", adaptive);
            QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Heatmap, debugDir, $"{baseName}-heatmap.png", debugOpts);
        }

        var outputPath = Path.Combine(outputDir, "qr-hard-art-diagnostics.txt");
        File.WriteAllLines(outputPath, lines, Encoding.UTF8);
        Console.WriteLine(string.Join(Environment.NewLine, lines));
    }

    private static string ResolveSamplesDir(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (Directory.Exists(candidate)) {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate sample directory '{relativePath}'.");
    }
}
