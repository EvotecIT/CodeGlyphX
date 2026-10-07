using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrPatternWorkflowTests {
    [Fact]
    public void PatternSourceFeedsTheSameComposerAsDirectPatternRendering() {
        const string payload = "https://example.com/art";
        var pattern = QrArtPatternPresets.CreatePatternOptions(QrArtPattern.Circuit, 2026);
        var composition = QrArtPatternPresets.CreateCompositionOptions(pattern.Pattern);
        var png = QrArt.RenderPatternPng(pattern);
        var pixels = ImageReader.DecodeRgba32(png, out var width, out var height);
        Assert.Equal(512, width);
        Assert.Equal(512, height);
        var code = QR.Encode(payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var fromSource = QrImageComposer.Render(code, pixels, width, height, composition);
        Assert.Equal(QrArt.ComposePattern(payload, pattern, composition).GetPixels(), fromSource.GetPixels());
        Assert.Equal(png, QrArt.RenderPatternPng(pattern));
        pattern.Seed++;
        Assert.NotEqual(png, QrArt.RenderPatternPng(pattern));
    }

    [Fact]
    public void PresetsReturnIndependentEditablePalettesAndGeometry() {
        foreach (QrArtPattern pattern in Enum.GetValues(typeof(QrArtPattern))) {
            var original = QrArtPatternPresets.CreatePatternOptions(pattern, 17);
            var expected = (Rendering.Rgba32[])original.Colors.Clone();
            original.Colors[0] = Rendering.Rgba32.Black;
            var fresh = QrArtPatternPresets.CreatePatternOptions(pattern, 17);
            Assert.Equal(expected, fresh.Colors);
            Assert.Equal(17, fresh.Seed);
            Assert.Equal(pattern, fresh.Pattern);
            var edited = QrArtPatternPresets.CreateCompositionOptions(pattern);
            edited.Art!.Scale = 0.65;
            edited.Canvas!.PaddingModules = 0;
            var other = QrArtPatternPresets.CreateCompositionOptions(pattern);
            Assert.Equal(0.95, other.Art!.Scale);
            Assert.Equal(8, other.Canvas!.PaddingModules);
            Assert.Equal(18, other.ModuleSize);
        }
    }

    [Fact]
    public void PatternSourceRejectsUnboundedOutputAndHonorsCancellation() {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.RenderPatternPng(size: 0));
        Assert.Throws<ArgumentException>(() => QrArt.RenderPatternPng(size: int.MaxValue));
        Assert.Throws<ArgumentException>(() => QrArt.RenderPatternPng(new QrArtPatternOptions { Colors = [] }));
        Assert.Throws<OperationCanceledException>(() => QrArt.RenderPatternPng(cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArtPatternPresets.CreatePatternOptions((QrArtPattern)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArtPatternPresets.CreateCompositionOptions((QrArtPattern)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArtPatternPresets.CreateCompositionOptions(QrArtPattern.Marble, 0));
    }

#if !NET472
    [Fact]
    public async System.Threading.Tasks.Task PatternSourceCanBeSearchedAndItsSelectedExportRecoversThePayload() {
        const string payload = "https://example.com/art";
        var pattern = QrArtPatternPresets.CreatePatternOptions(QrArtPattern.Marble, 2026);
        var composition = QrArtPatternPresets.CreateCompositionOptions(pattern.Pattern);
        var source = QrArt.RenderPatternPng(pattern);
        var result = await QrArt.SearchImageAsync(payload, source, new QrImageSearchOptions {
            Composition = composition, IncludeQuartileErrorCorrection = false, AdditionalVersions = 0,
            ValidationCandidates = 1, Results = 1, DecodeBudgetMilliseconds = 3000
        });
        Assert.Equal(8, result.EvaluatedCandidates);
        var candidate = Assert.Single(result.Candidates);
        Assert.True(candidate.Validation.AllPassed);
        var pixels = ImageReader.DecodeRgba32(source, out var width, out var height);
        Assert.Equal(QrImageComposer.Render(candidate.Code, pixels, width, height, composition).GetPixels(), candidate.Image.GetPixels());
    }
#endif
}
