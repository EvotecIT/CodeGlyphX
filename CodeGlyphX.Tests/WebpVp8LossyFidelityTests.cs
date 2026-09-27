using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]
public sealed class WebpVp8LossyFidelityTests {
    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(127, 127, 127)]
    public void LossyEncodingPreservesSolidColors(byte red, byte green, byte blue) {
        var source = new byte[16 * 16 * 4];
        for (int i = 0; i < source.Length; i += 4) {
            source[i] = red; source[i + 1] = green; source[i + 2] = blue; source[i + 3] = 255;
        }
        byte[] encoded = WebpWriter.WriteRgba32Lossy(16, 16, source, 64, 80);
        byte[] decoded = WebpReader.DecodeRgba32(encoded, out int width, out int height);
        Assert.Equal(16, width); Assert.Equal(16, height);
        for (int i = 0; i < source.Length; i++) {
            Assert.InRange(Math.Abs(source[i] - decoded[i]), 0, i % 4 == 3 ? 0 : 3);
        }
    }

    [Theory]
    [InlineData(48, 32)]
    [InlineData(65, 49)]
    public void LossyEncodingPreservesSmoothColorImage(int width, int height) {
        var source = new byte[width * height * 4];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
            int i = (y * width + x) * 4;
            source[i] = (byte)Math.Min(255, x * 3 + y * 2);
            source[i + 1] = (byte)Math.Min(255, x * 2 + y * 4);
            source[i + 2] = (byte)(x + y * 3); source[i + 3] = 255;
        }
        byte[] encoded = WebpWriter.WriteRgba32Lossy(width, height, source, width * 4, 80);
        byte[] decoded = WebpReader.DecodeRgba32(encoded, out int actualWidth, out int actualHeight);
        Assert.Equal(width, actualWidth); Assert.Equal(height, actualHeight);
        long error = 0;
        for (int i = 0; i < source.Length; i++) {
            if (i % 4 == 3) Assert.Equal(source[i], decoded[i]);
            else error += Math.Abs(source[i] - decoded[i]);
        }
        double mean = (double)error / (width * height * 3);
        Assert.True(mean <= 3, $"Mean RGB error {mean:F4} exceeds 3 at quality 80.");
    }
    [Theory]
    [InlineData(16384, 1)]
    [InlineData(1, 16384)]
    public void LossyWriterPreservesDimensionsBeyondVp8Limit(int width, int height) {
        var source = new byte[width * height * 4];
        for (int i = 0; i < source.Length; i += 4) {
            source[i] = 255; source[i + 3] = 255;
        }
        byte[] encoded = WebpWriter.WriteRgba32Lossy(width, height, source, width * 4, 80);
        byte[] decoded = WebpReader.DecodeRgba32(encoded, out int actualWidth, out int actualHeight);
        Assert.Equal(width, actualWidth); Assert.Equal(height, actualHeight);
        Assert.Equal(source.Length, decoded.Length);
        for (int i = 0; i < decoded.Length; i += 4) {
            Assert.InRange(decoded[i], 240, 255);
            Assert.Equal(255, decoded[i + 3]);
        }
    }

    [Fact]
    public void LossyAnimationPreservesOversizedFrameAndAlpha() {
        const int width = 16384;
        var source = new byte[width * 4];
        for (int i = 0; i < source.Length; i += 4) {
            source[i] = 255; source[i + 3] = 180;
        }
        var frame = new WebpAnimationFrame(source, width, 1, width * 4, durationMs: 100, blend: false);
        byte[] encoded = WebpWriter.WriteAnimationRgba32Lossy(width, 1, new[] { frame }, default, 80);
        byte[] decoded = WebpReader.DecodeRgba32(encoded, out int actualWidth, out int actualHeight);
        Assert.Equal(width, actualWidth); Assert.Equal(1, actualHeight);
        Assert.Equal(source.Length, decoded.Length);
        for (int i = 0; i < decoded.Length; i += 4) {
            Assert.InRange(decoded[i], 240, 255);
            Assert.Equal(180, decoded[i + 3]);
        }
    }

}
