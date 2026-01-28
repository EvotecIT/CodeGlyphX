using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpAnimationEncodeTests {
    [Fact]
    public void Webp_AnimationEncode_FirstFrameDecodes() {
        const int width = 4;
        const int height = 4;

        var frame1 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 255, 0, 0, 255),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var frame2 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 0, 255, 0, 255),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var webp = WebpWriter.WriteAnimationRgba32(
            width,
            height,
            new[] { frame1, frame2 },
            new WebpAnimationOptions(loopCount: 0, backgroundBgra: 0));

        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decoded.Length);
        AssertAllPixels(decoded, 255, 0, 0, 255);
    }

    private static byte[] CreateSolidRgba(int width, int height, byte r, byte g, byte b, byte a) {
        var data = new byte[checked(width * height * 4)];
        for (var i = 0; i < data.Length; i += 4) {
            data[i] = r;
            data[i + 1] = g;
            data[i + 2] = b;
            data[i + 3] = a;
        }
        return data;
    }

    private static void AssertAllPixels(byte[] rgba, byte r, byte g, byte b, byte a) {
        for (var i = 0; i < rgba.Length; i += 4) {
            Assert.Equal(r, rgba[i]);
            Assert.Equal(g, rgba[i + 1]);
            Assert.Equal(b, rgba[i + 2]);
            Assert.Equal(a, rgba[i + 3]);
        }
    }
}
