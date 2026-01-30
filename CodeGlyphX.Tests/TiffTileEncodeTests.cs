using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffTileEncodeTests {
    [Fact]
    public void Encode_Tiff_Tiled_Uncompressed_RoundTrip() {
        var rgba = new byte[] {
            255, 0, 0, 255,   0, 255, 0, 255,   0, 0, 255, 255,
            10, 20, 30, 255,  40, 50, 60, 255,  70, 80, 90, 255
        };
        var tiff = TiffWriter.WriteRgba32Tiled(width: 3, height: 2, rgba, stride: 12, tileWidth: 2, tileHeight: 2);
        var decoded = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(3, width);
        Assert.Equal(2, height);
        Assert.Equal(255, decoded[0]);
        Assert.Equal(0, decoded[1]);
        Assert.Equal(0, decoded[2]);
        Assert.Equal(10, decoded[12]);
        Assert.Equal(20, decoded[13]);
        Assert.Equal(30, decoded[14]);
    }

    [Fact]
    public void Encode_Tiff_Tiled_DeflatePredictor_RoundTrip() {
        var rgba = new byte[] {
            255, 255, 255, 255,  0, 0, 0, 255,
            0, 0, 0, 255,        255, 255, 255, 255
        };
        var tiff = TiffWriter.WriteRgba32Tiled(
            width: 2,
            height: 2,
            rgba,
            stride: 8,
            tileWidth: 2,
            tileHeight: 2,
            compression: TiffCompression.Deflate,
            usePredictor: true);
        var decoded = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(255, decoded[0]);
        Assert.Equal(255, decoded[1]);
        Assert.Equal(255, decoded[2]);
        Assert.Equal(0, decoded[4]);
        Assert.Equal(0, decoded[5]);
        Assert.Equal(0, decoded[6]);
    }
}
