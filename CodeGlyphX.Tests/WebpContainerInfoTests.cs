using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpContainerInfoTests {
    [Fact]
    public void Webp_Vp8X_TryReadInfo_ParsesDimensions() {
        var webp = BuildWebp("VP8X", BuildVp8XChunkData(width: 3, height: 5));

        Assert.True(ImageReader.TryDetectFormat(webp, out var format));
        Assert.Equal(ImageFormat.Webp, format);

        Assert.True(ImageReader.TryReadInfo(webp, out var info));
        Assert.Equal(ImageFormat.Webp, info.Format);
        Assert.Equal(3, info.Width);
        Assert.Equal(5, info.Height);
    }

    [Fact]
    public void Webp_Vp8L_TryReadInfo_ParsesDimensions() {
        var webp = BuildWebp("VP8L", BuildVp8LChunkData(width: 7, height: 2));

        Assert.True(ImageReader.TryDetectFormat(webp, out var format));
        Assert.Equal(ImageFormat.Webp, format);

        Assert.True(ImageReader.TryReadInfo(webp, out var info));
        Assert.Equal(ImageFormat.Webp, info.Format);
        Assert.Equal(7, info.Width);
        Assert.Equal(2, info.Height);
    }

    private static byte[] BuildWebp(string chunkFourCc, byte[] chunkData) {
        if (chunkFourCc.Length != 4) throw new ArgumentOutOfRangeException(nameof(chunkFourCc));

        using var ms = new MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0); // placeholder
        WriteAscii(ms, "WEBP");
        WriteAscii(ms, chunkFourCc);
        WriteU32LE(ms, (uint)chunkData.Length);
        ms.Write(chunkData, 0, chunkData.Length);
        if ((chunkData.Length & 1) != 0) ms.WriteByte(0);

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static byte[] BuildVp8XChunkData(int width, int height) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var data = new byte[10];
        var widthMinus1 = width - 1;
        var heightMinus1 = height - 1;

        WriteU24LE(data, 4, widthMinus1);
        WriteU24LE(data, 7, heightMinus1);
        return data;
    }

    private static byte[] BuildVp8LChunkData(int width, int height) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var data = new byte[5];
        data[0] = 0x2F;

        var widthMinus1 = width - 1;
        var heightMinus1 = height - 1;
        var bits = (uint)((widthMinus1 & 0x3FFF) | ((heightMinus1 & 0x3FFF) << 14));
        WriteU32LE(data, 1, bits);
        return data;
    }

    private static void WriteAscii(Stream stream, string text) {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteU32LE(Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU32LE(byte[] buffer, int offset, uint value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteU24LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
    }
}

