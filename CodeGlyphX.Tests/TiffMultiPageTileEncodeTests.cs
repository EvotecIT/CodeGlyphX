using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiPageTileEncodeTests {
    [Fact]
    public void Encode_Tiff_Tiled_MultiPage_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] {
            255, 0, 0, 255,  0, 255, 0, 255
        }, 2, 1, 8);
        var page2 = new TiffRgba32Page(new byte[] {
            0, 0, 255, 255,  255, 255, 0, 255
        }, 2, 1, 8);

        var tiff = TiffWriter.WriteRgba32PagesTiled(tileWidth: 2, tileHeight: 1, page1, page2);
        var pages = TiffReader.DecodePagesRgba32(tiff);

        Assert.Equal(2, pages.Length);
        Assert.Equal(255, pages[0].Rgba[0]);
        Assert.Equal(0, pages[0].Rgba[1]);
        Assert.Equal(0, pages[0].Rgba[2]);
        Assert.Equal(0, pages[1].Rgba[0]);
        Assert.Equal(0, pages[1].Rgba[1]);
        Assert.Equal(255, pages[1].Rgba[2]);
    }

    [Fact]
    public void Encode_Tiff_Tiled_MultiPage_Deflate_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] {
            10, 20, 30, 255,  40, 50, 60, 255
        }, 2, 1, 8);
        var page2 = new TiffRgba32Page(new byte[] {
            70, 80, 90, 255,  100, 110, 120, 255
        }, 2, 1, 8);

        var tiff = TiffWriter.WriteRgba32PagesTiled(tileWidth: 2, tileHeight: 1, compression: TiffCompression.Deflate, usePredictor: true, page1, page2);
        var pages = TiffReader.DecodePagesRgba32(tiff);

        Assert.Equal(2, pages.Length);
        Assert.Equal(10, pages[0].Rgba[0]);
        Assert.Equal(20, pages[0].Rgba[1]);
        Assert.Equal(30, pages[0].Rgba[2]);
        Assert.Equal(70, pages[1].Rgba[0]);
        Assert.Equal(80, pages[1].Rgba[1]);
        Assert.Equal(90, pages[1].Rgba[2]);
    }
}
