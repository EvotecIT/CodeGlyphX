using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffStripEncodeTests {
    [Fact]
    public void Encode_Tiff_Uses_Multiple_Strips() {
        const int width = 3;
        const int height = 3;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i + 0] = 10;
            rgba[i + 1] = 20;
            rgba[i + 2] = 30;
            rgba[i + 3] = 255;
        }

        using var ms = new System.IO.MemoryStream();
        TiffWriter.WriteRgba32(ms, width, height, rgba, width * 4, rowsPerStrip: 1);
        var data = ms.ToArray();

        var ifdOffset = ReadU32(data, 4);
        var entryCount = ReadU16(data, (int)ifdOffset);
        var entriesOffset = (int)ifdOffset + 2;
        var stripCount = 0u;
        for (var i = 0; i < entryCount; i++) {
            var entryOffset = entriesOffset + i * 12;
            var tag = ReadU16(data, entryOffset);
            if (tag == 273) {
                stripCount = ReadU32(data, entryOffset + 4);
                break;
            }
        }

        Assert.Equal((uint)height, stripCount);
        Assert.True(TiffReader.TryDecodeRgba32(data, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba.Length, decoded.Length);
    }

    private static ushort ReadU16(byte[] data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadU32(byte[] data, int offset) {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}
