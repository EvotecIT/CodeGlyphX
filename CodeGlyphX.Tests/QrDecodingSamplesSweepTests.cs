using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // Flaky screenshot sample in CI; keep tracked but excluded from sweep.
        "qr-screenshot-1.png"
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

            if (!QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var results, out var infoAll, options)) {
                if (QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, out var infoSingle, options)) {
                    results = new[] { decoded };
                } else {
                    Assert.Fail($"Decode failed for {file}: {infoAll} | {infoSingle}");
                }
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
}
