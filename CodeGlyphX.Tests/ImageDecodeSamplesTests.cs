using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CodeGlyphX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

public sealed class ImageDecodeSamplesTests {
    private readonly ITestOutputHelper _output;

    public ImageDecodeSamplesTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Decode_ImageSamples_EndToEnd() {
        var samplesDir = ResolveSamplesDirectory();
        if (samplesDir is null) {
            return;
        }

        var entries = LoadSamples(samplesDir);
        if (entries.Count == 0) {
            return;
        }

        var anyPresent = false;
        foreach (var entry in entries) {
            if (File.Exists(entry.ImagePath)) {
                anyPresent = true;
                break;
            }
        }
        if (!anyPresent) {
            return;
        }

        foreach (var entry in entries) {
            if (!File.Exists(entry.ImagePath)) {
                if (entry.Required) {
                    Assert.Fail($"Missing image sample: {entry.ImagePath}");
                } else {
                    _output.WriteLine($"Optional sample missing: {entry.ImagePath}");
                    continue;
                }
            }
            try {
                var data = File.ReadAllBytes(entry.ImagePath);
                var infoAvailable = ImageReader.TryReadInfo(data, out var info);

                if (infoAvailable) {
                    if (entry.ExpectedFormat.HasValue) {
                        Assert.Equal(entry.ExpectedFormat.Value, info.Format);
                    }
                    if (entry.ExpectedWidth.HasValue) {
                        Assert.Equal(entry.ExpectedWidth.Value, info.Width);
                    }
                    if (entry.ExpectedHeight.HasValue) {
                        Assert.Equal(entry.ExpectedHeight.Value, info.Height);
                    }
                } else if (entry.Required) {
                    Assert.Fail($"Failed to read image info: {entry.ImagePath}");
                }

                if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) {
                    if (entry.Required) {
                        Assert.Fail($"Failed to decode image sample: {entry.ImagePath}");
                    } else {
                        _output.WriteLine($"Optional sample failed to decode: {entry.ImagePath}");
                        continue;
                    }
                }

                if (entry.ExpectedWidth.HasValue) {
                    Assert.Equal(entry.ExpectedWidth.Value, width);
                }
                if (entry.ExpectedHeight.HasValue) {
                    Assert.Equal(entry.ExpectedHeight.Value, height);
                }
                if (infoAvailable) {
                    Assert.Equal(info.Width, width);
                    Assert.Equal(info.Height, height);
                }

                Assert.True(width > 0 && height > 0);
                Assert.Equal(checked(width * height * 4), rgba.Length);
            } catch (Exception ex) {
                if (entry.Required) {
                    Assert.Fail($"Exception decoding sample '{entry.Id}' at {entry.ImagePath}: {ex.GetType().Name} {ex.Message}");
                } else {
                    _output.WriteLine($"Optional sample threw: {entry.ImagePath} ({ex.GetType().Name}: {ex.Message})");
                }
            }
        }
    }

    private static string? ResolveSamplesDirectory() {
        var envPath = Environment.GetEnvironmentVariable("CODEGLYPHX_IMAGE_SAMPLES");
        if (!string.IsNullOrWhiteSpace(envPath) && Directory.Exists(envPath)) {
            return envPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var nested = Path.Combine(dir.FullName, "CodeGlyphX.Tests", "Fixtures", "ImageSamples");
            if (Directory.Exists(nested)) return nested;

            var direct = Path.Combine(dir.FullName, "Fixtures", "ImageSamples");
            if (Directory.Exists(direct)) return direct;

            dir = dir.Parent;
        }

        return null;
    }

    private static List<SampleEntry> LoadSamples(string samplesDir) {
        var manifestPath = Path.Combine(samplesDir, "manifest.json");
        if (!File.Exists(manifestPath)) return new List<SampleEntry>();

        try {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ImageSamplesManifest>(json, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });
            if (manifest?.Entries is null || manifest.Entries.Count == 0) return new List<SampleEntry>();

            var entries = new List<SampleEntry>(manifest.Entries.Count);
            foreach (var entry in manifest.Entries) {
                if (string.IsNullOrWhiteSpace(entry.FileName)) continue;

                ImageFormat? format = null;
                if (!string.IsNullOrWhiteSpace(entry.Format)) {
                    if (!Enum.TryParse(entry.Format, true, out ImageFormat parsedFormat)) {
                        Assert.Fail($"Invalid ImageFormat '{entry.Format}' in manifest entry '{entry.Id}'.");
                    }
                    format = parsedFormat;
                }

                var imagePath = Path.Combine(samplesDir, entry.FileName);
                entries.Add(new SampleEntry(
                    entry.Id ?? entry.FileName,
                    imagePath,
                    format,
                    entry.Width,
                    entry.Height,
                    entry.Required ?? true));
            }

            return entries;
        } catch {
            return new List<SampleEntry>();
        }
    }

    private sealed record SampleEntry(
        string Id,
        string ImagePath,
        ImageFormat? ExpectedFormat,
        int? ExpectedWidth,
        int? ExpectedHeight,
        bool Required);

    private sealed class ImageSamplesManifest {
        public List<ImageSamplesEntry> Entries { get; set; } = new();
    }

    private sealed class ImageSamplesEntry {
        public string? Id { get; set; }
        public string? FileName { get; set; }
        public string? Format { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? Required { get; set; }
    }
}
