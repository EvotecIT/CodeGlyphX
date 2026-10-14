using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ScanlineThresholdTests {
    [Theory]
    [InlineData(new byte[] { 0, 255, 0, 255 }, new int[] { 15, 31, 63, 127, 191, 239 })]
    [InlineData(new byte[] { 0, 127, 128, 255 }, new int[] { 128, 127, 129, 128, 0, 255 })]
    [InlineData(new byte[] { 255, 254, 1, 0, 254 }, new int[] { 255, 254, 2, 1, 0, 128 })]
    [InlineData(new byte[] { 17, 17, 17 }, new int[] { 17, 18, 16, 19 })]
    [InlineData(new byte[] { }, new int[] { 0, 127, 255 })]
    public void DistinctThresholds_PreserveFirstMaskAndEveryChangedPixel(byte[] luminance, int[] cutoffs) {
        // Direct pixel masks are an independent oracle for the histogram shortcut.
        var masks = new List<bool[]>();
        var expected = new List<int>();
        foreach (var cutoff in cutoffs) {
            var mask = luminance.Select(pixel => pixel < cutoff).ToArray();
            if (masks.Any(prior => prior.SequenceEqual(mask))) continue;
            masks.Add(mask);
            expected.Add(cutoff);
        }
        var original = (byte[])luminance.Clone();
        var thresholds = (int[])cutoffs.Clone();
        var count = ScanlineThresholds.RetainDistinct(luminance, CancellationToken.None, thresholds, thresholds.Length);
        Assert.Equal(expected, thresholds.Take(count));
        Assert.Equal(original, luminance);
    }

    [Fact]
    public void DistinctThresholds_CancellationDoesNotExposePartialMasks() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var thresholds = new[] { 63, 127, 191 };
        Assert.Equal(0, ScanlineThresholds.RetainDistinct(new byte[] { 0, 64, 128, 255 }, cancellation.Token, thresholds, thresholds.Length));
    }
}
