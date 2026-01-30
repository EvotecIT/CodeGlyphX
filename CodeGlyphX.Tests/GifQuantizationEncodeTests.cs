using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifQuantizationEncodeTests {
    [Fact]
    public void Encode_Gif_Quantizes_When_Colors_Exceed_256() {
        const int width = 17;
        const int height = 17;
        var rgba = new byte[width * height * 4];
        var idx = 0;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                rgba[idx++] = (byte)(x * 15);
                rgba[idx++] = (byte)(y * 15);
                rgba[idx++] = (byte)((x + y) * 7);
                rgba[idx++] = 255;
            }
        }

        var gif = GifWriter.WriteRgba32(width, height, rgba, width * 4);
        Assert.True(ImageReader.TryDecodeRgba32(gif, out var decoded, out var w, out var h));
        Assert.Equal(width, w);
        Assert.Equal(height, h);
        Assert.Equal(width * height * 4, decoded.Length);
    }
}
