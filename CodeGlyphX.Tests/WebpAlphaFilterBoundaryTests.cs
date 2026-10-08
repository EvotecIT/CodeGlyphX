using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]
public sealed class WebpAlphaFilterBoundaryTests {
    // VP8 pixels were produced by Pillow/libwebp 1.6.0. The uncompressed ALPH
    // chunks follow the container specification and were independently decoded
    // with libwebp to confirm every expected alpha sample.
    [Theory]
    [InlineData("UklGRmIAAABXRUJQVlA4WAoAAAAQAAAAAwAAAgAAQUxQSA0AAAAEGMBIoMBAqGBQoHCAAFZQOCAuAAAAEAIAnQEqBAADAAEAHCWgAnS6AfgB+AADyAD+7tPf/pNGqfj7S3/1ODBz5+dkAA==")]
    [InlineData("UklGRmIAAABXRUJQVlA4WAoAAAAQAAAAAwAAAgAAQUxQSA0AAAAIGMBIoMBAoGBQsHiYAFZQOCAuAAAAEAIAnQEqBAADAAEAHCWgAnS6AfgB+AADyAD+7tPf/pNGqfj7S3/1ODBz5+dkAA==")]
    [InlineData("UklGRmIAAABXRUJQVlA4WAoAAAAQAAAAAwAAAgAAQUxQSA0AAAAMGMBIoMAZwCFQyDm4AFZQOCAuAAAAEAIAnQEqBAADAAEAHCWgAnS6AfgB+AADyAD+7tPf/pNGqfj7S3/1ODBz5+dkAA==")]
    public void DecodeIndependentFilteredAlphaPreservesBoundarySamples(string base64) {
        byte[] expected = { 24, 216, 32, 192, 216, 24, 192, 32, 40, 200, 56, 184 };
        byte[] decoded = WebpReader.DecodeRgba32(Convert.FromBase64String(base64), out int width, out int height);

        Assert.Equal(4, width);
        Assert.Equal(3, height);
        for (int index = 0; index < expected.Length; index++) {
            Assert.Equal(expected[index], decoded[index * 4 + 3]);
        }
    }

    [Fact]
    public void EncodeLossyAlphaMarksTransparencyWithoutMetadata() {
        byte[] rgba = CreateAlternatingAlpha(16, 16, alternatingColumns: false);
        byte[] encoded = WebpWriter.WriteRgba32Lossy(16, 16, rgba, 16 * 4, quality: 85);

        Assert.Equal(0x10, ReadChunk(encoded, "VP8X")[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EncodeLossyAlphaUsesBoundaryResiduals(bool alternatingColumns) {
        const int width = 16;
        const int height = 16;
        byte[] rgba = CreateAlternatingAlpha(width, height, alternatingColumns);
        byte[] encoded = WebpWriter.WriteRgba32Lossy(width, height, rgba, width * 4, quality: 85);
        byte[] alpha = ReadChunk(encoded, "ALPH");

        Assert.Equal(0, alpha[0] & 3); // The managed encoder writes uncompressed ALPH samples.
        int filter = (alpha[0] >> 2) & 3;
        for (int x = 0; x < width; x++) {
            int value = rgba[x * 4 + 3];
            int previous = filter == 0 || x == 0 ? 0 : rgba[(x - 1) * 4 + 3];
            Assert.Equal(unchecked((byte)(value - previous)), alpha[1 + x]);
        }
        for (int y = 1; y < height; y++) {
            int value = rgba[(y * width) * 4 + 3];
            int previous = filter == 0 ? 0 : rgba[((y - 1) * width) * 4 + 3];
            Assert.Equal(unchecked((byte)(value - previous)), alpha[1 + y * width]);
        }

        byte[] decoded = WebpReader.DecodeRgba32(encoded, out int decodedWidth, out int decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba.Length, decoded.Length);
        for (int offset = 3; offset < rgba.Length; offset += 4) {
            Assert.Equal(rgba[offset], decoded[offset]);
        }
    }

    private static byte[] CreateAlternatingAlpha(int width, int height, bool alternatingColumns) {
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int offset = (y * width + x) * 4;
                rgba[offset] = 40;
                rgba[offset + 1] = 120;
                rgba[offset + 2] = 180;
                rgba[offset + 3] = ((alternatingColumns ? x : y) & 1) == 0 ? (byte)24 : (byte)216;
            }
        }
        return rgba;
    }

    private static byte[] ReadChunk(byte[] encoded, string name) {
        for (int offset = 12; offset + 8 <= encoded.Length;) {
            int length = BitConverter.ToInt32(encoded, offset + 4);
            if (System.Text.Encoding.ASCII.GetString(encoded, offset, 4) == name) {
                var payload = new byte[length];
                Buffer.BlockCopy(encoded, offset + 8, payload, 0, length);
                return payload;
            }
            offset += 8 + length + (length & 1);
        }
        throw new Xunit.Sdk.XunitException($"Expected {name} chunk was not written.");
    }
}
