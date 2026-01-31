using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class ImageReaderAnimationLimitTests {
    [Fact]
    public void TryDecodeAnimationFrames_Rejects_TooManyGifFrames() {
        var frames = BuildFrames(3);
        var gif = MatrixGifRenderer.RenderAnimation(frames, new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 }, durationMs: 50);
        var options = new ImageDecodeOptions { MaxAnimationFrames = 2 };

        Assert.False(ImageReader.TryDecodeAnimationFrames(gif, options, out _, out _, out _, out _));
    }

    [Fact]
    public void TryDecodeAnimationFrames_Rejects_GifDurationLimit() {
        var frames = BuildFrames(2);
        var durations = new[] { 400, 400 };
        var gif = MatrixGifRenderer.RenderAnimation(frames, new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 }, durations);
        var options = new ImageDecodeOptions { MaxAnimationDurationMs = 500 };

        Assert.False(ImageReader.TryDecodeAnimationFrames(gif, options, out _, out _, out _, out _));
    }

    [Fact]
    public void TryDecodeAnimationFrames_Rejects_WebpDurationLimit() {
        var frame1 = new WebpAnimationDecodeTests.AnimationFrameSpec(
            WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 10, g: 20, b: 30, a: 255),
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 500,
            blend: true,
            disposeToBackground: false);
        var frame2 = new WebpAnimationDecodeTests.AnimationFrameSpec(
            WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 5, g: 6, b: 7, a: 255),
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 600,
            blend: true,
            disposeToBackground: false);

        var webp = WebpAnimationDecodeTests.BuildAnimatedWebp(
            new[] { frame1, frame2 },
            canvasWidth: 1,
            canvasHeight: 1,
            bgraBackground: 0,
            loopCount: 0);

        var options = new ImageDecodeOptions { MaxAnimationDurationMs = 500 };
        ImageDecodeLimitViolation? violation = null;
        void Handler(ImageDecodeLimitViolation v) => violation = v;

        ImageReader.LimitViolation += Handler;
        try {
            Assert.True(ImageReader.TryDecodeAnimationFrames(webp, options, out var frames, out _, out _, out _));
            Assert.Single(frames);
        } finally {
            ImageReader.LimitViolation -= Handler;
        }

        Assert.True(violation.HasValue);
        Assert.Equal(ImageDecodeLimitKind.MaxAnimationDurationMs, violation!.Value.Kind);
    }

    [Fact]
    public void TryDecodeAnimationFrames_Rejects_WebpFramePixelsLimit() {
        const int width = 5000;
        const int height = 5000;
        var frame = new WebpAnimationDecodeTests.AnimationFrameSpec(
            WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: width, height: height, r: 10, g: 20, b: 30, a: 255),
            x: 0,
            y: 0,
            width: width,
            height: height,
            durationMs: 10,
            blend: true,
            disposeToBackground: false);

        var webp = WebpAnimationDecodeTests.BuildAnimatedWebp(
            new[] { frame },
            canvasWidth: width,
            canvasHeight: height,
            bgraBackground: 0,
            loopCount: 0);

        var options = new ImageDecodeOptions { MaxAnimationFramePixels = 1_000_000 };
        ImageDecodeLimitViolation? violation = null;
        void Handler(ImageDecodeLimitViolation v) => violation = v;

        ImageReader.LimitViolation += Handler;
        try {
            Assert.False(ImageReader.TryDecodeAnimationFrames(webp, options, out _, out _, out _, out _));
        } finally {
            ImageReader.LimitViolation -= Handler;
        }

        Assert.True(violation.HasValue);
        Assert.Equal(ImageDecodeLimitKind.MaxAnimationFramePixels, violation!.Value.Kind);
    }

    private static BitMatrix[] BuildFrames(int count) {
        var frames = new BitMatrix[count];
        for (var i = 0; i < count; i++) {
            var matrix = new BitMatrix(1, 1);
            matrix[0, 0] = (i % 2) == 0;
            frames[i] = matrix;
        }
        return frames;
    }
}
