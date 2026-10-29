using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CodeGlyphX.Examples;
using CodeGlyphX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

/// <summary>
/// The quality bar for the website QR gallery. Every variant of every look is rendered through the
/// generator's own code path for each matrix case (short and long URL, with and without a logo, small and
/// large size), then sampled plain, rotated by two degrees and blurred, and read by strict ZXing and the
/// CodeGlyphX decoder. A look stays in the gallery only while all of its checks pass. The badges on the
/// page come from Website/data/gallery-scan.json, which these tests compare with what they measure;
/// regenerate it with CODEGLYPHX_GALLERY_SCAN_WRITE=1.
/// </summary>
[Collection("ImageScannerSerial")]
public sealed class QrGalleryScanTests {
    internal const string ScanDataPath = "Website/data/gallery-scan.json";
    internal const string GalleryDataPath = "Website/data/gallery.json";
    private static readonly string[] Transforms = { "plain", "rotate2deg", "blur121" };
    private readonly ITestOutputHelper _output;

    public QrGalleryScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> VariantCases() => QrGalleryLooks.AllVariants.Select(v => new object[] { v.Id });

    internal sealed record VariantResult(string Id, int Checks, int Passed, int Zxing, int CodeGlyphX, IReadOnlyList<string> Failures);

    [Fact]
    public void Gallery_IsCurated_WithUniqueIdsAndPublishedRecipes() {
        var looks = QrGalleryLooks.All;
        Assert.InRange(looks.Count, 20, 30);
        Assert.Equal(looks.Count, looks.Select(l => l.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(looks.Count, looks.Select(l => l.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var variants = QrGalleryLooks.AllVariants.ToList();
        Assert.Equal(variants.Count, variants.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(looks, look => Assert.InRange(look.Variants.Count, 1, 2));

        // Every recipe block belongs to a variant, and every variant's published C# stands on its own.
        Assert.Equal(variants.Select(v => v.Id).OrderBy(id => id, StringComparer.Ordinal),
            QrGalleryLooks.AllRecipeBlocks.Keys.OrderBy(id => id, StringComparer.Ordinal));
        foreach (var variant in variants) {
            var snippet = QrGalleryLooks.Snippet(variant);
            Assert.Contains("var options = ", snippet, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"(?<![\w.])(R|Base|Hero|Theme|Palette|GradientCanvas)\("), snippet);
            Assert.DoesNotContain("QrGallery", snippet, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CommittedGalleryData_MatchesTheCuratedLooksAndRecipes() {
        var gallery = JsonNode.Parse(File.ReadAllText(RepoPath(GalleryDataPath)))!;
        var committedLooks = gallery["looks"]!.AsArray();
        Assert.Equal(QrGalleryLooks.All.Select(l => l.Id), committedLooks.Select(l => (string)l!["id"]!));
        foreach (var look in QrGalleryLooks.All) {
            var node = committedLooks.Single(l => (string)l!["id"]! == look.Id)!;
            var committedVariants = node["variants"]!.AsArray();
            Assert.Equal(look.Variants.Select(v => v.Id), committedVariants.Select(v => (string)v!["id"]!));
            foreach (var variant in look.Variants) {
                var v = committedVariants.Single(n => (string)n!["id"]! == variant.Id)!;
                Assert.Equal(QrGalleryLooks.Snippet(variant), (string)v["code"]!);
                var src = (string)v["src"]!;
                Assert.True(File.Exists(RepoPath("Website/static" + src)), "Missing gallery image " + src);
            }
        }
    }

    [Theory]
    [MemberData(nameof(VariantCases))]
    [Trait("Category", "CorpusTiming")]
    public void GalleryVariant_PassesEveryMatrixCase_AndMatchesItsPublishedBadge(string id) {
        var variant = QrGalleryLooks.AllVariants.Single(v => v.Id == id);
        var result = Measure(variant, _output);
        Assert.True(result.Failures.Count == 0, $"{id} failed {result.Failures.Count} of {result.Checks} checks:{Environment.NewLine}"
            + string.Join(Environment.NewLine, result.Failures));

        var published = JsonNode.Parse(File.ReadAllText(RepoPath(ScanDataPath)))!["variants"]!.AsArray()
            .SingleOrDefault(n => (string)n!["id"]! == id);
        Assert.True(published is not null, $"{ScanDataPath} has no result for {id}; regenerate it with CODEGLYPHX_GALLERY_SCAN_WRITE=1.");
        Assert.Equal(result.Checks, (int)published!["checks"]!);
        Assert.Equal(result.Passed, (int)published["passed"]!);
        Assert.Equal(result.Zxing, (int)published["zxing"]!);
        Assert.Equal(result.CodeGlyphX, (int)published["codeglyphx"]!);
    }

    [Fact]
    [Trait("Category", "CorpusTiming")]
    public void WriteMeasuredResults_WhenRequested() {
        if (Environment.GetEnvironmentVariable("CODEGLYPHX_GALLERY_SCAN_WRITE") != "1") return;
        var results = QrGalleryLooks.AllVariants.Select(v => Measure(v, _output)).ToList();
        var encoding = QrGalleryLooks.Encoding();
        var document = new {
            generatedBy = "CodeGlyphX.Tests/QrGalleryScanTests (CODEGLYPHX_GALLERY_SCAN_WRITE=1)",
            decoders = new[] { "ZXing.Net (strict: no TryHarder, no auto-rotate)", "CodeGlyphX QrImageDecoder" },
            payloads = QrGalleryLooks.MatrixPayloads.Select(p => new { name = p.Name, text = p.Text, version = QR.Encode(p.Text, encoding).Version }),
            logo = new[] { false, true },
            sizes = QrGalleryLooks.MatrixSizesPx,
            transforms = Transforms,
            checksPerVariant = QrGalleryLooks.Cases().Count() * Transforms.Length * 2,
            variants = results.Select(r => new { id = r.Id, checks = r.Checks, passed = r.Passed, zxing = r.Zxing, codeglyphx = r.CodeGlyphX, failures = r.Failures }),
        };
        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n";
        File.WriteAllText(RepoPath(ScanDataPath, mustExist: false), json, new UTF8Encoding(false));
    }

    internal static VariantResult Measure(QrGalleryLooks.Variant variant, ITestOutputHelper? output = null) {
        int checks = 0, zxing = 0, native = 0;
        var failures = new List<string>();
        foreach (var matrixCase in QrGalleryLooks.Cases()) {
            byte[] png;
            try {
                png = QrGalleryLooks.RenderPng(variant, matrixCase.Payload, matrixCase.Logo, matrixCase.SizePx);
            } catch (InvalidOperationException ex) {
                // RenderLogo refuses a placement it cannot validate; every check of that case counts as failed.
                checks += Transforms.Length * 2;
                failures.Add($"{matrixCase}: not rendered ({ex.Message})");
                continue;
            }
            Assert.True(ImageReader.TryDecodeRgba32(png, out _, out var width, out _));
            foreach (var observation in QrArtWave1ScanTests.ScanImageAt(png, matrixCase.Payload, width)) {
                checks += 2;
                if (observation.StrictPassed) zxing++; else failures.Add($"{matrixCase} {observation.Transform} ({width}px): ZXing");
                if (observation.LibraryPassed) native++; else failures.Add($"{matrixCase} {observation.Transform} ({width}px): CodeGlyphX");
            }
        }
        output?.WriteLine($"{variant.Id}: {zxing + native}/{checks} (ZXing {zxing}, CodeGlyphX {native})");
        foreach (var failure in failures) output?.WriteLine("  " + failure);
        return new VariantResult(variant.Id, checks, zxing + native, zxing, native, failures);
    }

    private static string RepoPath(string relativePath, bool mustExist = true, [CallerFilePath] string sourceFile = "") {
        var dir = FindRoot(AppContext.BaseDirectory) ?? FindRoot(Path.GetDirectoryName(sourceFile));
        if (dir is null) throw new DirectoryNotFoundException("Could not locate the repository root.");
        var path = Path.Combine(dir.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (mustExist) Assert.True(File.Exists(path), "Missing repository file: " + relativePath);
        return path;
    }

    private static DirectoryInfo? FindRoot(string? start) {
        var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CodeGlyphX.sln"))) dir = dir.Parent;
        return dir;
    }
}
