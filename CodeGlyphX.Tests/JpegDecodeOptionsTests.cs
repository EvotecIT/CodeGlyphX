using System;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class JpegDecodeOptionsTests {
    [Fact]
    public void Decode_AllowTruncated_ReturnsImage() {
        var rgba = CreateTestRgba(24, 24);
        var jpeg = JpegWriter.WriteRgba(24, 24, rgba, 24 * 4, new JpegEncodeOptions { Quality = 80 });
        var truncated = new byte[jpeg.Length - 10];
        Array.Copy(jpeg, 0, truncated, 0, truncated.Length);

        var decoded = JpegReader.DecodeRgba32(truncated, out var width, out var height, new JpegDecodeOptions(allowTruncated: true));
        Assert.Equal(24, width);
        Assert.Equal(24, height);
        Assert.Equal(width * height * 4, decoded.Length);
    }

    [Fact]
    public void Decode_HighQualityChroma_ReturnsImage() {
        var rgba = CreateTestRgba(32, 16);
        var jpeg = JpegWriter.WriteRgba(32, 16, rgba, 32 * 4, new JpegEncodeOptions {
            Quality = 75,
            Subsampling = JpegSubsampling.Y420
        });

        var decoded = JpegReader.DecodeRgba32(jpeg, out var width, out var height, new JpegDecodeOptions(highQualityChroma: true));
        Assert.Equal(32, width);
        Assert.Equal(16, height);
        Assert.Equal(width * height * 4, decoded.Length);
    }

    private static byte[] CreateTestRgba(int width, int height) {
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var i = (y * width + x) * 4;
                rgba[i + 0] = (byte)(x * 5);
                rgba[i + 1] = (byte)(y * 11);
                rgba[i + 2] = (byte)(255 - x * 3);
                rgba[i + 3] = 255;
            }
        }
        return rgba;
    }
}
