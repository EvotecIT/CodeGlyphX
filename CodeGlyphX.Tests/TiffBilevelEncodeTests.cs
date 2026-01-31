using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffBilevelEncodeTests {
    [Theory]
    [InlineData(TiffCompression.None)]
    [InlineData(TiffCompression.PackBits)]
    [InlineData(TiffCompression.Deflate)]
    public void Encode_Bilevel_Tiff_RoundTrip(TiffCompression compression) {
        const int width = 10;
        const int height = 1;
        var row = new byte[] { 0xAA, 0x80 }; // 10101010 10xxxxxx

        var tiff = TiffWriter.WriteBilevel(width, height, row, stride: 2, rowsPerStrip: 1, compression: compression);
        var rgba = TiffReader.DecodeRgba32(tiff, out var decodedWidth, out var decodedHeight);

        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);

        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[4]);
        Assert.Equal(255, rgba[8]);
        Assert.Equal(0, rgba[12]);
        Assert.Equal(255, rgba[16]);
        Assert.Equal(0, rgba[20]);
        Assert.Equal(255, rgba[24]);
        Assert.Equal(0, rgba[28]);
        Assert.Equal(255, rgba[32]);
        Assert.Equal(0, rgba[36]);
    }

    [Fact]
    public void Encode_Bilevel_Tiff_WhiteIsZero() {
        var row = new byte[] { 0x80 }; // first bit = 1
        var tiff = TiffWriter.WriteBilevel(8, 1, row, stride: 1, rowsPerStrip: 1, compression: TiffCompression.None, photometric: 0);
        var rgba = TiffReader.DecodeRgba32(tiff, out var decodedWidth, out var decodedHeight);

        Assert.Equal(8, decodedWidth);
        Assert.Equal(1, decodedHeight);
        Assert.Equal(0, rgba[0]);
        Assert.Equal(255, rgba[4]);
    }

    [Fact]
    public void Encode_Bilevel_From_Rgba_Defaults() {
        var rgba = new byte[] {
            0, 0, 0, 255,
            255, 255, 255, 255
        };

        var tiff = TiffWriter.WriteBilevelFromRgba(2, 1, rgba, stride: 8);
        var decoded = TiffReader.DecodeRgba32(tiff, out var decodedWidth, out var decodedHeight);

        Assert.Equal(2, decodedWidth);
        Assert.Equal(1, decodedHeight);
        Assert.Equal(0, decoded[0]);
        Assert.Equal(255, decoded[4]);
    }
}
