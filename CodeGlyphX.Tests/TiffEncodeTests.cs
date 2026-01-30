using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffEncodeTests {
    [Fact]
    public void Tiff_Encode_Respects_Compression_Mode() {
        var payload = "https://example.com/tiff";
        var none = QrCode.Render(payload, OutputFormat.Tiff, extras: new RenderExtras {
            TiffCompression = TiffCompressionMode.None
        }).Data;
        var packBits = QrCode.Render(payload, OutputFormat.Tiff, extras: new RenderExtras {
            TiffCompression = TiffCompressionMode.PackBits
        }).Data;
        var auto = QrCode.Render(payload, OutputFormat.Tiff, extras: new RenderExtras {
            TiffCompression = TiffCompressionMode.Auto
        }).Data;

        Assert.Equal(1, ReadCompression(none));
        Assert.Equal(32773, ReadCompression(packBits));
        var autoCompression = ReadCompression(auto);
        Assert.True(autoCompression == 1 || autoCompression == 32773);
    }

    private static ushort ReadCompression(byte[] tiff) {
        Assert.NotNull(tiff);
        Assert.True(tiff.Length >= 16);
        Assert.Equal((byte)'I', tiff[0]);
        Assert.Equal((byte)'I', tiff[1]);
        Assert.Equal(42, tiff[2]);
        Assert.Equal(0, tiff[3]);

        var ifdOffset = ReadU32(tiff, 4);
        Assert.True(ifdOffset + 2 <= tiff.Length);
        var count = ReadU16(tiff, (int)ifdOffset);
        var entriesOffset = (int)ifdOffset + 2;
        for (var i = 0; i < count; i++) {
            var entryOffset = entriesOffset + i * 12;
            if (entryOffset + 12 > tiff.Length) break;
            var tag = ReadU16(tiff, entryOffset);
            if (tag != 259) continue;
            var type = ReadU16(tiff, entryOffset + 2);
            var entryCount = ReadU32(tiff, entryOffset + 4);
            Assert.Equal(3, type);
            Assert.Equal(1u, entryCount);
            return ReadU16(tiff, entryOffset + 8);
        }

        Assert.True(false, "Compression tag not found.");
        return 0;
    }

    private static ushort ReadU16(byte[] data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadU32(byte[] data, int offset) {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}
