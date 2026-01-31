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


    [Fact]
    public void DecodeRgba32_Rejects_OversizedJpegDimensions() {
        var jpeg = BuildJpegHeader(width: 10_000, height: 10_000);
        var options = new ImageDecodeOptions { MaxPixels = 1_000_000 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(jpeg, options, out _, out _));
    }
    private static byte[] BuildJpegHeader(int width, int height) {
        var data = new byte[21];
        data[0] = 0xFF; // SOI
        data[1] = 0xD8;
        data[2] = 0xFF; // SOF0
        data[3] = 0xC0;
        data[4] = 0x00; // length (17)
        data[5] = 0x11;
        data[6] = 0x08; // precision
        WriteUInt16BE(data, 7, (ushort)height);
        WriteUInt16BE(data, 9, (ushort)width);
        data[11] = 0x03; // components
        data[12] = 0x01; data[13] = 0x11; data[14] = 0x00;
        data[15] = 0x02; data[16] = 0x11; data[17] = 0x00;
        data[18] = 0x03; data[19] = 0x11; data[20] = 0x00;
        return data;
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

    private static void WriteUInt16BE(byte[] data, int offset, ushort value) {
        data[offset + 0] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
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
