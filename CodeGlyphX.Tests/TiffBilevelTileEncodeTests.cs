using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffBilevelTileEncodeTests {
    [Theory]
    [InlineData(TiffCompression.None)]
    [InlineData(TiffCompression.PackBits)]
    [InlineData(TiffCompression.Deflate)]
    public void Encode_Bilevel_Tiff_Tiled_RoundTrip(TiffCompression compression) {
        const int width = 10;
        const int height = 2;
        var packed = new byte[] {
            0xAA, 0x80, // row 0: 10101010 10......
            0x55, 0x00  // row 1: 01010101 00......
        };

        var tiff = TiffWriter.WriteBilevelTiled(width, height, packed, stride: 2, tileWidth: 8, tileHeight: 1, compression: compression);
        var rgba = TiffReader.DecodeRgba32(tiff, out var decodedWidth, out var decodedHeight);

        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);

        Assert.Equal(255, rgba[0]); // x=0, y=0 -> white
        Assert.Equal(0, rgba[4]);   // x=1, y=0 -> black
        Assert.Equal(255, rgba[32]); // x=8, y=0 -> white
        Assert.Equal(0, rgba[36]);   // x=9, y=0 -> black

        var row1 = width * 4;
        Assert.Equal(0, rgba[row1 + 0]); // x=0, y=1 -> black
        Assert.Equal(255, rgba[row1 + 4]); // x=1, y=1 -> white
    }
}
