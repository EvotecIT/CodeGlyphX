using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpAnimationAlphaDecodeTests {
    [Fact]
    public void Decode_Webp_Animation_Preserves_Alpha() {
        var rgba = new byte[] {
            10, 20, 30, 0,     40, 50, 60, 255,
            70, 80, 90, 255,   100, 110, 120, 255
        };
        var frame = new WebpAnimationFrame(rgba, width: 2, height: 2, stride: 8, durationMs: 100);
        var webp = WebpWriter.WriteAnimationRgba32Lossy(2, 2, new[] { frame }, options: default, quality: 60);

        var frames = WebpReader.DecodeAnimationFrames(webp, out var width, out var height, out _);

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Single(frames);
        Assert.Equal(0, frames[0].Rgba[3]);
    }
}
