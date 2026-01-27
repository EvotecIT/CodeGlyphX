using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrDecodeSamplesExample {
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tga" };

    public static void Run(string outputDir) {
        var sampleDir = ResolveSamplesDir("Assets/DecodingSamples");
        var files = Directory.EnumerateFiles(sampleDir)
            .Where(path => ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2400,
            BudgetMilliseconds = 2500,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true
        };

        var lines = new List<string> {
            $"SampleDir: {sampleDir}",
            $"Files: {files.Length}",
            $"Options: Profile={options.Profile}, MaxDimension={options.MaxDimension}, BudgetMilliseconds={options.BudgetMilliseconds}, AggressiveSampling={options.AggressiveSampling}, StylizedSampling={options.StylizedSampling}, EnableTileScan={options.EnableTileScan}",
            string.Empty
        };
        var debugOutputs = ReadBoolEnv("CODEGLYPHX_DECODE_SAMPLES_DEBUG", false);
        var debugDir = Path.Combine(outputDir, "qr-debug");
        if (debugOutputs) {
            Directory.CreateDirectory(debugDir);
        }

        foreach (var file in files) {
            var bytes = File.ReadAllBytes(file);
            var name = Path.GetFileName(file);

            if (QrImageDecoder.TryDecodeAllImage(bytes, options, out var decoded) && decoded.Length > 0) {
                var texts = decoded
                    .Select(result => result.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                var payload = texts.Length == 0 ? "(no text payloads)" : string.Join(" | ", texts);
                lines.Add($"{name}: OK ({decoded.Length}) {payload}");
                continue;
            }

            if (QrImageDecoder.TryDecodeImage(bytes, out var single, out var info, options)) {
                lines.Add($"{name}: OK (single) {single.Text}");
            } else {
                lines.Add($"{name}: FAIL {info}");
                if (debugOutputs && ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height)) {
                    var baseName = Path.GetFileNameWithoutExtension(name);
                    var opts = new QrPixelDebugOptions();
                    QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin.png", opts);

                    var adaptive = new QrPixelDebugOptions {
                        AdaptiveThreshold = true,
                        AdaptiveWindowSize = 15,
                        AdaptiveOffset = 8
                    };
                    QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin-adaptive.png", adaptive);

                    QrPixelDebug.RenderToFile(rgba, width, height, width * 4, PixelFormat.Rgba32, QrPixelDebugMode.Heatmap, debugDir, $"{baseName}-heatmap.png", opts);
                }
            }
        }

        var outputPath = Path.Combine(outputDir, "qr-decode-samples.txt");
        File.WriteAllLines(outputPath, lines, Encoding.UTF8);
        Console.WriteLine(string.Join(Environment.NewLine, lines));
    }

    private static bool ReadBoolEnv(string name, bool fallback) {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
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
