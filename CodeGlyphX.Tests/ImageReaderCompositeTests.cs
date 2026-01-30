using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageReaderCompositeTests {
    [Fact]
    public void ImageReader_DecodeComposite_Gif_Succeeds() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        var rgba = ImageReader.DecodeRgba32Composite(gif, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
    }

    [Fact]
    public void ImageReader_TryDecodeComposite_Gif_Succeeds() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

        Assert.True(ImageReader.TryDecodeRgba32Composite(gif, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
    }
}
