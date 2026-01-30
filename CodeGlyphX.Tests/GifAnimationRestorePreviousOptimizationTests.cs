using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationRestorePreviousOptimizationTests {
    [Fact]
    public void Encode_Gif_Animation_RestorePrevious_Crops_Diff() {
        var frame1Pixels = new byte[] {
            255, 0, 0, 255, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        var frame1 = new GifAnimationFrame(frame1Pixels, 2, 2, 8, durationMs: 20);

        var frame2Pixels = new byte[] {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 255, 0, 255
        };
        var frame2 = new GifAnimationFrame(frame2Pixels, 2, 2, 8, durationMs: 20, disposal: GifDisposal.RestorePrevious);

        var gif = GifWriter.WriteAnimation(2, 2, new[] { frame1, frame2 });
        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out _, out _, out _));
        Assert.Equal(2, frames.Length);
        Assert.Equal(1, frames[1].Width);
        Assert.Equal(1, frames[1].Height);
    }
}
