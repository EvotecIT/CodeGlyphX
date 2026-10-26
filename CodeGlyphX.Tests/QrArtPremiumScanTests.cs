using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrArtPremiumScanTests {
    internal const string ShortUrl = "https://evotec.xyz";
    internal const string LongUrl = "https://evotec.xyz/products/codeglyphx/docs/getting-started?utm_source=print&utm_medium=flyer&utm_campaign=autumn-2026&ref=gallery-audit";
    private readonly ITestOutputHelper _output;
    public QrArtPremiumScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> HeroCases() {
        foreach (var hero in Enum.GetValues<QrHeroPreset>()) foreach (var dark in new[] { false, true })
            foreach (var longer in new[] { false, true }) foreach (var logo in new[] { false, true })
                yield return new object[] { hero, dark, longer, logo };
    }
    public static IEnumerable<object[]> FrameCases() {
        foreach (var template in Enum.GetValues<QrCanvasTemplate>().Where(t => t != QrCanvasTemplate.None))
            foreach (var position in Enum.GetValues<QrCanvasCaptionPosition>()) foreach (var longCaption in new[] { false, true })
                foreach (var dark in new[] { false, true }) foreach (var longer in new[] { false, true }) foreach (var logo in new[] { false, true })
                    yield return new object[] { template, position, longCaption, dark, longer, logo };
    }

    [Theory, MemberData(nameof(HeroCases)), Trait("Category", "CorpusTiming")]
    public void Heroes_PassExactPayloadsWithAndWithoutLogo_AtEveryDeliverySizeAndTransform(QrHeroPreset hero, bool dark, bool longer, bool logo) =>
        AssertGate($"hero-{hero}-{dark}-{longer}-{logo}", QrArt.Hero(hero, dark), longer ? LongUrl : ShortUrl, logo);

    [Theory, MemberData(nameof(FrameCases)), Trait("Category", "CorpusTiming")]
    public void Frames_PassBothCaptionPositionsAndLengths_WithEveryPayloadAndLogo(QrCanvasTemplate template,
        QrCanvasCaptionPosition position, bool longCaption, bool dark, bool longer, bool logo) {
        var options = QrArt.Hero(QrHeroPreset.Ink);
        options.Canvas = new QrCanvasOptions { Background = dark ? new Rgba32(20, 22, 30) : Rgba32.White,
            Template = template, Caption = new QrCanvasCaptionOptions {
                Position = position, Text = longCaption ? "Scan to explore the collection" : "SCAN ME" } };
        AssertGate($"frame-{template}-{position}-{longCaption}-{dark}-{longer}-{logo}", options, longer ? LongUrl : ShortUrl, logo);
    }

    private void AssertGate(string key, QrRenderOptions options, string payload, bool withLogo) {
        options.TargetSizePx = 1024;
        if (withLogo) options.LogoPng = Logo();
        var png = QR.Render(payload, OutputFormat.Png, options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var results = QrArtWave1ScanTests.ScanImage(png, payload).ToArray();
        Assert.Equal(9, results.Length);
        foreach (var result in results) _output.WriteLine(result.ToString());
        var evidence = Environment.GetEnvironmentVariable("CODEGLYPHX_PREMIUM_EVIDENCE");
        if (!string.IsNullOrEmpty(evidence)) {
            Directory.CreateDirectory(evidence);
            File.WriteAllBytes(Path.Combine(evidence, key + ".png"), png);
            File.WriteAllLines(Path.Combine(evidence, key + ".tsv"), results.Select(r => $"{r.Size}\t{r.Transform}\t{r.StrictPassed}\t{r.LibraryPassed}"));
        }
        Assert.True(results.All(r => r.StrictPassed && r.LibraryPassed), key + Environment.NewLine + string.Join(Environment.NewLine, results.Where(r => !r.StrictPassed || !r.LibraryPassed)));
    }

    internal static byte[] Logo() {
        var rgba = new byte[32 * 32 * 4];
        for (var y = 4; y < 28; y++) for (var x = 4; x < 28; x++) {
            if (x >= 10 && y >= 10 && y < 14 || x >= 10 && y >= 18 && y < 22 || x < 10) {
                var p = (y * 32 + x) * 4; rgba[p] = 80; rgba[p + 1] = 20; rgba[p + 2] = 40; rgba[p + 3] = 255;
            }
        }
        return PngImageEncoder.EncodeRgba32(rgba, 32, 32);
    }
}
