using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationDecodeTests {
    [Fact]
    public void Decode_Gif_Animation_Frames() {
        var gif = new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a
            0x01, 0x00, 0x01, 0x00, // 1x1
            0x80, 0x00, 0x00, // GCT, background, aspect
            0xFF, 0x00, 0x00, // red
            0x00, 0x00, 0xFF, // blue
            0x21, 0xF9, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, // GCE frame 1
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // image descriptor
            0x02, 0x02, 0x44, 0x01, 0x00, // image data (red)
            0x21, 0xF9, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, // GCE frame 2
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // image descriptor
            0x02, 0x02, 0x4C, 0x01, 0x00, // image data (blue)
            0x3B // trailer
        };

        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var frames, out var width, out var height, out var options));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(2, frames.Length);
        Assert.Equal(10, frames[0].DurationMs);
        Assert.Equal(10, frames[1].DurationMs);

        Assert.Equal(255, frames[0].Rgba[0]);
        Assert.Equal(0, frames[0].Rgba[1]);
        Assert.Equal(0, frames[0].Rgba[2]);
        Assert.Equal(255, frames[0].Rgba[3]);

        Assert.Equal(0, frames[1].Rgba[0]);
        Assert.Equal(0, frames[1].Rgba[1]);
        Assert.Equal(255, frames[1].Rgba[2]);
        Assert.Equal(255, frames[1].Rgba[3]);

        Assert.Equal(0, options.LoopCount);
    }

    [Fact]
    public void Decode_Gif_Animation_Canvas_Frames() {
        var gif = new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61,
            0x01, 0x00, 0x01, 0x00,
            0x80, 0x00, 0x00,
            0xFF, 0x00, 0x00,
            0x00, 0x00, 0xFF,
            0x21, 0xF9, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0x02, 0x02, 0x44, 0x01, 0x00,
            0x21, 0xF9, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0x02, 0x02, 0x4C, 0x01, 0x00,
            0x3B
        };

        Assert.True(GifReader.TryDecodeAnimationCanvasFrames(gif, out var frames, out var width, out var height, out _));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(2, frames.Length);
        Assert.Equal(255, frames[0].Rgba[0]);
        Assert.Equal(0, frames[1].Rgba[0]);
        Assert.Equal(255, frames[1].Rgba[2]);
    }
}
