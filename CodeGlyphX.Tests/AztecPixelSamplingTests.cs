using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecPixelSamplingTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SmallPhysicalModules_PreservePayload(int pitch) {
        const string expected = "SMALL AZTEC 42";
        var modules = AztecCode.Encode(expected).Modules;
        var pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions {
            ModuleSize = pitch, QuietZone = 2
        }, out var width, out var height, out var stride);

        Assert.True(AztecCode.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal(expected, text);
    }

    [Fact]
    public void IndependentTwoPixelSymbol_DecodesExactPayload() {
        Assert.True(AztecCode.TryDecodeImage(ReadFixture("two-pixel-modules.png"), out var text));
        Assert.Equal("Code 2D!", text);
    }

    [Theory]
    [InlineData("photograph-10.png", "mailto:zxing@googlegroups.com")]
    [InlineData("photograph-13.png", "mailto:zxing@googlegroups.com")]
    [InlineData("photograph-14.png", "mailto:zxing@googlegroups.com")]
    [InlineData("photograph-15.png", "mailto:zxing@googlegroups.com")]
    [InlineData("photograph-17.png", "http://code.google.com/p/zxing/source/browse/trunk/android/src/com/google/zxing/client/android/result/URIResultHandler.java")]
    [InlineData("photograph-19.png", "http://code.google.com/p/zxing/source/browse/trunk/android/src/com/google/zxing/client/android/result/URIResultHandler.java")]
    public void IndependentPhotographs_DecodeExactPayload(string name, string expected) {
        Assert.True(AztecCode.TryDecodeImage(ReadFixture(name), out var text));
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void PhotographOrientation_PreservesPayload(bool mirrored, bool inverted) {
        var source = ImageReader.DecodeRgba32(ReadFixture("photograph-19.png"), out var width, out var height);
        var pixels = new byte[source.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var from = (y * width + (mirrored ? width - 1 - x : x)) * 4;
            var to = (y * width + x) * 4;
            for (var channel = 0; channel < 3; channel++) pixels[to + channel] = inverted ? (byte)(255 - source[from + channel]) : source[from + channel];
            pixels[to + 3] = source[from + 3];
        }
        Assert.True(AztecCode.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out var text));
        Assert.Equal("http://code.google.com/p/zxing/source/browse/trunk/android/src/com/google/zxing/client/android/result/URIResultHandler.java", text);
    }

    private static byte[] ReadFixture(string name) {
        using var stream = typeof(AztecPixelSamplingTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Aztec." + name);
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return buffer.ToArray();
    }
}
