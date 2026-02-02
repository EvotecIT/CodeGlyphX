using System;
using System.IO;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrScreenshotDecodeTests {
    [Theory]
    [InlineData("qr-screenshot-1.png")]
    [InlineData("qr-screenshot-2.png")]
    [InlineData("qr-screenshot-3.png")]
    public void Decode_Screenshots(string fileName) {
        if (string.Equals(Environment.GetEnvironmentVariable("CODEGLYPHX_COVERAGE"), "1", StringComparison.OrdinalIgnoreCase)) {
            return;
        }
        var sampleDir = ResolveSamplesDir("Assets/DecodingSamples");
        var path = Path.Combine(sampleDir, fileName);
        var bytes = File.ReadAllBytes(path);
        var options = new QrPixelDecodeOptions {
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

        Assert.True(QrImageDecoder.TryDecodeAllImage(bytes, options, out var decoded), $"Decode failed for {fileName}.");
        Assert.NotEmpty(decoded);
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
