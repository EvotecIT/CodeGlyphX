using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffDeflateEncodeTests {
    [Fact]
    public void Encode_Tiff_Deflate_RoundTrip() {
        const int width = 4;
        const int height = 3;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i + 0] = 10;
            rgba[i + 1] = 40;
            rgba[i + 2] = 90;
            rgba[i + 3] = 255;
        }

        using var ms = new System.IO.MemoryStream();
        TiffWriter.WriteRgba32(ms, width, height, rgba, width * 4, rowsPerStrip: 1, compression: TiffCompression.Deflate);
        var data = ms.ToArray();

        Assert.True(TiffReader.TryDecodeRgba32(data, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba.Length, decoded.Length);
        Assert.Equal(10, decoded[0]);
        Assert.Equal(40, decoded[1]);
        Assert.Equal(90, decoded[2]);
    }
}
