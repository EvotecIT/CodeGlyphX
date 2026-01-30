using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffPlanarDecodeTests {
    [Fact]
    public void Decode_Tiff_Planar_Rgb() {
        var tiff = BuildPlanarRgbTiff();

        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(0x11, rgba[0]);
        Assert.Equal(0x22, rgba[1]);
        Assert.Equal(0x33, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [Fact]
    public void Encode_Tiff_Planar_RoundTrip() {
        var rgba = new byte[] { 10, 20, 30, 255 };
        var tiff = TiffWriter.WriteRgba32Planar(width: 1, height: 1, rgba, stride: 4);

        var decoded = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(10, decoded[0]);
        Assert.Equal(20, decoded[1]);
        Assert.Equal(30, decoded[2]);
        Assert.Equal(255, decoded[3]);
    }

    [Fact]
    public void Encode_Tiff_Planar_Deflate_RoundTrip() {
        var rgba = new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 };
        using var ms = new System.IO.MemoryStream();
        TiffWriter.WriteRgba32Planar(
            ms,
            width: 2,
            height: 1,
            rgba,
            stride: 8,
            rowsPerStrip: 1,
            compression: TiffCompression.Deflate,
            usePredictor: true);
        var tiff = ms.ToArray();

        var decoded = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(1, decoded[0]);
        Assert.Equal(2, decoded[1]);
        Assert.Equal(3, decoded[2]);
        Assert.Equal(4, decoded[4]);
        Assert.Equal(5, decoded[5]);
        Assert.Equal(6, decoded[6]);
    }

    private static byte[] BuildPlanarRgbTiff() {
        const int entryCount = 10;
        const int ifdSize = 2 + entryCount * 12 + 4;
        const int headerSize = 8;
        var bitsOffset = headerSize + ifdSize;
        var stripOffsetsOffset = bitsOffset + 6;
        var stripByteCountsOffset = stripOffsetsOffset + 12;
        var dataOffset = stripByteCountsOffset + 12;

        var data = new byte[dataOffset + 3];
        WriteAscii(data, 0, "II");
        WriteU16(data, 2, 42);
        WriteU32(data, 4, headerSize);

        WriteU16(data, headerSize, entryCount);
        var entryBase = headerSize + 2;
        var i = 0;
        WriteEntry(data, entryBase + i++ * 12, tag: 256, type: 4, count: 1, value: 1); // width
        WriteEntry(data, entryBase + i++ * 12, tag: 257, type: 4, count: 1, value: 1); // height
        WriteEntry(data, entryBase + i++ * 12, tag: 258, type: 3, count: 3, value: bitsOffset); // bits per sample
        WriteEntry(data, entryBase + i++ * 12, tag: 259, type: 3, count: 1, value: 1); // compression none
        WriteEntry(data, entryBase + i++ * 12, tag: 262, type: 3, count: 1, value: 2); // photometric RGB
        WriteEntry(data, entryBase + i++ * 12, tag: 273, type: 4, count: 3, value: stripOffsetsOffset); // strip offsets
        WriteEntry(data, entryBase + i++ * 12, tag: 277, type: 3, count: 1, value: 3); // samples per pixel
        WriteEntry(data, entryBase + i++ * 12, tag: 278, type: 4, count: 1, value: 1); // rows per strip
        WriteEntry(data, entryBase + i++ * 12, tag: 279, type: 4, count: 3, value: stripByteCountsOffset); // strip byte counts
        WriteEntry(data, entryBase + i++ * 12, tag: 284, type: 3, count: 1, value: 2); // planar configuration separate

        WriteU32(data, entryBase + entryCount * 12, 0); // next IFD

        WriteU16(data, bitsOffset + 0, 8);
        WriteU16(data, bitsOffset + 2, 8);
        WriteU16(data, bitsOffset + 4, 8);

        WriteU32(data, stripOffsetsOffset + 0, (uint)dataOffset);
        WriteU32(data, stripOffsetsOffset + 4, (uint)(dataOffset + 1));
        WriteU32(data, stripOffsetsOffset + 8, (uint)(dataOffset + 2));

        WriteU32(data, stripByteCountsOffset + 0, 1u);
        WriteU32(data, stripByteCountsOffset + 4, 1u);
        WriteU32(data, stripByteCountsOffset + 8, 1u);

        data[dataOffset + 0] = 0x11; // R
        data[dataOffset + 1] = 0x22; // G
        data[dataOffset + 2] = 0x33; // B

        return data;
    }

    private static void WriteEntry(byte[] buffer, int offset, ushort tag, ushort type, uint count, int value) {
        WriteU16(buffer, offset, tag);
        WriteU16(buffer, offset + 2, type);
        WriteU32(buffer, offset + 4, count);
        WriteU32(buffer, offset + 8, (uint)value);
    }

    private static void WriteAscii(byte[] buffer, int offset, string value) {
        buffer[offset] = (byte)value[0];
        buffer[offset + 1] = (byte)value[1];
    }

    private static void WriteU16(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteU32(byte[] buffer, int offset, uint value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
}
