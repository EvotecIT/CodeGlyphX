using System;
using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationTests {
    [Fact]
    public void GifAnimation_SingleFrame_Decodes() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out var width, out var height, out var options));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Single(frames);
        Assert.Equal(0, options.LoopCount);
        Assert.Equal(4, frames[0].Rgba.Length);
    }

    [Fact]
    public void GifAnimation_CanvasFrames_Decodes() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        Assert.True(GifReader.TryDecodeAnimationCanvasFrames(gif, out var frames, out var width, out var height, out _));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Single(frames);
        Assert.Equal(4, frames[0].Rgba.Length);
    }
}
