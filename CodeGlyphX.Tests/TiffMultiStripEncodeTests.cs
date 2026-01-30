using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiStripEncodeTests {
    [Fact]
    public void Tiff_ManagedEncode_MultiStrip_RoundTrips() {
        const int width = 3;
        const int height = 5;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var i = y * stride + x * 4;
                rgba[i] = (byte)(x * 40);
                rgba[i + 1] = (byte)(y * 30);
                rgba[i + 2] = (byte)(255 - x * 40);
                rgba[i + 3] = 255;
            }
        }

        var tiff = TiffWriter.WriteRgba32(width, height, rgba, stride, TiffCompressionMode.None, rowsPerStrip: 2);

        Assert.Equal(3, ReadStripCount(tiff));

        var decoded = ImageReader.DecodeRgba32(tiff, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    private static int ReadStripCount(ReadOnlySpan<byte> tiff) {
        Assert.True(TiffReader.IsTiff(tiff));
        var little = tiff[0] == (byte)'I';
        var ifdOffset = ReadUInt32(tiff, 4, little);
        var entryCount = ReadUInt16(tiff, (int)ifdOffset, little);
        var entriesOffset = (int)ifdOffset + 2;
        for (var i = 0; i < entryCount; i++) {
            var entryOffset = entriesOffset + i * 12;
            var tag = ReadUInt16(tiff, entryOffset, little);
            if (tag != 273) continue;
            var count = ReadUInt32(tiff, entryOffset + 4, little);
            return (int)count;
        }
        return 0;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool little) {
        if (little) {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool little) {
        if (little) {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }
}
