using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ExternalDecodeSamplesTests {
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".bmp",
        ".tif",
        ".tiff",
        ".ppm",
        ".pgm",
        ".pbm",
        ".pam",
        ".tga",
        ".xpm",
        ".xbm",
        ".ico"
    };

    [Fact]
    public void Decode_ExternalSamples_EndToEnd() {
        var samplesDir = ResolveSamplesDirectory();
        if (samplesDir is null) {
            return;
        }

        var entries = Directory
            .EnumerateFiles(samplesDir, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedImage)
            .Select(path => (
                ImagePath: path,
                ExpectedPath: Path.ChangeExtension(path, ".txt"),
                KindPath: Path.ChangeExtension(path, ".kind"),
                TypePath: Path.ChangeExtension(path, ".type")))
            .Where(entry => File.Exists(entry.ExpectedPath))
            .OrderBy(entry => entry.ImagePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (entries.Length == 0) {
            return;
        }

        var qrOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            EnableTileScan = true,
            MaxDimension = 2000
        };

        foreach (var entry in entries) {
            var expectedTexts = ReadExpectedTexts(entry.ExpectedPath);
            var expectedKind = ReadExpectedKind(entry.KindPath);
            var expectedType = ReadExpectedBarcodeType(entry.TypePath);
            var preferBarcode = expectedKind == CodeGlyphKind.Barcode1D || expectedType.HasValue;

            var image = File.ReadAllBytes(entry.ImagePath);
            if (expectedTexts.Count == 1) {
                var expectedText = expectedTexts[0];
                Assert.True(
                    CodeGlyph.TryDecodeImage(image, out var decoded, expectedType, preferBarcode, qrOptions),
                    $"Failed to decode external sample: {entry.ImagePath}");

                Assert.Equal(expectedText, decoded.Text);
                if (expectedKind.HasValue) {
                    Assert.Equal(expectedKind.Value, decoded.Kind);
                }
                if (expectedType.HasValue) {
                    Assert.Equal(expectedType.Value, decoded.Barcode?.Type);
                }
            } else {
                Assert.True(
                    CodeGlyph.TryDecodeAllImage(image, out var decoded, expectedType, includeBarcode: true, preferBarcode, qrOptions),
                    $"Failed to decode external sample (multi): {entry.ImagePath}");

                var decodedTexts = decoded.Select(result => result.Text).Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal).ToArray();
                foreach (var expectedText in expectedTexts) {
                    Assert.Contains(expectedText, decodedTexts);
                }

                if (expectedKind.HasValue) {
                    Assert.All(decoded, result => Assert.Equal(expectedKind.Value, result.Kind));
                }
                if (expectedType.HasValue) {
                    Assert.All(decoded, result => Assert.Equal(expectedType.Value, result.Barcode?.Type));
                }
            }
        }
    }

    private static string? ResolveSamplesDirectory() {
        var envPath = Environment.GetEnvironmentVariable("CODEGLYPHX_EXTERNAL_SAMPLES");
        if (!string.IsNullOrWhiteSpace(envPath) && Directory.Exists(envPath)) {
            return envPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var nested = Path.Combine(dir.FullName, "CodeGlyphX.Tests", "Fixtures", "ExternalSamples");
            if (Directory.Exists(nested)) return nested;

            var direct = Path.Combine(dir.FullName, "Fixtures", "ExternalSamples");
            if (Directory.Exists(direct)) return direct;

            dir = dir.Parent;
        }

        return null;
    }

    private static bool IsSupportedImage(string path) {
        var ext = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(ext) && SupportedExtensions.Contains(ext);
    }

    private static List<string> ReadExpectedTexts(string path) {
        var lines = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        if (lines.Count == 0) {
            Assert.Fail($"Expected text file is empty: {path}");
        }
        return lines;
    }

    private static CodeGlyphKind? ReadExpectedKind(string path) {
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!Enum.TryParse<CodeGlyphKind>(text, ignoreCase: true, out var kind)) {
            Assert.Fail($"Invalid CodeGlyphKind '{text}' in {path}");
        }
        return kind;
    }

    private static BarcodeType? ReadExpectedBarcodeType(string path) {
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!Enum.TryParse<BarcodeType>(text, ignoreCase: true, out var type)) {
            Assert.Fail($"Invalid BarcodeType '{text}' in {path}");
        }
        return type;
    }
}
