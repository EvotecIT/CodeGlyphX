#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFinderRunTests {
    private static readonly ConstructorInfo ImageConstructor = typeof(QrGrayImage)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(byte[]), typeof(byte), typeof(byte), typeof(byte), typeof(byte[]) }, null)!;

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(1, true, false)]
    [InlineData(3, false, false)]
    [InlineData(3, true, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, true)]
    [InlineData(3, false, true)]
    [InlineData(3, true, true)]
    public void FinderRuns_PreserveCroppedGeometryAndObservationCounts(int moduleSize, bool invert, bool adaptive) {
        // The right finder touches the last pixel of its row. Padding on the
        // left places runs across vector boundaries without adding a quiet zone.
        var image = CreateImage(moduleSize, invert, adaptive, left: 31);
        var candidates = QrFinderPatternDetector.FindCandidates(image, invert, aggressive: false,
            rowStepOverride: 1, allowFullScan: false);
        var near = 3.5 * moduleSize;
        var far = 25.5 * moduleSize;
        Assert.Equal(3, candidates.Count);
        AssertFinder(candidates[0], 31 + near, near, moduleSize, 3 * moduleSize);
        AssertFinder(candidates[1], 31 + far, near, moduleSize, 3 * moduleSize);
        AssertFinder(candidates[2], 31 + near, far, moduleSize, 3 * moduleSize);
    }

    [Fact]
    public void FinderRuns_StopAtRowBoundaryAndKeepCandidateLimit() {
        var image = CreateImage(1, invert: false, adaptive: false, left: 0);
        var output = new List<QrFinderPatternDetector.FinderPattern>();
        var polls = 0;
        QrFinderPatternDetector.FindCandidates(image, invert: false, output, aggressive: false,
            shouldStop: () => ++polls == 8, rowStepOverride: 1, allowFullScan: false);
        Assert.Equal(8, polls);
        Assert.Equal(2, output.Count);
        AssertFinder(output[0], 3.5, 3.5, 1, 3);
        AssertFinder(output[1], 25.5, 3.5, 1, 3);

        QrFinderPatternDetector.FindCandidates(image, invert: false, output, aggressive: false,
            rowStepOverride: 1, maxCandidates: 1, allowFullScan: false);
        AssertFinder(Assert.Single(output), 3.5, 3.5, 1, 1);

        QrFinderPatternDetector.FindCandidates(image, invert: false, output, aggressive: false,
            shouldStop: () => true, rowStepOverride: 1, allowFullScan: false);
        Assert.Empty(output);
    }

    private static void AssertFinder(QrFinderPatternDetector.FinderPattern finder, double x, double y, int moduleSize, int count) {
        Assert.Equal(x, finder.X);
        Assert.Equal(y, finder.Y);
        Assert.Equal(moduleSize, finder.ModuleSize);
        Assert.Equal(count, finder.Count);
    }

    private static QrGrayImage CreateImage(int moduleSize, bool invert, bool adaptive, int left) {
        var modules = QrCodeEncoder.EncodeText("RUN-BOUNDARIES", new QrEncodingOptions { MinVersion = 3, MaxVersion = 3 }).Modules;
        var pixels = QrPngRenderer.RenderPixels(modules, new QrPngRenderOptions { ModuleSize = moduleSize, QuietZone = 0 },
            out var sourceWidth, out var height, out _);
        var width = sourceWidth + left;
        var gray = new byte[width * height];
        Array.Fill(gray, invert ? (byte)0 : (byte)255);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < sourceWidth; x++)
                gray[y * width + left + x] = (byte)(pixels[(y * sourceWidth + x) * 4] ^ (invert ? 255 : 0));
        }
        var map = adaptive ? new byte[gray.Length] : null;
        if (map is not null) {
            for (var at = 0; at < map.Length; at++) map[at] = (byte)(64 + at % 128);
        }
        return (QrGrayImage)ImageConstructor.Invoke(new object?[] { width, height, gray, (byte)0, (byte)255, (byte)127, map });
    }
}
#endif
