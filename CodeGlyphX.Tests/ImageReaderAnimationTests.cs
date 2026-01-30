using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageReaderAnimationTests {
    [Fact]
    public void ImageReader_DecodeAnimationCanvasFrames_Gif() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        var frames = ImageReader.DecodeAnimationCanvasFrames(gif, out var width, out var height, out var options);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Single(frames);
        Assert.Equal(0, options.LoopCount);
        Assert.Equal(4, frames[0].Rgba.Length);
    }

    [Fact]
    public void ImageReader_DecodeAnimationCanvasFrames_Webp() {
        var frame1 = new WebpAnimationDecodeTests.AnimationFrameSpec(
            WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 10, g: 20, b: 30, a: 255),
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 50,
            blend: true,
            disposeToBackground: false);
        var frame2 = new WebpAnimationDecodeTests.AnimationFrameSpec(
            WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 5, g: 6, b: 7, a: 255),
            x: 1,
            y: 1,
            width: 1,
            height: 1,
            durationMs: 70,
            blend: false,
            disposeToBackground: true);

        var webp = WebpAnimationDecodeTests.BuildAnimatedWebp(
            new[] { frame1, frame2 },
            canvasWidth: 2,
            canvasHeight: 2,
            bgraBackground: 0x11223344,
            loopCount: 2);

        var frames = ImageReader.DecodeAnimationCanvasFrames(webp, out var width, out var height, out var options);

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(2, options.LoopCount);
        Assert.Equal(0x11443322u, options.BackgroundRgba);
        Assert.Equal(2, frames.Length);
        AssertPixel(frames[0].Rgba, width, 0, 0, 10, 20, 30, 255);
        AssertPixel(frames[1].Rgba, width, 1, 1, 5, 6, 7, 255);
    }

    private static void AssertPixel(byte[] rgba, int width, int x, int y, byte r, byte g, byte b, byte a) {
        var index = (y * width + x) * 4;
        Assert.Equal(r, rgba[index]);
        Assert.Equal(g, rgba[index + 1]);
        Assert.Equal(b, rgba[index + 2]);
        Assert.Equal(a, rgba[index + 3]);
    }
}
