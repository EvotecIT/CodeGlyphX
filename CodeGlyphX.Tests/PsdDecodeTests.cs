using System;
using System.Collections.Generic;
using System.IO;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PsdDecodeTests {
    [Fact]
    public void Psd_Rgb8_Raw_WithAlpha() {
        var psd = BuildPsdRaw(1, 1, depth: 8, colorMode: 3,
            new byte[] { 10 },
            new byte[] { 20 },
            new byte[] { 30 },
            new byte[] { 40 });

        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 10, 20, 30, 40 }, rgba);
    }

    [Fact]
    public void Psd_Grayscale8_Raw() {
        var psd = BuildPsdRaw(1, 1, depth: 8, colorMode: 1,
            new byte[] { 200 });

        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 200, 200, 200, 255 }, rgba);
    }

    [Fact]
    public void Psd_Cmyk8_Raw_WithAlpha() {
        var psd = BuildPsdRaw(1, 1, depth: 8, colorMode: 4,
            new byte[] { 0 },
            new byte[] { 255 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 128 });

        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 0, 255, 128 }, rgba);
    }

    [Fact]
    public void Psd_Grayscale16_Raw() {
        var psd = BuildPsdRaw(1, 1, depth: 16, colorMode: 1,
            BuildChannel16(0x1234));

        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);

        var expected = Convert16To8(0x1234);
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { expected, expected, expected, 255 }, rgba);
    }

    [Fact]
    public void Psd_Grayscale8_Rle() {
        var psd = BuildPsdRle(2, 1, depth: 8, colorMode: 1,
            new byte[] { 0x10, 0x20 });

        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] {
            0x10, 0x10, 0x10, 255,
            0x20, 0x20, 0x20, 255
        }, rgba);
    }

    private static byte[] BuildPsdRaw(int width, int height, ushort depth, ushort colorMode, params byte[][] channels) {
        return BuildPsd(width, height, depth, colorMode, channels, rle: false);
    }

    private static byte[] BuildPsdRle(int width, int height, ushort depth, ushort colorMode, params byte[][] channels) {
        return BuildPsd(width, height, depth, colorMode, channels, rle: true);
    }

    private static byte[] BuildPsd(int width, int height, ushort depth, ushort colorMode, byte[][] channels, bool rle) {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (depth != 8 && depth != 16) throw new ArgumentOutOfRangeException(nameof(depth));
        if (channels is null || channels.Length == 0) throw new ArgumentException("Channels required.", nameof(channels));

        var bytesPerSample = depth == 16 ? 2 : 1;
        var pixelCount = checked(width * height);
        var channelBytes = checked(pixelCount * bytesPerSample);
        for (var i = 0; i < channels.Length; i++) {
            if (channels[i].Length != channelBytes) throw new ArgumentException("Channel size mismatch.", nameof(channels));
        }

        using var ms = new MemoryStream();
        WriteAscii(ms, "8BPS");
        WriteU16(ms, 1);
        WriteZeros(ms, 6);
        WriteU16(ms, (ushort)channels.Length);
        WriteU32(ms, (uint)height);
        WriteU32(ms, (uint)width);
        WriteU16(ms, depth);
        WriteU16(ms, colorMode);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU16(ms, (ushort)(rle ? 1 : 0));

        if (!rle) {
            for (var c = 0; c < channels.Length; c++) {
                ms.Write(channels[c], 0, channels[c].Length);
            }
            return ms.ToArray();
        }

        var rowBytes = checked(width * bytesPerSample);
        var encodedRows = new List<byte[]>(channels.Length * height);
        for (var c = 0; c < channels.Length; c++) {
            var channel = channels[c];
            for (var y = 0; y < height; y++) {
                var row = new ReadOnlySpan<byte>(channel, y * rowBytes, rowBytes);
                var encoded = PackBitsEncode(row);
                encodedRows.Add(encoded);
            }
        }

        foreach (var encoded in encodedRows) {
            if (encoded.Length > ushort.MaxValue) throw new ArgumentException("Encoded row too large.");
            WriteU16(ms, (ushort)encoded.Length);
        }

        foreach (var encoded in encodedRows) {
            ms.Write(encoded, 0, encoded.Length);
        }

        return ms.ToArray();
    }

    private static byte[] BuildChannel16(params ushort[] samples) {
        var data = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++) {
            var value = samples[i];
            data[i * 2] = (byte)(value >> 8);
            data[i * 2 + 1] = (byte)value;
        }
        return data;
    }

    private static byte Convert16To8(ushort value) {
        return (byte)((value + 128) / 257);
    }

    private static byte[] PackBitsEncode(ReadOnlySpan<byte> data) {
        var output = new List<byte>(data.Length + 8);
        var i = 0;
        while (i < data.Length) {
            var runStart = i;
            var runValue = data[i];
            var runLength = 1;
            while (runStart + runLength < data.Length && runLength < 128 && data[runStart + runLength] == runValue) {
                runLength++;
            }

            if (runLength >= 3) {
                output.Add((byte)(257 - runLength));
                output.Add(runValue);
                i += runLength;
                continue;
            }

            var literalStart = i;
            var literalLength = 0;
            while (i < data.Length && literalLength < 128) {
                if (i + 2 < data.Length && data[i] == data[i + 1] && data[i] == data[i + 2]) {
                    break;
                }
                i++;
                literalLength++;
            }

            output.Add((byte)(literalLength - 1));
            for (var l = 0; l < literalLength; l++) {
                output.Add(data[literalStart + l]);
            }
        }
        return output.ToArray();
    }

    private static void WriteAscii(Stream stream, string value) {
        for (var i = 0; i < value.Length; i++) {
            stream.WriteByte((byte)value[i]);
        }
    }

    private static void WriteZeros(Stream stream, int count) {
        for (var i = 0; i < count; i++) {
            stream.WriteByte(0);
        }
    }

    private static void WriteU16(Stream stream, ushort value) {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    private static void WriteU32(Stream stream, uint value) {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }
}
