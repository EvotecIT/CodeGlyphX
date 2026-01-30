using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffMultiPageEncodeTests {
    [Fact]
    public void Tiff_ManagedEncode_MultiPage_RoundTrips() {
        const int width1 = 2;
        const int height1 = 2;
        const int stride1 = width1 * 4;
        var page1 = new byte[] {
            0, 0, 0, 255,       255, 0, 0, 255,
            0, 255, 0, 255,     0, 0, 255, 255
        };

        const int width2 = 3;
        const int height2 = 1;
        const int stride2 = width2 * 4;
        var page2 = new byte[] {
            10, 20, 30, 0,
            40, 50, 60, 128,
            70, 80, 90, 255
        };

        var pages = new[] {
            new TiffRgba32Page(page1, width1, height1, stride1, TiffCompressionMode.None),
            new TiffRgba32Page(page2, width2, height2, stride2, TiffCompressionMode.Deflate)
        };

        var tiff = TiffWriter.WriteRgba32(pages);

        Assert.True(ImageReader.TryReadPageCount(tiff, out var pageCount));
        Assert.Equal(2, pageCount);

        var decoded1 = ImageReader.DecodeRgba32(tiff, 0, out var decodedWidth1, out var decodedHeight1);
        Assert.Equal(width1, decodedWidth1);
        Assert.Equal(height1, decodedHeight1);
        Assert.Equal(page1, decoded1);

        var decoded2 = ImageReader.DecodeRgba32(tiff, 1, out var decodedWidth2, out var decodedHeight2);
        Assert.Equal(width2, decodedWidth2);
        Assert.Equal(height2, decodedHeight2);
        Assert.Equal(page2, decoded2);
    }

    [Fact]
    public void Tiff_ManagedEncode_MultiPage_MultiStrip_UsesRowsPerStrip() {
        const int width1 = 3;
        const int height1 = 5;
        const int stride1 = width1 * 4;
        var page1 = new byte[height1 * stride1];
        for (var y = 0; y < height1; y++) {
            for (var x = 0; x < width1; x++) {
                var i = y * stride1 + x * 4;
                page1[i] = (byte)(x * 20);
                page1[i + 1] = (byte)(y * 30);
                page1[i + 2] = 10;
                page1[i + 3] = 255;
            }
        }

        const int width2 = 2;
        const int height2 = 2;
        const int stride2 = width2 * 4;
        var page2 = new byte[] {
            0, 0, 0, 255,       255, 255, 255, 255,
            255, 0, 0, 255,     0, 255, 0, 255
        };

        var pages = new[] {
            new TiffRgba32Page(page1, width1, height1, stride1, TiffCompressionMode.None, rowsPerStrip: 2),
            new TiffRgba32Page(page2, width2, height2, stride2, TiffCompressionMode.None)
        };

        var tiff = TiffWriter.WriteRgba32(pages);

        Assert.Equal(3, ReadStripCount(tiff, pageIndex: 0));
        Assert.Equal(1, ReadStripCount(tiff, pageIndex: 1));

        var decoded1 = ImageReader.DecodeRgba32(tiff, 0, out var decodedWidth1, out var decodedHeight1);
        Assert.Equal(width1, decodedWidth1);
        Assert.Equal(height1, decodedHeight1);
        Assert.Equal(page1, decoded1);
    }

    private static int ReadStripCount(ReadOnlySpan<byte> tiff, int pageIndex) {
        Assert.True(TiffReader.IsTiff(tiff));
        if (pageIndex < 0) return 0;
        var little = tiff[0] == (byte)'I';
        var ifdOffset = ReadUInt32(tiff, 4, little);
        for (var page = 0; page <= pageIndex; page++) {
            if (ifdOffset == 0 || ifdOffset > tiff.Length - 2) return 0;
            var entryCount = ReadUInt16(tiff, (int)ifdOffset, little);
            var entriesOffset = (int)ifdOffset + 2;
            var maxEntries = Math.Min(entryCount, (ushort)((tiff.Length - entriesOffset) / 12));
            var stripCount = 0;
            for (var i = 0; i < maxEntries; i++) {
                var entryOffset = entriesOffset + i * 12;
                var tag = ReadUInt16(tiff, entryOffset, little);
                if (tag != 273) continue;
                stripCount = (int)ReadUInt32(tiff, entryOffset + 4, little);
            }
            var nextIfdOffsetPos = entriesOffset + entryCount * 12;
            var nextOffset = nextIfdOffsetPos + 4 <= tiff.Length
                ? ReadUInt32(tiff, nextIfdOffsetPos, little)
                : 0u;
            if (page == pageIndex) return stripCount;
            ifdOffset = nextOffset;
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
