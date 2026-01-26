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
            return;
        }

        var entries = LoadSamples(samplesDir);
        if (entries.Count == 0) {
            return;
        }

        foreach (var entry in entries) {
            var expectedTexts = entry.ExpectedTexts;
            var expectedKind = entry.ExpectedKind;
            var expectedType = entry.ExpectedBarcodeType;
            var preferBarcode = expectedKind == CodeGlyphKind.Barcode1D || expectedType.HasValue;

            if (!File.Exists(entry.ImagePath)) {
                if (entry.Required) {
                    Assert.Fail($"Missing external sample: {entry.ImagePath}");
                } else {
                    _output.WriteLine($"Optional sample missing: {entry.ImagePath}");
                    continue;
                }
            }

            var image = File.ReadAllBytes(entry.ImagePath);
            var options = new CodeGlyphDecodeOptions {
                ExpectedBarcode = expectedType,
                PreferBarcode = preferBarcode,
                IncludeBarcode = true,
                Qr = new QrPixelDecodeOptions {
                    Profile = QrDecodeProfile.Robust,
                    AggressiveSampling = true,
                    EnableTileScan = true,
                    MaxDimension = 2000,
                    MaxMilliseconds = 4000,
                    BudgetMilliseconds = 6000
                },
                Image = new ImageDecodeOptions {
                    MaxDimension = 2000,
                    MaxMilliseconds = 4000
                },
                Barcode = new BarcodeDecodeOptions {
                    EnableTileScan = true,
                    TileGrid = 0
                }
            };

            if (expectedTexts.Count == 1) {
                var expectedText = expectedTexts[0];
                string decodedText;
                string diag;
                CodeGlyphKind? actualKind = null;
                BarcodeType? actualBarcodeType = null;
                bool ok;

                if (expectedKind.HasValue) {
                    ok = TryDecodeByKind(image, expectedKind.Value, expectedType, options, out decodedText, out diag, out actualBarcodeType);
                    actualKind = expectedKind.Value;
                } else {
                    ok = CodeGlyph.TryDecodeImage(image, out var decoded, options);
                    decodedText = ok ? decoded.Text : string.Empty;
                    diag = ok ? "auto-decode ok" : "auto-decode failed";
                    actualKind = ok ? decoded.Kind : null;
                    actualBarcodeType = ok ? decoded.Barcode?.Type : null;
                }

                if (!ok) {
                    if (entry.Required) {
                        Assert.Fail($"Failed to decode external sample: {entry.ImagePath} ({diag})");
                    }
                    _output.WriteLine($"Optional sample failed to decode: {entry.ImagePath} ({diag})");
                    continue;
                }

                if (entry.Required) {
                    Assert.Equal(expectedText, decodedText);
                    if (expectedKind.HasValue) {
                        Assert.Equal(expectedKind.Value, actualKind);
                    }
                    if (expectedType.HasValue) {
                        Assert.Equal(expectedType.Value, actualBarcodeType);
                    }
                } else if (!string.Equals(expectedText, decodedText, StringComparison.Ordinal)) {
                    _output.WriteLine($"Optional sample mismatch: {entry.ImagePath} expected '{expectedText}', got '{decodedText}'.");
                }
            } else {
                if (!CodeGlyph.TryDecodeAllImage(image, out var decoded, options)) {
                    if (entry.Required) {
                        Assert.Fail($"Failed to decode external sample (multi): {entry.ImagePath}");
                    }
                    _output.WriteLine($"Optional sample failed to decode (multi): {entry.ImagePath}");
                    continue;
                }

                var decodedTexts = decoded.Select(result => result.Text).Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal).ToArray();
                if (entry.Required) {
                    foreach (var expectedText in expectedTexts) {
                        Assert.Contains(expectedText, decodedTexts);
                    }

                    if (expectedKind.HasValue) {
                        Assert.All(decoded, result => Assert.Equal(expectedKind.Value, result.Kind));
                    }
                    if (expectedType.HasValue) {
                        Assert.All(decoded, result => Assert.Equal(expectedType.Value, result.Barcode?.Type));
                    }
                } else {
                    foreach (var expectedText in expectedTexts) {
                        if (!decodedTexts.Contains(expectedText, StringComparer.Ordinal)) {
                            _output.WriteLine($"Optional sample mismatch: {entry.ImagePath} missing '{expectedText}'.");
                        }
                    }
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

                CodeGlyphKind? kind = null;
                if (!string.IsNullOrWhiteSpace(entry.Kind)) {
                    if (!Enum.TryParse(entry.Kind, true, out CodeGlyphKind parsedKind)) {
                        Assert.Fail($"Invalid CodeGlyphKind '{entry.Kind}' in manifest entry '{entry.Id}'.");
                    }
                    kind = parsedKind;
                }

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

    private static bool TryDecodeByKind(
        byte[] image,
        CodeGlyphKind kind,
        BarcodeType? expectedType,
        CodeGlyphDecodeOptions options,
        out string text,
        out string diagnostics,
        out BarcodeType? actualBarcodeType) {
        text = string.Empty;
        diagnostics = string.Empty;
        actualBarcodeType = null;

        switch (kind) {
            case CodeGlyphKind.Qr:
                if (!ImageReader.TryDecodeRgba32(image, out var qrRgba, out var qrW, out var qrH)) {
                    diagnostics = "image decode failed";
                    return false;
                }
                if (QrDecoder.TryDecode(qrRgba, qrW, qrH, qrW * 4, PixelFormat.Rgba32, out var qrDecoded, out var qrInfo, options.Qr)) {
                    text = qrDecoded.Text;
                    diagnostics = qrInfo.ToString();
                    return true;
                }
                diagnostics = qrInfo.ToString();
                return false;

            case CodeGlyphKind.DataMatrix:
                if (DataMatrixCode.TryDecodeImage(image, options.Image, out var dmText, out var dmDiag)) {
                    text = dmText;
                    diagnostics = Format(dmDiag);
                    return true;
                }
                diagnostics = Format(dmDiag);
                return false;

            case CodeGlyphKind.Pdf417:
                if (Pdf417Code.TryDecodeImage(image, options.Image, out var pdfText, out var pdfDiag)) {
                    text = pdfText;
                    diagnostics = Format(pdfDiag);
                    return true;
                }
                diagnostics = Format(pdfDiag);
                return false;

            case CodeGlyphKind.Aztec:
                if (AztecCode.TryDecodeImage(image, options.Image, out var azText, out var azDiag)) {
                    text = azText;
                    diagnostics = Format(azDiag);
                    return true;
                }
                diagnostics = Format(azDiag);
                return false;

            case CodeGlyphKind.Barcode1D:
                if (!ImageReader.TryDecodeRgba32(image, out var barRgba, out var barW, out var barH)) {
                    diagnostics = "image decode failed";
                    return false;
                }
                if (BarcodeDecoder.TryDecode(barRgba, barW, barH, barW * 4, PixelFormat.Rgba32, expectedType, options.Barcode, options.CancellationToken, out var barcode, out var barDiag)) {
                    text = barcode.Text;
                    actualBarcodeType = barcode.Type;
                    diagnostics = Format(barDiag);
                    return true;
                }
                if (options.Barcode?.EnableTileScan == true
                    && BarcodeDecoder.TryDecodeAll(barRgba, barW, barH, barW * 4, PixelFormat.Rgba32, out var allHits, expectedType, options.Barcode, options.CancellationToken)) {
                    BarcodeDecoded? hit = null;
                    if (expectedType.HasValue) {
                        hit = allHits.FirstOrDefault(candidate => candidate.Type == expectedType.Value);
                    }
                    hit ??= allHits.FirstOrDefault();
                    if (hit is not null) {
                        text = hit.Text;
                        actualBarcodeType = hit.Type;
                        diagnostics = "tile scan";
                        return true;
                    }
                }
                diagnostics = Format(barDiag);
                return false;

            default:
                diagnostics = "unsupported kind";
                return false;
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

    private static string Format(DataMatrixDecodeDiagnostics diagnostics) {
        return $"attempts={diagnostics.AttemptCount} mirrored={diagnostics.MirroredTried} success={diagnostics.Success} failure={diagnostics.Failure}";
    }

    private static string Format(Pdf417DecodeDiagnostics diagnostics) {
        return $"attempts={diagnostics.AttemptCount} startCandidates={diagnostics.StartPatternCandidates} startAttempts={diagnostics.StartPatternAttempts} mirrored={diagnostics.MirroredTried} success={diagnostics.Success} failure={diagnostics.Failure}";
    }

    private static string Format(AztecDecodeDiagnostics diagnostics) {
        return $"attempts={diagnostics.AttemptCount} inverted={diagnostics.InvertedTried} mirrored={diagnostics.MirroredTried} success={diagnostics.Success} failure={diagnostics.Failure}";
    }

    private static string Format(BarcodeDecodeDiagnostics diagnostics) {
        return $"candidates={diagnostics.CandidateCount} attempts={diagnostics.AttemptCount} inverted={diagnostics.InvertedTried} reversed={diagnostics.ReversedTried} success={diagnostics.Success} failure={diagnostics.Failure}";
    }

    private sealed record SampleEntry(
        string ImagePath,
        List<string> ExpectedTexts,
        CodeGlyphKind? ExpectedKind,
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
