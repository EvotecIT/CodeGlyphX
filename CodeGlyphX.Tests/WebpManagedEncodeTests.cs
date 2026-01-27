using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpManagedEncodeTests {
    [Fact]
    public void Webp_ManagedEncode_Vp8L_LiteralOnly_RoundTripsBinaryImage() {
        const int width = 2;
        const int height = 2;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,       255, 255, 255, 255,
            255, 255, 255, 255, 0, 0, 0, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_LiteralOnly_RoundTripsMultipleChannelValues() {
        const int width = 3;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,
            64, 0, 0, 255,
            128, 0, 0, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }
}
