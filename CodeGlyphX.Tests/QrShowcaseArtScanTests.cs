using System.Runtime.CompilerServices;
using CodeGlyphX.Examples;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

/// <summary>The expressive art, procedural gallery tiles and Playground scene swatches published on the website are
/// rendered through their generators' own code paths, and the shipped files are checked as well. Each must decode its
/// exact payload with strict ZXing and the native decoder.</summary>
[Collection("ImageScannerSerial")]
public sealed class QrShowcaseArtScanTests {
    private const string ScenePayload = "https://example.com/scenes";
    private const int SceneSwatchSize = 400;
    private readonly ITestOutputHelper _output;

    public QrShowcaseArtScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> ExpressiveCases() => QrExpressiveArtExample.Scenes.Select(s => new object[] { s });
    public static IEnumerable<object[]> ProceduralCases() => Enumerable.Range(0, QrProceduralArtExample.Designs.Length).Select(i => new object[] { i });
    public static IEnumerable<object[]> SceneCases() => Enum.GetValues<QrSceneStyle>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(ExpressiveCases))]
    [Trait("Category", "CorpusTiming")]
    public void ExpressiveArt_GeneratorOutput_DecodesExactPayload(string scene) {
        var pixels = QrExpressiveArtExample.Source(scene, RepoPath("Assets/Art/apollo17-earth.jpg"), out var width, out var height);
        var png = QrExpressiveArtExample.Compose(scene, pixels, width, height).ToPng();
        AssertScans(png, QrExpressiveArtExample.Payload, scene);
    }

    [Theory]
    [MemberData(nameof(ExpressiveCases))]
    [Trait("Category", "CorpusTiming")]
    public void ExpressiveArt_ShippedFile_DecodesExactPayload(string scene) {
        var png = File.ReadAllBytes(RepoPath($"Website/static/images/art/qr-expressive-{scene}.png"));
        AssertScans(png, QrExpressiveArtExample.Payload, scene);
    }

    [Theory]
    [MemberData(nameof(ProceduralCases))]
    [Trait("Category", "CorpusTiming")]
    public void ProceduralGalleryTile_DecodesExactPayload_AtTileSize(int index) {
        var png = QrProceduralArtExample.Compose(index).ToPng();
        var name = QrProceduralArtExample.Designs[index].Name;
        AssertScans(png, QrProceduralArtExample.Payload, name, QrProceduralArtExample.GalleryTile);
    }

    [Theory]
    [MemberData(nameof(SceneCases))]
    [Trait("Category", "CorpusTiming")]
    public void PlaygroundSceneSwatch_PresetRender_AndShippedFile_DecodeExactPayload(QrSceneStyle style) {
        var rendered = QrArt.ComposeScene(ScenePayload, QrScenePresets.Create(style, SceneSwatchSize)).Image.ToPng();
        var shipped = File.ReadAllBytes(RepoPath($"CodeGlyphX.Playground/wwwroot/art/scenes/scene-{style.ToString().ToLowerInvariant()}.png"));
        foreach (var png in new[] { rendered, shipped }) {
            Assert.True(ImageReader.TryDecodeRgba32(png, out _, out var width, out _));
            Assert.Equal(SceneSwatchSize, width);
            AssertResults(QrArtWave1ScanTests.ScanImageAt(png, ScenePayload, SceneSwatchSize).ToList(), 3, style.ToString());
        }
    }

    // Standard sizes plus the image's own width (or the size it is shown at), each plain, rotated and blurred.
    private void AssertScans(byte[] png, string payload, string name, int? deliverySize = null) {
        Assert.True(ImageReader.TryDecodeRgba32(png, out _, out var width, out _));
        var results = QrArtWave1ScanTests.ScanImage(png, payload).ToList();
        results.AddRange(QrArtWave1ScanTests.ScanImageAt(png, payload, deliverySize ?? width));
        AssertResults(results, 12, name);
    }

    private void AssertResults(IReadOnlyList<QrArtWave1ScanTests.ScanObservation> results, int expected, string name) {
        foreach (var result in results) _output.WriteLine($"{name} {result}");
        Assert.Equal(expected, results.Count);
        var failures = results.Where(r => !r.StrictPassed || !r.LibraryPassed).ToArray();
        Assert.True(failures.Length == 0, $"{name}: " + string.Join(Environment.NewLine, failures.Select(r => r.ToString())));
    }

    // Build output normally sits inside the repository; with a redirected artifacts path the source location is used.
    private static string RepoPath(string relativePath, [CallerFilePath] string sourceFile = "") {
        var dir = FindRoot(AppContext.BaseDirectory) ?? FindRoot(Path.GetDirectoryName(sourceFile));
        if (dir is null) throw new DirectoryNotFoundException("Could not locate the repository root.");
        var path = Path.Combine(dir.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), "Missing repository file: " + relativePath);
        return path;
    }

    private static DirectoryInfo? FindRoot(string? start) {
        var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CodeGlyphX.sln"))) dir = dir.Parent;
        return dir;
    }
}
