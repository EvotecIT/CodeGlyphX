using System.IO;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiPageTests {
    [Fact]
    public void Decode_Tiff_MultiPage() {
        var tiff = BuildTwoPageTiff();

        var page0 = TiffReader.DecodeRgba32(tiff, 0, out var width0, out var height0);
        Assert.Equal(1, width0);
        Assert.Equal(1, height0);
        Assert.Equal(0, page0[0]);
        Assert.Equal(0, page0[1]);
        Assert.Equal(0, page0[2]);
        Assert.Equal(255, page0[3]);

        var page1 = TiffReader.DecodeRgba32(tiff, 1, out var width1, out var height1);
        Assert.Equal(1, width1);
        Assert.Equal(1, height1);
        Assert.Equal(255, page1[0]);
        Assert.Equal(255, page1[1]);
        Assert.Equal(255, page1[2]);
        Assert.Equal(255, page1[3]);
    }

    [Fact]
    public void Decode_Tiff_Tiles() {
        var tiff = BuildTiledTiff();

        var rgba = TiffReader.DecodeRgba32(tiff, 0, out var width, out var height);
        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(0, rgba[0]);
        Assert.Equal(64, rgba[4]);
        Assert.Equal(128, rgba[8]);
        Assert.Equal(255, rgba[12]);
        Assert.Equal(255, rgba[3]);
        Assert.Equal(255, rgba[7]);
        Assert.Equal(255, rgba[11]);
        Assert.Equal(255, rgba[15]);
    }

    private static byte[] BuildTwoPageTiff() {
        const ushort entryCount = 9;
        const int ifdSize = 2 + entryCount * 12 + 4;
        const int ifd1Offset = 8;
        const int ifd2Offset = ifd1Offset + ifdSize;
        const int dataOffset1 = ifd2Offset + ifdSize;
        const int dataOffset2 = dataOffset1 + 1;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteHeader(bw, ifd1Offset);

        ms.Position = ifd1Offset;
        WriteIfd(bw, entryCount, dataOffset1, nextIfdOffset: ifd2Offset);

        ms.Position = ifd2Offset;
        WriteIfd(bw, entryCount, dataOffset2, nextIfdOffset: 0);

        ms.Position = dataOffset1;
        bw.Write((byte)0);
        ms.Position = dataOffset2;
        bw.Write((byte)255);

        return ms.ToArray();
    }

    private static byte[] BuildTiledTiff() {
        const ushort entryCount = 9;
        const int ifdSize = 2 + entryCount * 12 + 4;
        const int ifdOffset = 8;
        const int dataOffset = ifdOffset + ifdSize;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteHeader(bw, ifdOffset);

        ms.Position = ifdOffset;
        bw.Write(entryCount);
        WriteEntry(bw, TagImageWidth, TypeLong, 1, 2);
        WriteEntry(bw, TagImageLength, TypeLong, 1, 2);
        WriteEntry(bw, TagBitsPerSample, TypeShort, 1, 8);
        WriteEntry(bw, TagCompression, TypeShort, 1, 1);
        WriteEntry(bw, TagPhotometric, TypeShort, 1, 1);
        WriteEntry(bw, TagTileWidth, TypeLong, 1, 2);
        WriteEntry(bw, TagTileLength, TypeLong, 1, 2);
        WriteEntry(bw, TagTileOffsets, TypeLong, 1, (uint)dataOffset);
        WriteEntry(bw, TagTileByteCounts, TypeLong, 1, 4);
        bw.Write((uint)0);

        ms.Position = dataOffset;
        bw.Write((byte)0);
        bw.Write((byte)64);
        bw.Write((byte)128);
        bw.Write((byte)255);

        return ms.ToArray();
    }

    private static void WriteHeader(BinaryWriter bw, int ifdOffset) {
        bw.Write((byte)'I');
        bw.Write((byte)'I');
        bw.Write((ushort)42);
        bw.Write((uint)ifdOffset);
    }

    private static void WriteIfd(BinaryWriter bw, ushort entryCount, int stripOffset, int nextIfdOffset) {
        bw.Write(entryCount);
        WriteEntry(bw, TagImageWidth, TypeLong, 1, 1);
        WriteEntry(bw, TagImageLength, TypeLong, 1, 1);
        WriteEntry(bw, TagBitsPerSample, TypeShort, 1, 8);
        WriteEntry(bw, TagCompression, TypeShort, 1, 1);
        WriteEntry(bw, TagPhotometric, TypeShort, 1, 1);
        WriteEntry(bw, TagStripOffsets, TypeLong, 1, (uint)stripOffset);
        WriteEntry(bw, TagSamplesPerPixel, TypeShort, 1, 1);
        WriteEntry(bw, TagRowsPerStrip, TypeLong, 1, 1);
        WriteEntry(bw, TagStripByteCounts, TypeLong, 1, 1);
        bw.Write((uint)nextIfdOffset);
    }

    private static void WriteEntry(BinaryWriter bw, ushort tag, ushort type, uint count, uint value) {
        bw.Write(tag);
        bw.Write(type);
        bw.Write(count);
        bw.Write(value);
    }

    private const ushort TagImageWidth = 256;
    private const ushort TagImageLength = 257;
    private const ushort TagBitsPerSample = 258;
    private const ushort TagCompression = 259;
    private const ushort TagPhotometric = 262;
    private const ushort TagStripOffsets = 273;
    private const ushort TagSamplesPerPixel = 277;
    private const ushort TagRowsPerStrip = 278;
    private const ushort TagStripByteCounts = 279;
    private const ushort TagTileWidth = 322;
    private const ushort TagTileLength = 323;
    private const ushort TagTileOffsets = 324;
    private const ushort TagTileByteCounts = 325;

    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;
}
