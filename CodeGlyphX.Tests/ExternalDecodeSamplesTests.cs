using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

public sealed class ExternalDecodeSamplesTests {
    private readonly ITestOutputHelper _output;

    public ExternalDecodeSamplesTests(ITestOutputHelper output) {
        _output = output;
    }

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
            Assert.Fail("External sample directory is missing.");
        }

        var entries = LoadSamples(samplesDir);
        if (entries.Count == 0) {
            Assert.Fail("External sample manifest contains no entries.");
        }

        foreach (var entry in entries) {
            if (!File.Exists(entry.ImagePath)) {
                if (entry.Required) Assert.Fail($"Missing external sample: {entry.ImagePath}");
                _output.WriteLine($"Optional sample missing: {entry.ImagePath}");
                continue;
            }
            var image = File.ReadAllBytes(entry.ImagePath);
            var formats = ResolveFormats(entry.ExpectedKind, entry.ExpectedBarcodeType);
            var result = SymbolScanner.Scan(image, new ScanOptions {
                Formats = formats, MaxSymbols = entry.ExpectedTexts.Count == 1 ? 1 : 32,
                TimeoutMilliseconds = TestBudget.Adjust(12000),
                Qr = new QrPixelDecodeOptions {
                    Profile = QrDecodeProfile.Robust, AggressiveSampling = true, EnableTileScan = true,
                    MaxDimension = 2000, BudgetMilliseconds = 6000
                },
                Image = new ImageDecodeOptions { MaxDimension = 2000 },
                Barcode = new BarcodeDecodeOptions { EnableTileScan = true }
            });
            if (!result.IsSuccess) {
                var failure = $"{entry.ImagePath}: {result.Status}/{result.CompletionReason}: {result.Failure}";
                if (entry.Required) Assert.Fail($"Failed to decode external sample: {failure}");
                _output.WriteLine($"Optional sample failed to decode: {failure}");
                continue;
            }
            var texts = result.Symbols.Select(symbol => symbol.Text).Distinct(StringComparer.Ordinal).ToArray();
            foreach (var expected in entry.ExpectedTexts) {
                if (entry.Required) Assert.Contains(expected, texts);
                else if (!texts.Contains(expected, StringComparer.Ordinal))
                    _output.WriteLine($"Optional sample mismatch: {entry.ImagePath} missing '{expected}'.");
            }
            if (entry.Required && formats is not null) {
                Assert.All(result.Symbols, symbol => Assert.Contains(symbol.Format, formats));
            }
        }
    }

    // Existing corpus manifests describe broad families. Translate fixture metadata at this test boundary.
    private static SymbolFormat[]? ResolveFormats(string? kind, BarcodeType? barcodeType) {
        if (barcodeType.HasValue) {
            var match = SymbolCapabilities.All.Single(capability => capability.LegacyBarcodeType == barcodeType.Value);
            return new[] { match.Format };
        }
        return kind?.ToLowerInvariant() switch {
            null => null,
            "qr" => new[] { SymbolFormat.QrCode },
            "microqr" => new[] { SymbolFormat.MicroQrCode },
            "datamatrix" => new[] { SymbolFormat.DataMatrix },
            "pdf417" => new[] { SymbolFormat.Pdf417 },
            "aztec" => new[] { SymbolFormat.Aztec },
            "barcode1d" => SymbolCapabilities.All.Where(capability => capability.Family == SymbolFamily.Linear && capability.CanScanImages)
                .Select(capability => capability.Format).ToArray(),
            _ => throw new InvalidDataException($"Unknown fixture family '{kind}'.")
        };
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

    private static List<SampleEntry> LoadSamples(string samplesDir) {
        var manifestPath = Path.Combine(samplesDir, "manifest.json");
        if (File.Exists(manifestPath)) {
            var manifestEntries = TryLoadManifest(manifestPath, samplesDir);
            if (manifestEntries.Count > 0) {
                return manifestEntries;
            }
        }

        return Directory
            .EnumerateFiles(samplesDir, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedImage)
            .Select(path => {
                var expectedPath = Path.ChangeExtension(path, ".txt");
                var kindPath = Path.ChangeExtension(path, ".kind");
                var typePath = Path.ChangeExtension(path, ".type");
                if (!File.Exists(expectedPath)) return null;
                return new SampleEntry(
                    path,
                    ReadExpectedTexts(expectedPath),
                    ReadExpectedKind(kindPath),
                    ReadExpectedBarcodeType(typePath),
                    Required: true);
            })
            .Where(entry => entry is not null)
            .OrderBy(entry => entry!.ImagePath, StringComparer.OrdinalIgnoreCase)
            .Cast<SampleEntry>()
            .ToList();
    }

    private static List<SampleEntry> TryLoadManifest(string manifestPath, string samplesDir) {
        try {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ExternalSamplesManifest>(json, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });
            if (manifest?.Entries is null || manifest.Entries.Count == 0) return new List<SampleEntry>();

            var entries = new List<SampleEntry>(manifest.Entries.Count);
            foreach (var entry in manifest.Entries) {
                if (string.IsNullOrWhiteSpace(entry.FileName)) continue;
                var expected = entry.ExpectedTexts ?? (entry.ExpectedText is null ? null : new List<string> { entry.ExpectedText });
                if (expected is null || expected.Count == 0) continue;

                var kind = entry.Kind;
                if (!string.IsNullOrWhiteSpace(kind)) ResolveFormats(kind, null);
                else kind = null;

                BarcodeType? barcodeType = null;
                if (!string.IsNullOrWhiteSpace(entry.BarcodeType)) {
                    if (!Enum.TryParse(entry.BarcodeType, true, out BarcodeType parsedType)) {
                        Assert.Fail($"Invalid BarcodeType '{entry.BarcodeType}' in manifest entry '{entry.Id}'.");
                    }
                    barcodeType = parsedType;
                }

                var imagePath = Path.Combine(samplesDir, entry.FileName);
                entries.Add(new SampleEntry(imagePath, expected, kind, barcodeType, entry.Required ?? true));
            }
            return entries;
        } catch {
            return new List<SampleEntry>();
        }
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

    private static string? ReadExpectedKind(string path) {
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        ResolveFormats(text, null);
        return text;
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

    private sealed record SampleEntry(
        string ImagePath,
        List<string> ExpectedTexts,
        string? ExpectedKind,
        BarcodeType? ExpectedBarcodeType,
        bool Required);

    private sealed class ExternalSamplesManifest {
        public List<ExternalSamplesEntry> Entries { get; set; } = new();
    }

    private sealed class ExternalSamplesEntry {
        public string? Id { get; set; }
        public string? FileName { get; set; }
        public List<string>? ExpectedTexts { get; set; }
        public string? ExpectedText { get; set; }
        public string? Kind { get; set; }
        public string? BarcodeType { get; set; }
        public bool? Required { get; set; }
    }
}
