using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecTests {
    [Theory]
    [InlineData("HELLO AZTEC")]
    [InlineData("Aztec 12345")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP")]
    public void Aztec_Roundtrip(string text) {
        var matrix = AztecCode.Encode(text);
        Assert.True(AztecCode.TryDecode(matrix, out var decoded));
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void Aztec_Decode_RotatedPixels() {
        var matrix = AztecCode.Encode("AZTEC-ROTATE");
        var pixels = MatrixPngRenderer.RenderPixels(matrix, new MatrixPngRenderOptions {
            ModuleSize = 3,
            QuietZone = 2
        }, out var width, out var height, out _);

        var rotated = Rotate90Rgba(pixels, width, height, out var rotW, out var rotH);
        Assert.True(AztecCode.TryDecode(rotated, rotW, rotH, rotW * 4, PixelFormat.Rgba32, out var text));
        Assert.Equal("AZTEC-ROTATE", text);
    }

    private static byte[] Rotate90Rgba(byte[] pixels, int width, int height, out int outWidth, out int outHeight) {
        outWidth = height;
        outHeight = width;
        var rotated = new byte[outWidth * outHeight * 4];

        for (var y = 0; y < height; y++) {
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = height - 1 - y;
                var ny = x;
                var dst = (ny * outWidth + nx) * 4;
                rotated[dst + 0] = pixels[src + 0];
                rotated[dst + 1] = pixels[src + 1];
                rotated[dst + 2] = pixels[src + 2];
                rotated[dst + 3] = pixels[src + 3];
            }
        }

        return rotated;
    }
}
