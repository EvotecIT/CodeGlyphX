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

    [Fact]
    public void Webp_ManagedEncode_Vp8L_ColorIndexingPalette_RoundTripsSmallPalette() {
        const int width = 5;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,
            32, 32, 32, 255,
            64, 64, 64, 255,
            96, 96, 96, 255,
            128, 128, 128, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_FallsBackWhenPaletteTooLarge() {
        const int width = 17;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var v = (byte)(x * 8);
            var i = x * 4;
            rgba[i] = v;
            rgba[i + 1] = v;
            rgba[i + 2] = v;
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_RunLengthBackref_RoundTripsSolidRun() {
        const int width = 12;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var i = x * 4;
            rgba[i] = 10;
            rgba[i + 1] = 20;
            rgba[i + 2] = 30;
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }
}
