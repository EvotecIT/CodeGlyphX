using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffPackBitsEncodeTests {
    [Fact]
    public void Encode_Tiff_PackBits_RoundTrip() {
        const int width = 4;
        const int height = 2;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i + 0] = 50;
            rgba[i + 1] = 100;
            rgba[i + 2] = 150;
            rgba[i + 3] = 255;
        }

        using var ms = new System.IO.MemoryStream();
        TiffWriter.WriteRgba32(ms, width, height, rgba, width * 4, rowsPerStrip: 1, compression: TiffCompression.PackBits);
        var data = ms.ToArray();

        Assert.True(TiffReader.TryDecodeRgba32(data, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba.Length, decoded.Length);
        Assert.Equal(50, decoded[0]);
        Assert.Equal(100, decoded[1]);
        Assert.Equal(150, decoded[2]);
    }
}
