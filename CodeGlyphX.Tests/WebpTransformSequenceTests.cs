using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpTransformSequenceTests {
    [Fact]
    public void Webp_ManagedVp8L_SubtractGreen_AppliesAfterColorIndexingExpansion() {
        var payload = BuildSubtractGreenPlusIndexingPayload();
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(4, width);
        Assert.Equal(1, height);

        AssertPixel(rgba, x: 0, r: 0, g: 0, b: 0, a: 255);
        AssertPixel(rgba, x: 1, r: 1, g: 1, b: 1, a: 255);
        AssertPixel(rgba, x: 2, r: 2, g: 2, b: 2, a: 255);
        AssertPixel(rgba, x: 3, r: 3, g: 3, b: 3, a: 255);
    }

    private static byte[] BuildSubtractGreenPlusIndexingPayload() {
        var writer = new BitWriterLsb();

        // Main header (4x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(4 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        // Transforms: subtract green, then color indexing.
        writer.WriteBits(1, 1);
        writer.WriteBits(2, 2); // subtract green

        writer.WriteBits(1, 1);
        writer.WriteBits(3, 2); // color indexing
        writer.WriteBits(4 - 1, 8); // color table size minus 1
        WritePaletteSubImage(writer);

        writer.WriteBits(0, 1); // no more transforms

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Encoded width is 1 when widthBits=2 (4 pixels packed per entry).
        WriteSimpleSingleSymbolCode(writer, 228);   // packed indices: 0,1,2,3
        WriteSimpleSingleSymbolCode(writer, 0);     // red residual
        WriteSimpleSingleSymbolCode(writer, 0);     // blue residual
        WriteSimpleSingleSymbolCode(writer, 255);   // alpha
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        return writer.ToArray();
    }

    private static void WritePaletteSubImage(BitWriterLsb writer) {
        // Palette subimage header (4x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(4 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Delta-coded palette entries. Only green increases (0..3).
        WriteSimpleTwoSymbolCode(writer, 0, 1);     // green deltas
        WriteSimpleSingleSymbolCode(writer, 0);     // red deltas
        WriteSimpleSingleSymbolCode(writer, 0);     // blue deltas
        WriteSimpleTwoSymbolCode(writer, 0, 255);   // alpha deltas
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        // Entry 0: g=0, a=255.
        writer.WriteBits(0, 1); // green 0
        writer.WriteBits(1, 1); // alpha 255

        // Entry 1: g=1, a=0.
        writer.WriteBits(1, 1); // green 1
        writer.WriteBits(0, 1); // alpha 0

        // Entry 2: g=1, a=0.
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);

        // Entry 3: g=1, a=0.
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
    }

    private static void WriteSimpleSingleSymbolCode(BitWriterLsb writer, int symbol) {
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(symbol, 8);
    }

    private static void WriteSimpleTwoSymbolCode(BitWriterLsb writer, int symbol0, int symbol1) {
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(symbol0, 8);
        writer.WriteBits(symbol1, 8);
    }

    private static byte[] BuildWebpVp8l(byte[] payload) {
        using var ms = new MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0);
        WriteAscii(ms, "WEBP");
        WriteAscii(ms, "VP8L");
        WriteU32LE(ms, (uint)payload.Length);
        ms.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0) ms.WriteByte(0);

        var bytes = ms.ToArray();
        var riffSize = (uint)(bytes.Length - 8);
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static void AssertPixel(byte[] rgba, int x, byte r, byte g, byte b, byte a) {
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

