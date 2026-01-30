using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffPredictorEncodeTests {
    [Fact]
    public void Encode_Tiff_Predictor_Deflate_RoundTrip() {
        const int width = 5;
        const int height = 2;
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var idx = (y * width + x) * 4;
                rgba[idx + 0] = (byte)(x * 30);
                rgba[idx + 1] = (byte)(y * 40);
                rgba[idx + 2] = (byte)(x * 20);
                rgba[idx + 3] = 255;
            }
        }

        using var ms = new System.IO.MemoryStream();
        TiffWriter.WriteRgba32(ms, width, height, rgba, width * 4, rowsPerStrip: 1, compression: TiffCompression.Deflate, usePredictor: true);
        var data = ms.ToArray();

        Assert.True(TiffReader.TryDecodeRgba32(data, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba.Length, decoded.Length);
        Assert.Equal(rgba[0], decoded[0]);
        Assert.Equal(rgba[1], decoded[1]);
        Assert.Equal(rgba[2], decoded[2]);
    }
}
