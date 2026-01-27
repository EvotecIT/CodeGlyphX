using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class JpegRoundTripTests {
    [Fact]
    public void WriterReader_PreservesContrast() {
        const int width = 64;
        const int height = 64;
        var stride = width * 4;
        var rgba = new byte[height * stride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var blockY = y / 8;
            for (var x = 0; x < width; x++) {
                var blockX = x / 8;
                var isWhite = ((blockX + blockY) & 1) == 0;
                var v = isWhite ? (byte)255 : (byte)0;
                var p = row + x * 4;
                rgba[p + 0] = v;
                rgba[p + 1] = v;
                rgba[p + 2] = v;
                rgba[p + 3] = 255;
            }
        }

        var jpeg = JpegWriter.WriteRgba(width, height, rgba, stride, quality: 90);
        var decoded = JpegReader.DecodeRgba32(jpeg, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);

        var min = 255;
        var max = 0;
        var minR = 255;
        var minG = 255;
        var minB = 255;
        var maxR = 0;
        var maxG = 0;
        var maxB = 0;
        for (var i = 0; i < decoded.Length; i += 4) {
            var r = decoded[i + 0];
            var g = decoded[i + 1];
            var b = decoded[i + 2];
            var luma = (77 * r + 150 * g + 29 * b + 128) >> 8;
            if (luma < min) min = luma;
            if (luma > max) max = luma;
            if (r < minR) minR = r;
            if (g < minG) minG = g;
            if (b < minB) minB = b;
            if (r > maxR) maxR = r;
            if (g > maxG) maxG = g;
            if (b > maxB) maxB = b;
        }

        Assert.True(min <= 80, $"Min luma too high: {min}. Channels min/max: R={minR}/{maxR}, G={minG}/{maxG}, B={minB}/{maxB}.");
        Assert.True(max >= 200, $"Max luma too low: {max}. Channels min/max: R={minR}/{maxR}, G={minG}/{maxG}, B={minB}/{maxB}.");
    }
}
