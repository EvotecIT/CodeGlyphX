using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationRenderTests {
    [Fact]
    public void Render_Gif_Animation_From_Matrix_Frames() {
        var frame1 = new BitMatrix(2, 2);
        frame1[0, 0] = true;
        frame1[1, 1] = true;

        var frame2 = new BitMatrix(2, 2);
        frame2[0, 1] = true;
        frame2[1, 0] = true;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0
        };

        var gif = MatrixGifRenderer.RenderAnimation(new[] { frame1, frame2 }, opts, durationMs: 50);
        var frames = GifReader.DecodeAnimationFrames(gif, out var width, out var height, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(2, width);
        Assert.Equal(2, height);
    }

    [Fact]
    public void Render_Gif_Animation_With_Durations() {
        var frame1 = new BitMatrix(1, 1);
        frame1[0, 0] = true;
        var frame2 = new BitMatrix(1, 1);
        frame2[0, 0] = false;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0
        };

        var gif = MatrixGifRenderer.RenderAnimation(new[] { frame1, frame2 }, opts, new[] { 30, 60 });
        var frames = GifReader.DecodeAnimationFrames(gif, out _, out _, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(30, frames[0].DurationMs);
        Assert.Equal(60, frames[1].DurationMs);
    }
}
