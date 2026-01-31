using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpColorTransformTests {
    [Fact]
    public void Webp_ManagedVp8L_ColorTransform_AdjustsChannels() {
        var payload = BuildColorTransformVp8lPayload();
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);

        AssertPixel(rgba, x: 0, r: 15, g: 10, b: 0, a: 255);
    }

    private static byte[] BuildColorTransformVp8lPayload() {
        var writer = new BitWriterLsb();

        // Main header (1x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        // Transforms: color transform only.
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 2); // color transform
        writer.WriteBits(0, 3); // sizeBits code => sizeBits=2 (block size 4)
        WriteColorTransformSubImage(writer); // 1x1, g2r=32
        writer.WriteBits(0, 1); // no more transforms

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Main prefix codes: single literals for the only pixel.
        WriteSimpleSingleSymbolCode(writer, 10);    // green
        WriteSimpleSingleSymbolCode(writer, 5);     // red
        WriteSimpleSingleSymbolCode(writer, 0);     // blue
        WriteSimpleSingleSymbolCode(writer, 255);   // alpha
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        // No residual bits needed for single-symbol codes.
        return writer.ToArray();
    }

    private static void WriteColorTransformSubImage(BitWriterLsb writer) {
        // Subimage data (1x1), no header and no transforms.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Transform element channels are interpreted as:
        // blue=g2r, green=g2b, red=r2b.
        WriteSimpleSingleSymbolCode(writer, 0);     // green (g2b)
        WriteSimpleSingleSymbolCode(writer, 0);     // red (r2b)
        WriteSimpleSingleSymbolCode(writer, 32);    // blue (g2r => add green to red)
        WriteSimpleSingleSymbolCode(writer, 255);   // alpha
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused
    }

    private static void WriteSimpleSingleSymbolCode(BitWriterLsb writer, int symbol) {
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(symbol, 8);
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

public sealed class WebpColorIndexingTransformTests {
    [Fact]
    public void Webp_ManagedVp8L_ColorIndexing_ExpandsPackedPixels() {
        var payload = BuildColorIndexingVp8lPayload();
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(4, width);
        Assert.Equal(1, height);

        AssertPixel(rgba, x: 0, r: 0, g: 0, b: 0, a: 255);
        AssertPixel(rgba, x: 1, r: 1, g: 1, b: 1, a: 255);
        AssertPixel(rgba, x: 2, r: 2, g: 2, b: 2, a: 255);
        AssertPixel(rgba, x: 3, r: 3, g: 3, b: 3, a: 255);
    }

    private static byte[] BuildColorIndexingVp8lPayload() {
        var writer = new BitWriterLsb();

        // Main header (4x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(4 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        // Transforms: color indexing only with a 4-entry palette.
        writer.WriteBits(1, 1);
        writer.WriteBits(3, 2); // color indexing transform
        writer.WriteBits(4 - 1, 8); // color table size minus 1
        WriteColorIndexingPaletteSubImage(writer);
        writer.WriteBits(0, 1); // no more transforms

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Encoded width is 1 when widthBits=2 (4 pixels per entry).
        WriteSimpleSingleSymbolCode(writer, 228);   // packed indices: 0,1,2,3
        WriteSimpleSingleSymbolCode(writer, 0);     // red
        WriteSimpleSingleSymbolCode(writer, 0);     // blue
        WriteSimpleSingleSymbolCode(writer, 255);   // alpha
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        return writer.ToArray();
    }

    private static void WriteColorIndexingPaletteSubImage(BitWriterLsb writer) {
        // Palette subimage data (4x1), no header and no transforms.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Delta-coded palette: entries become (0..3) for RGB with alpha 255.
        WriteSimpleTwoSymbolCode(writer, 0, 1);     // green deltas
        WriteSimpleTwoSymbolCode(writer, 0, 1);     // red deltas
        WriteSimpleTwoSymbolCode(writer, 0, 1);     // blue deltas
        WriteSimpleTwoSymbolCode(writer, 0, 255);   // alpha deltas
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        // Pixel 0 deltas: g=0, r=0, b=0, a=255.
        writer.WriteBits(0, 1); // green 0
        writer.WriteBits(0, 1); // red 0
        writer.WriteBits(0, 1); // blue 0
        writer.WriteBits(1, 1); // alpha 255

        // Pixel 1 deltas: g=1, r=1, b=1, a=0.
        writer.WriteBits(1, 1); // green 1
        writer.WriteBits(1, 1); // red 1
        writer.WriteBits(1, 1); // blue 1
        writer.WriteBits(0, 1); // alpha 0

        // Pixel 2 deltas: g=1, r=1, b=1, a=0.
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);

        // Pixel 3 deltas: g=1, r=1, b=1, a=0.
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
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
