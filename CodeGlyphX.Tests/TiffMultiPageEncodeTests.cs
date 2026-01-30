using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiPageEncodeTests {
    [Fact]
    public void Encode_Tiff_MultiPage_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] { 255, 0, 0, 255 }, 1, 1, 4);
        var page2 = new TiffRgba32Page(new byte[] { 0, 255, 0, 255 }, 1, 1, 4);

        var tiff = TiffWriter.WriteRgba32Pages(page1, page2);
        var pages = TiffReader.DecodePagesRgba32(tiff);

        Assert.Equal(2, pages.Length);
        Assert.Equal(255, pages[0].Rgba[0]);
        Assert.Equal(0, pages[0].Rgba[1]);
        Assert.Equal(0, pages[0].Rgba[2]);
        Assert.Equal(255, pages[0].Rgba[3]);

        Assert.Equal(0, pages[1].Rgba[0]);
        Assert.Equal(255, pages[1].Rgba[1]);
        Assert.Equal(0, pages[1].Rgba[2]);
        Assert.Equal(255, pages[1].Rgba[3]);
    }

    [Fact]
    public void Encode_Tiff_MultiPage_DeflatePredictor_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] {
            10, 20, 30, 255,
            40, 50, 60, 255
        }, 2, 1, 8);
        var page2 = new TiffRgba32Page(new byte[] {
            70, 80, 90, 255,
            100, 110, 120, 255
        }, 2, 1, 8);

        var tiff = TiffWriter.WriteRgba32Pages(TiffCompression.Deflate, usePredictor: true, page1, page2);
        var pages = TiffReader.DecodePagesRgba32(tiff);

        Assert.Equal(2, pages.Length);
        Assert.Equal(10, pages[0].Rgba[0]);
        Assert.Equal(20, pages[0].Rgba[1]);
        Assert.Equal(30, pages[0].Rgba[2]);
        Assert.Equal(255, pages[0].Rgba[3]);

        Assert.Equal(70, pages[1].Rgba[0]);
        Assert.Equal(80, pages[1].Rgba[1]);
        Assert.Equal(90, pages[1].Rgba[2]);
        Assert.Equal(255, pages[1].Rgba[3]);
    }

    [Fact]
    public void Encode_Tiff_MultiPage_Strips_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] {
            255, 0, 0, 255,
            0, 255, 0, 255
        }, 1, 2, 4);
        var page2 = new TiffRgba32Page(new byte[] {
            0, 0, 255, 255,
            255, 255, 0, 255
        }, 1, 2, 4);

        var tiff = TiffWriter.WriteRgba32Pages(rowsPerStrip: 1, compression: TiffCompression.None, usePredictor: false, page1, page2);
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
    public void Encode_Tiff_MultiPage_PackBits_Strips_RoundTrip() {
        var page1 = new TiffRgba32Page(new byte[] {
            10, 10, 10, 255, 10, 10, 10, 255,
            20, 20, 20, 255, 20, 20, 20, 255
        }, 2, 2, 8);
        var page2 = new TiffRgba32Page(new byte[] {
            30, 30, 30, 255, 30, 30, 30, 255,
            40, 40, 40, 255, 40, 40, 40, 255
        }, 2, 2, 8);

        var tiff = TiffWriter.WriteRgba32Pages(rowsPerStrip: 1, compression: TiffCompression.PackBits, usePredictor: false, page1, page2);
        var pages = TiffReader.DecodePagesRgba32(tiff);

        Assert.Equal(2, pages.Length);
        Assert.Equal(10, pages[0].Rgba[0]);
        Assert.Equal(10, pages[0].Rgba[1]);
        Assert.Equal(10, pages[0].Rgba[2]);
        Assert.Equal(30, pages[1].Rgba[0]);
        Assert.Equal(30, pages[1].Rgba[1]);
        Assert.Equal(30, pages[1].Rgba[2]);
    }
}
