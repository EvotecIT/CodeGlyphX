using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]
public sealed class WebpVp8IndependentProducerTests {
    // Produced independently with Pillow/libwebp, quality 80, method 6.
    // Expected pixels were decoded by libwebp, not by CodeGlyphX's encoder.
    [Theory]
    [InlineData("UklGRjwAAABXRUJQVlA4IDAAAADQAQCdASoQABAAAUAmJaACdLoB+AADsAD+8ut//NgVzXPv9//S4P0uD9Lg/9KQAAA=", 255, 1, 0)]
    [InlineData("UklGRiQAAABXRUJQVlA4IBgAAABQAQCdASoQABAAAUAmJaQABHQAAP4AAAA=", 127, 127, 127)]
    public void DecodeIndependentSolidImagePreservesPixels(string base64, int red, int green, int blue) {
        byte[] pixels = WebpReader.DecodeRgba32(Convert.FromBase64String(base64), out int width, out int height);
        Assert.Equal(16, width);
        Assert.Equal(16, height);
        for (int offset = 0; offset < pixels.Length; offset += 4) {
            Assert.InRange((int)pixels[offset], Math.Max(0, red - 1), Math.Min(255, red + 1));
            Assert.InRange((int)pixels[offset + 1], Math.Max(0, green - 1), Math.Min(255, green + 1));
            Assert.InRange((int)pixels[offset + 2], Math.Max(0, blue - 1), Math.Min(255, blue + 1));
            Assert.Equal(255, pixels[offset + 3]);
        }
    }
    [Theory]
    [InlineData("independent-vp8-pattern", 48, 32)]
    [InlineData("independent-vp8-odd-color", 65, 49)]
    public void DecodeIndependentPatternPreservesReconstructedPixels(string fixture, int expectedWidth, int expectedHeight) {
        byte[] encoded = ReadFixture(fixture + ".webp");
        byte[] expected = ReadFixture(fixture + ".rgba");
        byte[] actual = WebpReader.DecodeRgba32(encoded, out int width, out int height);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expected.Length, actual.Length);
        long totalError = 0;
        int maximumError = 0;
        for (int i = 0; i < actual.Length; i++) {
            int error = Math.Abs(actual[i] - expected[i]);
            totalError += error;
            maximumError = Math.Max(maximumError, error);
        }
        Assert.True(totalError <= actual.Length, $"Mean channel error {(double)totalError / actual.Length:F4} exceeds 1.");
        Assert.True(maximumError <= 3, $"Maximum channel error {maximumError} exceeds 3.");
    }

    private static byte[] ReadFixture(string name) {
        using Stream input = typeof(WebpVp8IndependentProducerTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Webp." + name)!;
        Assert.NotNull(input);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
