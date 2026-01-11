using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodeConfidenceTests {
    [Fact]
    public void DecodeInfo_Confidence_IsInRange() {
        var pixels = QrEasy.RenderPixels("CONFIDENCE", out var width, out var height, out var stride);

        Assert.True(QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, out var info));
        Assert.Equal("CONFIDENCE", decoded.Text);
        Assert.InRange(info.Confidence, 0.0, 1.0);
        Assert.True(info.Confidence > 0.5);
    }
}
