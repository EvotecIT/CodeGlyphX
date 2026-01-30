using System;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffBilevelDecodeTests {
    [Fact]
    public void Decode_Bilevel_Tiff_BlackIsZero() {
        var tiff = BuildBilevelTiff(width: 8, height: 2, photometric: 1, packedRows: new byte[] { 0xAA, 0x55 });
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(8, width);
        Assert.Equal(2, height);

        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[4]);
        Assert.Equal(255, rgba[8]);
        Assert.Equal(0, rgba[12]);

        var row1 = 1 * width * 4;
        Assert.Equal(0, rgba[row1 + 0]);
        Assert.Equal(255, rgba[row1 + 4]);
        Assert.Equal(0, rgba[row1 + 8]);
        Assert.Equal(255, rgba[row1 + 12]);
    }

    [Fact]
    public void Decode_Bilevel_Tiff_WhiteIsZero() {
        var tiff = BuildBilevelTiff(width: 8, height: 1, photometric: 0, packedRows: new byte[] { 0x80 });
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(8, width);
        Assert.Equal(1, height);

        Assert.Equal(0, rgba[0]);
        Assert.Equal(255, rgba[4]);
    }

    private static byte[] BuildBilevelTiff(int width, int height, ushort photometric, byte[] packedRows) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var bytesPerRow = (width + 7) / 8;
        if (packedRows.Length != bytesPerRow * height) {
            throw new ArgumentException("Packed row data does not match dimensions.", nameof(packedRows));
        }

        const int entryCount = 9;
        const int headerSize = 8;
        var ifdSize = 2 + entryCount * 12 + 4;
        var imageOffset = headerSize + ifdSize;
        var data = new byte[imageOffset + packedRows.Length];

        data[0] = (byte)'I';
        data[1] = (byte)'I';
        data[2] = 42;
        data[3] = 0;
        WriteUInt32LE(data, 4, headerSize);

        WriteUInt16LE(data, headerSize, entryCount);
        var entryOffset = headerSize + 2;

        WriteEntry(data, ref entryOffset, 256, 3, 1, (uint)width); // ImageWidth
        WriteEntry(data, ref entryOffset, 257, 3, 1, (uint)height); // ImageLength
        WriteEntry(data, ref entryOffset, 258, 3, 1, 1); // BitsPerSample
        WriteEntry(data, ref entryOffset, 259, 3, 1, 1); // Compression
        WriteEntry(data, ref entryOffset, 262, 3, 1, photometric); // PhotometricInterpretation
        WriteEntry(data, ref entryOffset, 273, 4, 1, (uint)imageOffset); // StripOffsets
        WriteEntry(data, ref entryOffset, 277, 3, 1, 1); // SamplesPerPixel
        WriteEntry(data, ref entryOffset, 278, 3, 1, (uint)height); // RowsPerStrip
        WriteEntry(data, ref entryOffset, 279, 4, 1, (uint)packedRows.Length); // StripByteCounts

        WriteUInt32LE(data, headerSize + 2 + entryCount * 12, 0);

        Buffer.BlockCopy(packedRows, 0, data, imageOffset, packedRows.Length);
        return data;
    }

    private static void WriteEntry(byte[] data, ref int offset, ushort tag, ushort type, uint count, uint value) {
        WriteUInt16LE(data, offset, tag);
        WriteUInt16LE(data, offset + 2, type);
        WriteUInt32LE(data, offset + 4, count);
        WriteUInt32LE(data, offset + 8, value);
        offset += 12;
    }

    private static void WriteUInt16LE(byte[] data, int offset, ushort value) {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32LE(byte[] data, int offset, uint value) {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }
}
