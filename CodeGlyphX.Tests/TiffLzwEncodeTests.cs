using System.IO;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffLzwEncodeTests {
    [Fact]
    public void Encode_Tiff_Lzw_RoundTrip() {
        const int width = 3;
        const int height = 2;
        var rgba = new byte[] {
            0, 0, 0, 255,
            255, 255, 255, 255,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            128, 128, 128, 255
        };

        using var ms = new MemoryStream();
        TiffWriter.WriteRgba32(ms, width, height, rgba, stride: width * 4, rowsPerStrip: 1, compression: TiffCompression.Lzw, usePredictor: true);
        var encoded = ms.ToArray();
        var decoded = TiffReader.DecodeRgba32(encoded, out var decodedWidth, out var decodedHeight);

        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Encode_Tiff_Lzw_Tiled_RoundTrip() {
        const int width = 4;
        const int height = 3;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i + 0] = (byte)(i * 3);
            rgba[i + 1] = (byte)(i * 5);
            rgba[i + 2] = (byte)(i * 7);
            rgba[i + 3] = 255;
        }

        using var ms = new MemoryStream();
        TiffWriter.WriteRgba32Tiled(ms, width, height, rgba, stride: width * 4, tileWidth: 2, tileHeight: 2, compression: TiffCompression.Lzw, usePredictor: false);
        var encoded = ms.ToArray();
        var decoded = TiffReader.DecodeRgba32(encoded, out var decodedWidth, out var decodedHeight);

        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }
}
