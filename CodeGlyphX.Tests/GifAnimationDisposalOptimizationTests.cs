using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationDisposalOptimizationTests {
    [Fact]
    public void Encode_Gif_Animation_RestoreBackground_Crops_With_Transparent_Hole() {
        var red = new byte[] { 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255 };
        var frame1 = new GifAnimationFrame(red, 2, 2, 8, durationMs: 20, disposeToBackground: true);

        var frame2Pixels = new byte[] {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        var frame2 = new GifAnimationFrame(frame2Pixels, 2, 2, 8, durationMs: 20, disposeToBackground: true);

        var gif = GifWriter.WriteAnimation(2, 2, new[] { frame1, frame2 }, new GifAnimationOptions(loopCount: 0, backgroundRgba: 0));
        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out _, out _, out _));
        Assert.Equal(2, frames.Length);
        Assert.Equal(2, frames[1].Width);
        Assert.Equal(2, frames[1].Height);
    }
}
