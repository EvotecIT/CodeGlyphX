using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeAnimationRenderExtrasTests {
    [Fact]
    public void Render_Barcode_Gif_Animation_Uses_RenderExtras_Frames() {
        var frame1 = new Barcode1D(new[] {
            new BarSegment(true, 2),
            new BarSegment(false, 1),
            new BarSegment(true, 1)
        });
        var frame2 = new Barcode1D(new[] {
            new BarSegment(true, 1),
            new BarSegment(false, 1),
            new BarSegment(true, 2)
        });

        var options = new BarcodeOptions {
            ModuleSize = 1,
            QuietZone = 0,
            HeightModules = 4
        };

        var extras = new RenderExtras {
            BarcodeGifFrames = new[] { frame1, frame2 },
            AnimationDurationsMs = new[] { 40, 60 }
        };

        var output = Barcode.Render(frame1, OutputFormat.Gif, options, extras);
        var frames = GifReader.DecodeAnimationFrames(output.Data, out _, out _, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(40, frames[0].DurationMs);
        Assert.Equal(60, frames[1].DurationMs);
    }

    [Fact]
    public void Render_Barcode_Webp_Animation_Uses_RenderExtras_Frames() {
        var frame1 = new Barcode1D(new[] {
            new BarSegment(true, 2),
            new BarSegment(false, 1),
            new BarSegment(true, 1)
        });
        var frame2 = new Barcode1D(new[] {
            new BarSegment(true, 1),
            new BarSegment(false, 1),
            new BarSegment(true, 2)
        });

        var options = new BarcodeOptions {
            ModuleSize = 1,
            QuietZone = 0,
            HeightModules = 4
        };

        var extras = new RenderExtras {
            BarcodeWebpFrames = new[] { frame1, frame2 },
            AnimationDurationsMs = new[] { 35, 55 }
        };

        var output = Barcode.Render(frame1, OutputFormat.Webp, options, extras);
        var frames = WebpReader.DecodeAnimationFrames(output.Data, out _, out _, out _);

        Assert.Equal(2, frames.Length);
        Assert.Equal(35, frames[0].DurationMs);
        Assert.Equal(55, frames[1].DurationMs);
    }
}
