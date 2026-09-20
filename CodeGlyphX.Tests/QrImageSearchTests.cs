using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrImageSearchTests {
    [Fact]
    public void SearchEvaluatesEveryMaskAndReturnsMeasuredRankedAlternatives() {
        const string payload = "ART-SEARCH";
        var source = QrPngRenderer.Render(QR.Encode("SOURCE").Modules, new QrPngRenderOptions { ModuleSize = 6 });
        var options = new QrImageSearchOptions {
            AdditionalVersions = 0, IncludeQuartileErrorCorrection = false,
            ValidationCandidates = 2, Results = 2, DecodeBudgetMilliseconds = 3000,
            Composition = new QrImageCompositionOptions { ModuleSize = 6, Strength = 0.5, Art = new QrImageArtOptions() }
        };
        var result = QrArt.SearchImage(payload, source, options);
        Assert.Equal(8, result.EvaluatedCandidates);
        Assert.Equal(2, result.ValidatedCandidates);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(2, result.Candidates.Select(c => c.Code.Mask).Distinct().Count());
        Assert.All(result.Candidates, c => {
            Assert.InRange(c.Fidelity, 0, 100);
            Assert.True(c.Validation.AllPassed);
            Assert.All(c.Validation.Checks, check => Assert.Equal(payload, check.DecodedText));
        });
        Assert.True(result.Candidates[0].Fidelity >= result.Candidates[1].Fidelity);
        options.Results = 1;
        var single = QrArt.SearchImage(payload, source, options);
        Assert.Equal(2, single.ValidatedCandidates);
        var winner = Assert.Single(single.Candidates);
        Assert.Equal(result.Candidates[0].Code.Mask, winner.Code.Mask);
        Assert.Equal(result.Candidates[0].Image.GetPixels(), winner.Image.GetPixels());
    }

    [Fact]
    public void SearchRejectsBadBoundsAndHonorsCancellationBeforeImageDecode() {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.SearchImage("x", new byte[0], new QrImageSearchOptions { AdditionalVersions = 3 }));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => QrArt.SearchImage("x", new byte[0], cancellationToken: cancelled.Token));
        Assert.Throws<OperationCanceledException>(() => QrImageComposer.Render(QR.Encode("x"), new byte[4], 1, 1, null, cancelled.Token));
    }
}
