using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpPredictorTransformTests {
    [Fact]
    public void Webp_ManagedVp8L_PredictorTransform_ReconstructsPixels() {
        var payload = BuildPredictorVp8lPayload();
        var webp = BuildWebpVp8l(payload);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(2, width);
        Assert.Equal(1, height);

        AssertPixel(rgba, width, x: 0, r: 0, g: 0, b: 0, a: 255);
        AssertPixel(rgba, width, x: 1, r: 255, g: 255, b: 255, a: 255);
    }

    private static byte[] BuildPredictorVp8lPayload() {
        var writer = new BitWriterLsb();

        // Main header (2x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(2 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1); // alpha hint
        writer.WriteBits(0, 3); // version

        // Transforms: predictor only.
        writer.WriteBits(1, 1); // has transform
        writer.WriteBits(0, 2); // predictor transform
        writer.WriteBits(0, 3); // sizeBits code => sizeBits=2 (block size 4)
        WritePredictorSubImage(writer); // 1x1, mode=1 (predict left)
        writer.WriteBits(0, 1); // no more transforms

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Main prefix codes: allow 0 and 255.
        WriteSimpleTwoSymbolCode(writer, 0, 255);   // green
        WriteSimpleTwoSymbolCode(writer, 0, 255);   // red
        WriteSimpleTwoSymbolCode(writer, 0, 255);   // blue
        WriteSimpleSingleSymbolCode(writer, 0);     // alpha residual always 0
        WriteSimpleSingleSymbolCode(writer, 0);     // distance unused

        // Pixel 0 residuals: all zeros => black.
        writer.WriteBits(0, 1); // green 0
        writer.WriteBits(0, 1); // red 0
        writer.WriteBits(0, 1); // blue 0

        // Pixel 1 residuals: all 255 => white (with predictor-left).
        writer.WriteBits(1, 1); // green 255
        writer.WriteBits(1, 1); // red 255
        writer.WriteBits(1, 1); // blue 255

        return writer.ToArray();
    }

    private static void WritePredictorSubImage(BitWriterLsb writer) {
        // Predictor subimage header (1x1).
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(1 - 1, 14);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 3);

        // No color cache, no meta.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        // Single-symbol codes: green=1 (mode 1), others 0/255.
        WriteSimpleSingleSymbolCode(writer, 1);     // green (predict left)
        WriteSimpleSingleSymbolCode(writer, 0);     // red
        WriteSimpleSingleSymbolCode(writer, 0);     // blue
        WriteSimpleSingleSymbolCode(writer, 255);   // alpha
        WriteSimpleSingleSymbolCode(writer, 0);     // distance
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

