using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpManagedDecodeTests {
    [Fact]
    public void Webp_ManagedLiteralOnly_Vp8L_DecodesPixel() {
        var payload = BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 255, g: 0, b: 0, a: 255);
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDetectFormat(webp, out var format));
        Assert.Equal(ImageFormat.Webp, format);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, rgba);
    }

    private static byte[] BuildLiteralOnlyVp8lPayload(int width, int height, int r, int g, int b, int a) {
        var writer = new BitWriterLsb();

        writer.WriteBits(0x2F, 8);
        writer.WriteBits(width - 1, 14);
        writer.WriteBits(height - 1, 14);
        writer.WriteBits(a != 255 ? 1 : 0, 1);
        writer.WriteBits(0, 3); // version

        writer.WriteBits(0, 1); // no transforms
        writer.WriteBits(0, 1); // no color cache
        writer.WriteBits(0, 1); // no meta prefix codes

        // Prefix codes group (simple single-symbol codes).
        WriteSimplePrefixCode(writer, g);   // green / length / cache
        WriteSimplePrefixCode(writer, r);   // red
        WriteSimplePrefixCode(writer, b);   // blue
        WriteSimplePrefixCode(writer, a);   // alpha
        WriteSimplePrefixCode(writer, 0);   // distance (unused)

        return writer.ToArray();
    }

    private static void WriteSimplePrefixCode(BitWriterLsb writer, int symbol) {
        writer.WriteBits(1, 1);      // simple code
        writer.WriteBits(0, 1);      // one symbol
        writer.WriteBits(1, 1);      // first symbol uses 8 bits
        writer.WriteBits(symbol, 8); // symbol0
    }

    private static byte[] BuildWebpVp8l(byte[] payload) {
        using var ms = new MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0); // placeholder
        WriteAscii(ms, "WEBP");
        WriteAscii(ms, "VP8L");
        WriteU32LE(ms, (uint)payload.Length);
        ms.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0) ms.WriteByte(0);

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
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

    private sealed class BitWriterLsb {
        private readonly List<byte> _bytes = new();
        private int _bitPos;

        public void WriteBits(int value, int count) {
            for (var i = 0; i < count; i++) {
                var bit = (value >> i) & 1;
                var byteIndex = _bitPos >> 3;
                var bitIndex = _bitPos & 7;
                if (byteIndex >= _bytes.Count) _bytes.Add(0);
                if (bit != 0) _bytes[byteIndex] = (byte)(_bytes[byteIndex] | (1 << bitIndex));
                _bitPos++;
            }
        }

        public byte[] ToArray() => _bytes.ToArray();
    }
}

