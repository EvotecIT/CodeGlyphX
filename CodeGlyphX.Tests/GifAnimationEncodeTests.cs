using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationEncodeTests {
    [Fact]
    public void Gif_AnimationEncode_RoundTripsFrames() {
        const int width = 2;
        const int height = 2;
        const int stride = width * 4;

        var frame1 = new byte[] {
            0, 0, 0, 255,       255, 0, 0, 255,
            0, 255, 0, 255,     0, 0, 255, 255
        };

        var frame2 = new byte[] {
            0, 0, 0, 0,         255, 255, 0, 255,
            255, 0, 255, 255,   0, 255, 255, 255
        };

        var frames = new[] {
            new GifAnimationFrame(frame1, width, height, stride, durationMs: 120, disposalMethod: GifDisposalMethod.RestoreBackground),
            new GifAnimationFrame(frame2, width, height, stride, durationMs: 50, disposalMethod: GifDisposalMethod.None)
        };

        var gif = GifWriter.WriteAnimationRgba32(width, height, frames, new GifAnimationOptions(loopCount: 3, backgroundRgba: 0x000000FF));

        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var decodedFrames, out var canvasWidth, out var canvasHeight, out var options));
        Assert.Equal(width, canvasWidth);
        Assert.Equal(height, canvasHeight);
        Assert.Equal(3, options.LoopCount);
        Assert.Equal(2, decodedFrames.Length);

        Assert.Equal(frame1, decodedFrames[0].Rgba);
        Assert.Equal(GifDisposalMethod.RestoreBackground, decodedFrames[0].DisposalMethod);
        Assert.Equal(frame2, decodedFrames[1].Rgba);
    }
}
