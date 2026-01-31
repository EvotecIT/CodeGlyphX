using System;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageReaderHostileTests {
    [Fact]
    public void DecodeRgba32_Rejects_OversizedPngDimensions() {
        var png = BuildPngHeader(width: 100_000, height: 100_000);
        var options = new ImageDecodeOptions { MaxPixels = 10_000_000 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(png, options, out _, out _));
    }

    [Fact]
    public void TryDecodeRgba32_Rejects_OversizedGifDimensions() {
        var gif = BuildGifHeader(width: 60_000, height: 60_000);
        var options = new ImageDecodeOptions { MaxPixels = 1_000_000 };

        Assert.False(ImageReader.TryDecodeRgba32(gif, options, out _, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_InputAboveMaxBytes() {
        var data = new byte[32];
        var options = new ImageDecodeOptions { MaxBytes = 8 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, options, out _, out _));
    }

    private static byte[] BuildPngHeader(int width, int height) {
        var data = new byte[24];
        data[0] = 137;
        data[1] = 80;
        data[2] = 78;
        data[3] = 71;
        data[4] = 13;
        data[5] = 10;
        data[6] = 26;
        data[7] = 10;
        data[12] = (byte)'I';
        data[13] = (byte)'H';
        data[14] = (byte)'D';
        data[15] = (byte)'R';
        WriteUInt32BE(data, 16, (uint)width);
        WriteUInt32BE(data, 20, (uint)height);
        return data;
    }

    private static byte[] BuildGifHeader(int width, int height) {
        var data = new byte[10];
        data[0] = (byte)'G';
        data[1] = (byte)'I';
        data[2] = (byte)'F';
        data[3] = (byte)'8';
        data[4] = (byte)'9';
        data[5] = (byte)'a';
        WriteUInt16LE(data, 6, (ushort)width);
        WriteUInt16LE(data, 8, (ushort)height);
        return data;
    }

    private static void WriteUInt32BE(byte[] data, int offset, uint value) {
        data[offset + 0] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteUInt16LE(byte[] data, int offset, ushort value) {
        data[offset + 0] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)(value >> 8);
    }
}
