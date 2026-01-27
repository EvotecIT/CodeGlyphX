using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngSuiteDecodeTests {
    [Fact]
    public void Decode_PngSuite_Samples() {
        var samplesDir = ResolveSamplesDir("CodeGlyphX.Tests/Fixtures/ImageSamples");
        var files = Directory.EnumerateFiles(samplesDir, "pngsuite-*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(files);

        foreach (var file in files) {
            var bytes = File.ReadAllBytes(file);
            var rgba = PngReader.DecodeRgba32(bytes, out var width, out var height);
            Assert.True(width > 0 && height > 0, $"Invalid size for {file}.");
            Assert.Equal(width * height * 4, rgba.Length);
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
