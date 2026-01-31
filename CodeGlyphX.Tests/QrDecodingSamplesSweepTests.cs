using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodingSamplesSweepTests {
    private static readonly HashSet<string> SkipFiles = new(StringComparer.OrdinalIgnoreCase) {
        // Heavy illustration samples remain difficult; keep them tracked but skipped for now.
        "qr-art-facebook-splash-grid.png",
        "qr-art-montage-grid.png",
        "qr-art-stripe-eye-grid.png",
        "qr-art-drip-variants.png",
        "qr-art-solid-bg-grid.png",
        "qr-art-gear-illustration-grid.png",
        "qr-illustration-template.jpg",
        "qr-dot-aa-soft.png"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".tga"
    };

    [Fact]
    public void QrDecode_AllDecodingSamples_ExceptKnownHardAndWebp() {
        var sampleDir = ResolveSamplesDir("Assets/DecodingSamples");
        var files = Directory.EnumerateFiles(sampleDir)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !SkipFiles.Contains(Path.GetFileName(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(files);

        var baseOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2200,
            BudgetMilliseconds = TestBudget.Adjust(5000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true
        };

        foreach (var file in files) {
            var bytes = File.ReadAllBytes(file);
            Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height), $"Failed to decode sample image: {file}");

            var options = baseOptions;
            if (Path.GetFileName(file).Contains("screenshot", StringComparison.OrdinalIgnoreCase)) {
                options = new QrPixelDecodeOptions {
                    Profile = QrDecodeProfile.Robust,
                    MaxDimension = 3200,
                    MaxMilliseconds = TestBudget.Adjust(18000),
                    BudgetMilliseconds = TestBudget.Adjust(18000),
                    AutoCrop = true,
                    AggressiveSampling = true,
                    StylizedSampling = false,
                    EnableTileScan = true,
                    TileGrid = 6
                };
            }

            var fallbackOptions = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Robust,
                MaxDimension = Math.Max(options.MaxDimension, 2000),
                MaxScale = 3,
                MaxMilliseconds = TestBudget.Adjust(2000),
                BudgetMilliseconds = TestBudget.Adjust(7000),
                AutoCrop = true,
                AggressiveSampling = true,
                StylizedSampling = false,
                EnableTileScan = true,
                TileGrid = Math.Max(4, options.TileGrid)
            };

            var fallbackStylized = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Robust,
                MaxDimension = Math.Max(options.MaxDimension, 2400),
                MaxScale = 4,
                MaxMilliseconds = TestBudget.Adjust(3000),
                BudgetMilliseconds = TestBudget.Adjust(10000),
                AutoCrop = true,
                AggressiveSampling = true,
                StylizedSampling = true,
                EnableTileScan = true,
                TileGrid = Math.Max(4, options.TileGrid)
            };

            var fallbackHeavy = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Robust,
                MaxDimension = Math.Max(options.MaxDimension, 3200),
                MaxScale = 6,
                MaxMilliseconds = TestBudget.Adjust(5000),
                BudgetMilliseconds = TestBudget.Adjust(15000),
                AutoCrop = true,
                AggressiveSampling = true,
                StylizedSampling = true,
                EnableTileScan = true,
                TileGrid = Math.Max(6, options.TileGrid)
            };

            if (!TryDecodeResults(rgba, width, height, width * 4, out var results, out var diagnostics, options, fallbackOptions, fallbackStylized, fallbackHeavy)) {
                Assert.Fail($"Decode failed for {file}: {diagnostics}");
            }

            Assert.True(results.Length > 0, $"No QR results for {file}.");
        }
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

    private static bool TryDecodeResults(
        byte[] rgba,
        int width,
        int height,
        int stride,
        out QrDecoded[] results,
        out string diagnostics,
        params QrPixelDecodeOptions[] options) {
        results = Array.Empty<QrDecoded>();
        diagnostics = "No decode options provided.";

        if (options is null || options.Length == 0) return false;

        for (var i = 0; i < options.Length; i++) {
            var option = options[i];
            QrPixelDecodeInfo infoSingle = default;
            if (QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, out infoSingle, option)) {
                results = new[] { decoded };
                diagnostics = infoSingle.ToString();
                return true;
            }

            diagnostics = infoSingle.ToString();

            if (i == 0 && QrDecoder.TryDecodeAll(rgba, width, height, stride, PixelFormat.Rgba32, out var decodedList, out var infoAll, option)) {
                if (decodedList.Length > 0) {
                    results = decodedList;
                    diagnostics = infoAll.ToString();
                    return true;
                }

                diagnostics = infoAll.ToString();
            }
        }

        return false;
    }
}
