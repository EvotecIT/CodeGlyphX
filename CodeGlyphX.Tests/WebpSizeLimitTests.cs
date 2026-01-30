using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpSizeLimitTests {
    [Fact]
    public void WebpReader_SizeLimit_IsConfigurable() {
        var payload = WebpVp8TestHelper.BuildKeyframePayload(1, 1, WebpVp8TestHelper.CreateBoolData(8));
        var webp = WebpVp8TestHelper.BuildWebpVp8(payload);

        var originalLimit = WebpReader.MaxWebpBytes;
        try {
            WebpReader.MaxWebpBytes = webp.Length - 1;
            Assert.False(WebpReader.TryReadDimensions(webp, out _, out _));
            Assert.Throws<FormatException>(() => WebpReader.DecodeRgba32(webp, out _, out _));

            WebpReader.MaxWebpBytes = webp.Length;
            Assert.True(WebpReader.TryReadDimensions(webp, out var width, out var height));
            Assert.Equal(1, width);
            Assert.Equal(1, height);
        } finally {
            WebpReader.MaxWebpBytes = originalLimit;
        }
    }
}
