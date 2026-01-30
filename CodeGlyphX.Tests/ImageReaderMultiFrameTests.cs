using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageReaderMultiFrameTests {
    [Fact]
    public void Decode_Gif_Animation_Frames_Via_ImageReader() {
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

        Assert.True(ImageReader.TryDecodeGifAnimationFrames(gif, out var frames, out var width, out var height, out _));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(2, frames.Length);
    }

    [Fact]
    public void Decode_Tiff_Pages_Via_ImageReader() {
        var page1 = new TiffRgba32Page(new byte[] { 255, 0, 0, 255 }, 1, 1, 4);
        var page2 = new TiffRgba32Page(new byte[] { 0, 255, 0, 255 }, 1, 1, 4);
        var tiff = TiffWriter.WriteRgba32Pages(page1, page2);

        Assert.True(ImageReader.TryDecodeTiffPagesRgba32(tiff, out var pages));
        Assert.Equal(2, pages.Length);
    }
}
