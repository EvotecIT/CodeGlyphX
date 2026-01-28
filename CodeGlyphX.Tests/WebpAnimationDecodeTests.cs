using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpAnimationDecodeTests {
    [Fact]
    public void Webp_ManagedDecode_AnimatedWebp_DecodesFirstFrame() {
        var vp8lPayload = BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 10, g: 20, b: 30, a: 128);
        var webp = BuildAnimatedWebp(
            vp8lPayload,
            canvasWidth: 2,
            canvasHeight: 2,
            frameX: 0,
            frameY: 0,
            frameWidth: 1,
            frameHeight: 1,
            blend: true,
            bgraBackground: 0);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height));
        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(16, rgba.Length);

        Assert.Equal(new byte[] { 10, 20, 30, 128 }, rgba.AsSpan(0, 4).ToArray());
        Assert.Equal(0, rgba[4]);
        Assert.Equal(0, rgba[5]);
        Assert.Equal(0, rgba[6]);
        Assert.Equal(0, rgba[7]);
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

    private static byte[] BuildAnimatedWebp(
        byte[] vp8lPayload,
        int canvasWidth,
        int canvasHeight,
        int frameX,
        int frameY,
        int frameWidth,
        int frameHeight,
        bool blend,
        uint bgraBackground) {
        using var ms = new MemoryStream();

        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0); // placeholder
        WriteAscii(ms, "WEBP");

        var flags = (byte)0x02; // animation
        if (vp8lPayload.Length > 0) {
            flags |= 0x10; // alpha flag
        }

        using (var vp8x = new MemoryStream()) {
            vp8x.WriteByte(flags);
            vp8x.WriteByte(0);
            vp8x.WriteByte(0);
            vp8x.WriteByte(0);
            WriteU24LE(vp8x, canvasWidth - 1);
            WriteU24LE(vp8x, canvasHeight - 1);
            WriteChunk(ms, "VP8X", vp8x.ToArray());
        }

        using (var anim = new MemoryStream()) {
            WriteU32LE(anim, bgraBackground);
            WriteU16LE(anim, 0); // loop count
            WriteChunk(ms, "ANIM", anim.ToArray());
        }

        using (var frame = new MemoryStream()) {
            WriteU24LE(frame, frameX / 2);
            WriteU24LE(frame, frameY / 2);
            WriteU24LE(frame, frameWidth - 1);
            WriteU24LE(frame, frameHeight - 1);
            WriteU24LE(frame, 0); // duration
            frame.WriteByte((byte)(blend ? 0x00 : 0x02));

            using (var vp8l = new MemoryStream()) {
                vp8l.Write(vp8lPayload, 0, vp8lPayload.Length);
                WriteChunk(frame, "VP8L", vp8l.ToArray());
            }

            WriteChunk(ms, "ANMF", frame.ToArray());
        }

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static void WriteChunk(Stream stream, string fourCc, byte[] payload) {
        WriteAscii(stream, fourCc);
        WriteU32LE(stream, (uint)payload.Length);
        stream.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0) {
            stream.WriteByte(0);
        }
    }

    private static void WriteAscii(Stream stream, string text) {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteU16LE(Stream stream, ushort value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }

    private static void WriteU24LE(Stream stream, int value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
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
        private readonly System.Collections.Generic.List<byte> _bytes = new();
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
