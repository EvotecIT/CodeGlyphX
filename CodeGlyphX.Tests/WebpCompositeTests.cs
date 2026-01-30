using CodeGlyphX.Rendering;
using CodeGlyphX.Tests;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpCompositeTests {
    [Fact]
    public void ImageReader_DecodeComposite_Webp_Succeeds() {
        var payload = WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 12, g: 34, b: 56, a: 128);
        var webp = WebpAnimationDecodeTests.BuildAnimatedWebp(
            payload,
            canvasWidth: 2,
            canvasHeight: 2,
            frameX: 0,
            frameY: 0,
            frameWidth: 1,
            frameHeight: 1,
            blend: true,
            bgraBackground: 0);

        var rgba = ImageReader.DecodeRgba32Composite(webp, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(16, rgba.Length);
    }

    [Fact]
    public void ImageReader_TryDecodeComposite_Webp_Succeeds() {
        var payload = WebpAnimationDecodeTests.BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 1, g: 2, b: 3, a: 255);
        var webp = WebpAnimationDecodeTests.BuildAnimatedWebp(
            payload,
            canvasWidth: 1,
            canvasHeight: 1,
            frameX: 0,
            frameY: 0,
            frameWidth: 1,
            frameHeight: 1,
            blend: false,
            bgraBackground: 0);

        Assert.True(ImageReader.TryDecodeRgba32Composite(webp, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
    }
}
