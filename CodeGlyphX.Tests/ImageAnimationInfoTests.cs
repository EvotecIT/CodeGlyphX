using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageAnimationInfoTests {
    [Fact]
    public void ReadAnimationInfo_Gif() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        Assert.True(ImageReader.TryReadAnimationInfo(gif, out var info));
        Assert.Equal(ImageFormat.Gif, info.Format);
        Assert.Equal(1, info.Width);
        Assert.Equal(1, info.Height);
        Assert.Equal(1, info.FrameCount);
        Assert.Equal(0, info.Options.LoopCount);
    }

    [Fact]
    public void ReadAnimationInfo_Webp() {
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

        Assert.True(ImageReader.TryReadAnimationInfo(webp, out var info));
        Assert.Equal(ImageFormat.Webp, info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal(2, info.FrameCount);
        Assert.Equal(2, info.Options.LoopCount);
        Assert.Equal(0x11443322u, info.Options.BackgroundRgba);
    }
}
