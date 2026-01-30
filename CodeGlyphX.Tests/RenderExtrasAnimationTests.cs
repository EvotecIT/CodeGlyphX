using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderExtrasAnimationTests {
    [Fact]
    public void Render_Uses_GifFrames_From_Extras() {
        var frame1 = new BitMatrix(1, 1);
        frame1[0, 0] = true;
        var frame2 = new BitMatrix(1, 1);
        frame2[0, 0] = false;

        var extras = new RenderExtras {
            GifFrames = new[] { frame1, frame2 },
            AnimationDurationMs = 30
        };

        var output = QrCode.Render("test", OutputFormat.Gif, extras: extras);
        var frames = GifReader.DecodeAnimationFrames(output.Data, out _, out _, out _);

        Assert.Equal(2, frames.Length);
    }

    [Fact]
    public void Render_Uses_WebpFrames_From_Extras() {
        var frame1 = new BitMatrix(1, 1);
        frame1[0, 0] = true;
        var frame2 = new BitMatrix(1, 1);
        frame2[0, 0] = false;

        var extras = new RenderExtras {
            WebpFrames = new[] { frame1, frame2 },
            AnimationDurationMs = 40
        };

        var output = QrCode.Render("test", OutputFormat.Webp, extras: extras);
        var frames = WebpReader.DecodeAnimationFrames(output.Data, out _, out _, out _);

        Assert.Equal(2, frames.Length);
    }
}
