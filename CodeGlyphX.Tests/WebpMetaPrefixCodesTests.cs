using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpMetaPrefixCodesTests {
    [Fact]
    public void Webp_ManagedVp8L_MetaPrefixCodes_SelectsGroups() {
        var payload = BuildMetaPrefixedVp8lPayload();
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(8, width);
        Assert.Equal(1, height);

        // First 4 pixels should be black, next 4 should be white.
        AssertPixel(rgba, width, x: 0, r: 0, g: 0, b: 0, a: 255);
        AssertPixel(rgba, width, x: 3, r: 0, g: 0, b: 0, a: 255);
        AssertPixel(rgba, width, x: 4, r: 255, g: 255, b: 255, a: 255);
        AssertPixel(rgba, width, x: 7, r: 255, g: 255, b: 255, a: 255);
    }

    private static byte[] BuildMetaPrefixedVp8lPayload() {
        var writer = new BitWriterLsb();

        // Main VP8L header (8x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(8 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1); // alpha hint
        writer.WriteBits(0, 3); // version
        writer.WriteBits(0, 1); // no transforms

        writer.WriteBits(0, 1); // no color cache
        writer.WriteBits(1, 1); // meta prefix codes enabled
        writer.WriteBits(0, 3); // prefix bits code => prefixBits = 2

        // Meta image (entropy image) is 2x1 when prefixBits=2 and width=8.
        WriteMetaImage(writer);

        // Main prefix groups (2 groups: black and white).
        WriteBlackGroup(writer);
        WriteWhiteGroup(writer);

        // No additional pixel bits are needed because groups use single-symbol codes.
        return writer.ToArray();
    }

    private static void WriteMetaImage(BitWriterLsb writer) {
        // Meta image header (2x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(2 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1); // alpha hint
        writer.WriteBits(0, 3); // version
        writer.WriteBits(0, 1); // no transforms

        writer.WriteBits(0, 1); // no color cache
        writer.WriteBits(0, 1); // no meta prefix codes

        // Meta green uses two symbols (0 and 1) so we can choose group per block.
        WriteSimpleTwoSymbolCode(writer, symbol0: 0, symbol1: 1); // green
        WriteSimpleSingleSymbolCode(writer, 0);   // red
        WriteSimpleSingleSymbolCode(writer, 0);   // blue
        WriteSimpleSingleSymbolCode(writer, 255); // alpha
        WriteSimpleSingleSymbolCode(writer, 0);   // distance

        // Meta pixels: first block uses group 0 (bit 0), second uses group 1 (bit 1).
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
    }

    private static void WriteBlackGroup(BitWriterLsb writer) {
        WriteSimpleSingleSymbolCode(writer, 0);   // green
        WriteSimpleSingleSymbolCode(writer, 0);   // red
        WriteSimpleSingleSymbolCode(writer, 0);   // blue
        WriteSimpleSingleSymbolCode(writer, 255); // alpha
        WriteSimpleSingleSymbolCode(writer, 0);   // distance
    }

    private static void WriteWhiteGroup(BitWriterLsb writer) {
        WriteSimpleSingleSymbolCode(writer, 255); // green
        WriteSimpleSingleSymbolCode(writer, 255); // red
        WriteSimpleSingleSymbolCode(writer, 255); // blue
        WriteSimpleSingleSymbolCode(writer, 255); // alpha
        WriteSimpleSingleSymbolCode(writer, 0);   // distance
    }

    private static void WriteSimpleSingleSymbolCode(BitWriterLsb writer, int symbol) {
        writer.WriteBits(1, 1);      // simple code
        writer.WriteBits(0, 1);      // one symbol
        writer.WriteBits(1, 1);      // first symbol uses 8 bits
        writer.WriteBits(symbol, 8); // symbol0
    }

    private static void WriteSimpleTwoSymbolCode(BitWriterLsb writer, int symbol0, int symbol1) {
        writer.WriteBits(1, 1);       // simple code
        writer.WriteBits(1, 1);       // two symbols
        writer.WriteBits(1, 1);       // first symbol uses 8 bits
        writer.WriteBits(symbol0, 8); // symbol0
        writer.WriteBits(symbol1, 8); // symbol1
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

    private static void AssertPixel(byte[] rgba, int width, int x, byte r, byte g, byte b, byte a) {
        var offset = x * 4;
        Assert.Equal(r, rgba[offset]);
        Assert.Equal(g, rgba[offset + 1]);
        Assert.Equal(b, rgba[offset + 2]);
        Assert.Equal(a, rgba[offset + 3]);
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

