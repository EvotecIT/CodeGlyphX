using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageScalerCancellationTests {
    [Fact]
    public void BothResamplersHonorCancellationBeforeAllocatingOutput() {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var rgba = new byte[4];
        Assert.Throws<OperationCanceledException>(() => ImageScaler.ResizeToFitBox(rgba, 1, 1, 4, 8000, 6000, Rgba32.White, false, source.Token));
        Assert.Throws<OperationCanceledException>(() => ImageScaler.ResizeToFitNearest(rgba, 1, 1, 4, 8000, 6000, Rgba32.White, false, source.Token));
    }

    [Fact]
    public void LargeBoxAveragesDoNotOverflow() {
        var rgba = new byte[3000 * 3000 * 4];
        Array.Fill(rgba, (byte)255);
        var result = ImageScaler.ResizeToFitBox(rgba, 3000, 3000, 12000, 1, 1, Rgba32.White, false);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, result);
    }
}
