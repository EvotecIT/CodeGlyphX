using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Art;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrIllustratedCompositionTests {
    [Theory]
    [InlineData(QrIllustratedStyle.EngravedPortrait)]
    [InlineData(QrIllustratedStyle.BotanicalBadge)]
    [InlineData(QrIllustratedStyle.GeometricPoster)]
    public void FramePreservesQuietZoneAndQrAndExportsSelfContainedSvg(QrIllustratedStyle style) {
        const string payload = "ILLUSTRATED-QR";
        var qr = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var options = QrIllustratedComposer.CreateOptions(style, 16);
        var original = QrImageComposer.Render(qr, new byte[] { 170, 150, 90, 255 }, 1, 1, options);
        var framed = QrIllustratedComposer.Frame(original, style);
        var before = original.GetPixels(); var after = framed.Image.GetPixels();
        for (var y = original.QrOffsetY; y < original.QrOffsetY + original.QrSize; y++) {
            var offset = (y * original.Size + original.QrOffsetX) * 4;
            Assert.Equal(before.AsSpan(offset, original.QrSize * 4).ToArray(), after.AsSpan(offset, original.QrSize * 4).ToArray());
        }
        var svg = XDocument.Parse(framed.ToSvg());
        XNamespace ns = "http://www.w3.org/2000/svg";
        var embedded = (string)svg.Root!.Element(ns + "image")!.Attribute("href")!;
        Assert.StartsWith("data:image/png;base64,", embedded);
        Assert.Equal(original.ToPng(), Convert.FromBase64String(embedded.Substring(22)));
        Assert.NotEmpty(svg.Descendants(ns + "polyline"));
        Assert.All(QrArt.ValidateImage(framed.Image.ToPng(), payload, TestBudget.Adjust(3000)).Checks, check => Assert.True(check.Passed, check.Name));
    }

    [Fact]
    public void LayoutExplorationMeasuresEveryMaskWithoutMutatingOptions() {
        var source = CodeGlyphX.Rendering.Png.PngImageEncoder.EncodeRgba32(new byte[] { 180, 170, 130, 255 }, 1, 1);
        var options = new QrImageSearchOptions {
            ExploreLayouts = true, AdditionalVersions = 0, IncludeQuartileErrorCorrection = false,
            ValidationCandidates = 1, Results = 1, DecodeBudgetMilliseconds = 3000,
            Composition = QrIllustratedComposer.CreateOptions(QrIllustratedStyle.BotanicalBadge, 6)
        };
        var result = QrArt.SearchImage("LAYOUT-SEARCH", source, options);
        Assert.Equal(40, result.EvaluatedCandidates);
        Assert.Equal(1, result.ValidatedCandidates);
        Assert.Equal(0.5, options.Composition.ImagePositionX);
        Assert.Equal(1, options.Composition.ImageZoom);
        var candidate = Assert.Single(result.Candidates);
        Assert.InRange(candidate.Layout.Zoom, 1, 4);
        Assert.Equal(3, candidate.Validation.Checks.Count);
        Assert.All(candidate.Validation.Checks.Where(check => check.Passed), check => Assert.Equal("LAYOUT-SEARCH", check.DecodedText));
    }

    [Fact]
    public async Task AsyncImageValidationPreservesSyncReportsAndCancellation() {
        const string payload = "ASYNC-ART";
        var png = CodeGlyphX.Rendering.Png.QrPngRenderer.Render(QR.Encode(payload).Modules, new CodeGlyphX.Rendering.Png.QrPngRenderOptions());
        var sync = QrArt.ValidateImage(png, payload);
        var asyncReport = await QrArt.ValidateImageAsync(png, payload);
        Assert.Equal(sync.Checks.Select(c => (c.Name, c.DecodedText)), asyncReport.Checks.Select(c => (c.Name, c.DecodedText)));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => QrArt.ValidateImageAsync(png, payload, cancellationToken: cancelled.Token));
    }

    [Fact]
    public void IllustrationRejectsUnknownStylesAndHonorsCancellation() {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrIllustratedComposer.CreateOptions((QrIllustratedStyle)99));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => QrIllustratedComposer.Render(QR.Encode("x"), new byte[4], 1, 1,
            QrIllustratedStyle.BotanicalBadge, cancellationToken: cancelled.Token));
    }
}
