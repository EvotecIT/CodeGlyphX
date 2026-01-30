using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationOptimizationTests {
    [Fact]
    public void Encode_Gif_Animation_Crops_Diff_Frames() {
        var canvas = new byte[] {
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
        var frame1 = new GifAnimationFrame(canvas, 2, 2, 8, durationMs: 20);

        var frame2Pixels = new byte[] {
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            255, 0, 0, 255
        };
        var frame2 = new GifAnimationFrame(frame2Pixels, 2, 2, 8, durationMs: 20);

        var gif = GifWriter.WriteAnimation(2, 2, new[] { frame1, frame2 });
        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out _, out _, out _));
        Assert.Equal(2, frames.Length);
        Assert.Equal(1, frames[1].Width);
        Assert.Equal(1, frames[1].Height);
    }
}
