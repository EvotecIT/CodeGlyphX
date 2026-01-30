using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiPageEncodeTests {
    [Fact]
    public void Tiff_ManagedEncode_MultiPage_RoundTrips() {
        const int width1 = 2;
        const int height1 = 2;
        const int stride1 = width1 * 4;
        var page1 = new byte[] {
            0, 0, 0, 255,       255, 0, 0, 255,
            0, 255, 0, 255,     0, 0, 255, 255
        };

        const int width2 = 3;
        const int height2 = 1;
        const int stride2 = width2 * 4;
        var page2 = new byte[] {
            10, 20, 30, 0,
            40, 50, 60, 128,
            70, 80, 90, 255
        };

        var pages = new[] {
            new TiffRgba32Page(page1, width1, height1, stride1, TiffCompressionMode.None),
            new TiffRgba32Page(page2, width2, height2, stride2, TiffCompressionMode.Deflate)
        };

        var tiff = TiffWriter.WriteRgba32(pages);

        Assert.True(ImageReader.TryReadPageCount(tiff, out var pageCount));
        Assert.Equal(2, pageCount);

        var decoded1 = ImageReader.DecodeRgba32(tiff, 0, out var decodedWidth1, out var decodedHeight1);
        Assert.Equal(width1, decodedWidth1);
        Assert.Equal(height1, decodedHeight1);
        Assert.Equal(page1, decoded1);

        var decoded2 = ImageReader.DecodeRgba32(tiff, 1, out var decodedWidth2, out var decodedHeight2);
        Assert.Equal(width2, decodedWidth2);
        Assert.Equal(height2, decodedHeight2);
        Assert.Equal(page2, decoded2);
    }
}
