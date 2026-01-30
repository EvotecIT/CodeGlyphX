using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationEncodeTests {
    [Fact]
    public void Encode_Gif_Animation_RoundTrip() {
        var frame1 = new GifAnimationFrame(new byte[] { 255, 0, 0, 255 }, 1, 1, 4, durationMs: 20);
        var frame2 = new GifAnimationFrame(new byte[] { 0, 0, 255, 255 }, 1, 1, 4, durationMs: 30);
        var options = new GifAnimationOptions(loopCount: 2, backgroundRgba: 0);

        var gif = GifWriter.WriteAnimation(1, 1, new[] { frame1, frame2 }, options);
        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out var width, out var height, out var decodedOptions));

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(2, frames.Length);
        Assert.Equal(2, decodedOptions.LoopCount);

        Assert.Equal(255, frames[0].Rgba[0]);
        Assert.Equal(0, frames[0].Rgba[2]);
        Assert.Equal(255, frames[1].Rgba[2]);
    }
}
