using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodeProfileTests {
    [Fact]
    public void Decode_Profiles_Work() {
        var pixels = QrEasy.RenderPixels("PROFILE", out var width, out var height, out var stride);

        var fast = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast };
        Assert.True(QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decodedFast, out var infoFast, fast));
        Assert.Equal("PROFILE", decodedFast.Text);
        Assert.Equal(1, infoFast.Scale);

        var balanced = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced };
        Assert.True(QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decodedBalanced, out var infoBalanced, balanced));
        Assert.Equal("PROFILE", decodedBalanced.Text);
        Assert.True(infoBalanced.Scale >= 1);

        var robust = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust };
        Assert.True(QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decodedRobust, out var infoRobust, robust));
        Assert.Equal("PROFILE", decodedRobust.Text);
        Assert.True(infoRobust.Scale >= 1);
    }
}
