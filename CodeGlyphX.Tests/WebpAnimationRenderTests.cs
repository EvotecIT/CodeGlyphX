using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpAnimationRenderTests {
    [Fact]
    public void Render_Webp_Animation_From_Matrix_Frames() {
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

        var webp = MatrixWebpRenderer.RenderAnimation(new[] { frame1, frame2 }, opts, durationMs: 50, quality: 80);
        var frames = WebpReader.DecodeAnimationFrames(webp, out var width, out var height, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(2, width);
        Assert.Equal(2, height);
    }

    [Fact]
    public void Render_Webp_Animation_With_Durations() {
        var frame1 = new BitMatrix(1, 1);
        frame1[0, 0] = true;
        var frame2 = new BitMatrix(1, 1);
        frame2[0, 0] = false;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0
        };

        var webp = MatrixWebpRenderer.RenderAnimation(new[] { frame1, frame2 }, opts, new[] { 40, 80 }, quality: 100);
        var frames = WebpReader.DecodeAnimationFrames(webp, out _, out _, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(40, frames[0].DurationMs);
        Assert.Equal(80, frames[1].DurationMs);
    }
}
