using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
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

    [Fact]
    public void Webp_ManagedDecode_AnimatedWebp_DecodesFrames() {
        var frame1 = new AnimationFrameSpec(
            BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 10, g: 20, b: 30, a: 128),
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 120,
            blend: true,
            disposeToBackground: false);
        var frame2 = new AnimationFrameSpec(
            BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 5, g: 6, b: 7, a: 255),
            x: 2,
            y: 2,
            width: 1,
            height: 1,
            durationMs: 80,
            blend: false,
            disposeToBackground: true);

        var webp = BuildAnimatedWebp(
            new[] { frame1, frame2 },
            canvasWidth: 3,
            canvasHeight: 3,
            bgraBackground: 0x11223344,
            loopCount: 3);

        Assert.True(WebpReader.TryDecodeAnimationFrames(webp, out var frames, out var canvasWidth, out var canvasHeight, out var options));
        Assert.Equal(3, canvasWidth);
        Assert.Equal(3, canvasHeight);
        Assert.Equal(3, options.LoopCount);
        Assert.Equal(0x11223344u, options.BackgroundBgra);
        Assert.Equal(2, frames.Length);

        Assert.Equal(1, frames[0].Width);
        Assert.Equal(1, frames[0].Height);
        Assert.Equal(4, frames[0].Stride);
        Assert.Equal(120, frames[0].DurationMs);
        Assert.True(frames[0].Blend);
        Assert.False(frames[0].DisposeToBackground);
        Assert.Equal(0, frames[0].X);
        Assert.Equal(0, frames[0].Y);
        Assert.Equal(new byte[] { 10, 20, 30, 128 }, frames[0].Rgba.AsSpan(0, 4).ToArray());

        Assert.Equal(1, frames[1].Width);
        Assert.Equal(1, frames[1].Height);
        Assert.Equal(4, frames[1].Stride);
        Assert.Equal(80, frames[1].DurationMs);
        Assert.False(frames[1].Blend);
        Assert.True(frames[1].DisposeToBackground);
        Assert.Equal(2, frames[1].X);
        Assert.Equal(2, frames[1].Y);
        Assert.Equal(new byte[] { 5, 6, 7, 255 }, frames[1].Rgba.AsSpan(0, 4).ToArray());
    }

    [Fact]
    public void Webp_ManagedDecode_AnimatedWebp_DecodesCanvasFrames() {
        var frame1 = new AnimationFrameSpec(
            BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 10, g: 0, b: 0, a: 128),
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 60,
            blend: false,
            disposeToBackground: false);
        var frame2 = new AnimationFrameSpec(
            BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 0, g: 20, b: 0, a: 128),
            x: 2,
            y: 0,
            width: 1,
            height: 1,
            durationMs: 60,
            blend: true,
            disposeToBackground: true);
        var frame3 = new AnimationFrameSpec(
            BuildLiteralOnlyVp8lPayload(width: 1, height: 1, r: 0, g: 0, b: 30, a: 128),
            x: 0,
            y: 2,
            width: 1,
            height: 1,
            durationMs: 60,
            blend: false,
            disposeToBackground: false);

        var webp = BuildAnimatedWebp(
            new[] { frame1, frame2, frame3 },
            canvasWidth: 4,
            canvasHeight: 4,
            bgraBackground: 0xFF070809,
            loopCount: 0);

        Assert.True(WebpReader.TryDecodeAnimationFrames(webp, out var rawFrames, out _, out _, out _));
        AssertPixel(rawFrames[0].Rgba, rawFrames[0].Width, 0, 0, 10, 0, 0, 128);

        Assert.True(WebpReader.TryDecodeAnimationCanvasFrames(webp, out var frames, out var canvasWidth, out var canvasHeight, out _));
        Assert.Equal(4, canvasWidth);
        Assert.Equal(4, canvasHeight);
        Assert.Equal(3, frames.Length);

        Assert.Equal(canvasWidth, frames[0].Width);
        Assert.Equal(canvasHeight, frames[0].Height);

        AssertPixel(frames[0].Rgba, canvasWidth, 0, 0, 10, 0, 0, 128);
        AssertPixel(frames[0].Rgba, canvasWidth, 2, 0, 7, 8, 9, 255);
        AssertPixel(frames[0].Rgba, canvasWidth, 0, 2, 7, 8, 9, 255);
        AssertPixel(frames[0].Rgba, canvasWidth, 2, 2, 7, 8, 9, 255);

        AssertPixel(frames[1].Rgba, canvasWidth, 0, 0, 10, 0, 0, 128);
        AssertPixel(frames[1].Rgba, canvasWidth, 2, 0, 3, 14, 4, 255);
        AssertPixel(frames[1].Rgba, canvasWidth, 0, 2, 7, 8, 9, 255);
        AssertPixel(frames[1].Rgba, canvasWidth, 2, 2, 7, 8, 9, 255);

        AssertPixel(frames[2].Rgba, canvasWidth, 0, 0, 10, 0, 0, 128);
        AssertPixel(frames[2].Rgba, canvasWidth, 2, 0, 7, 8, 9, 255);
        AssertPixel(frames[2].Rgba, canvasWidth, 0, 2, 0, 0, 30, 128);
        AssertPixel(frames[2].Rgba, canvasWidth, 2, 2, 7, 8, 9, 255);
    }

    internal static byte[] BuildLiteralOnlyVp8lPayload(int width, int height, int r, int g, int b, int a) {
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

    private static void AssertPixel(byte[] rgba, int width, int x, int y, byte r, byte g, byte b, byte a) {
        var index = (y * width + x) * 4;
        Assert.Equal(r, rgba[index]);
        Assert.Equal(g, rgba[index + 1]);
        Assert.Equal(b, rgba[index + 2]);
        Assert.Equal(a, rgba[index + 3]);
    }

    internal static byte[] BuildAnimatedWebp(
        byte[] vp8lPayload,
        int canvasWidth,
        int canvasHeight,
        int frameX,
        int frameY,
        int frameWidth,
        int frameHeight,
        bool blend,
        uint bgraBackground) {
        var frame = new AnimationFrameSpec(
            vp8lPayload,
            frameX,
            frameY,
            frameWidth,
            frameHeight,
            durationMs: 0,
            blend,
            disposeToBackground: false);
        return BuildAnimatedWebp(new[] { frame }, canvasWidth, canvasHeight, bgraBackground, loopCount: 0);
    }

    internal static byte[] BuildAnimatedWebp(
        AnimationFrameSpec[] frames,
        int canvasWidth,
        int canvasHeight,
        uint bgraBackground,
        int loopCount) {
        using var ms = new MemoryStream();

        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0); // placeholder
        WriteAscii(ms, "WEBP");

        var flags = (byte)0x02; // animation
        if (UsesAlpha(frames)) {
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
            WriteU16LE(anim, (ushort)loopCount); // loop count
            WriteChunk(ms, "ANIM", anim.ToArray());
        }

        foreach (var frame in frames) {
            using var framePayload = new MemoryStream();
            WriteU24LE(framePayload, frame.X / 2);
            WriteU24LE(framePayload, frame.Y / 2);
            WriteU24LE(framePayload, frame.Width - 1);
            WriteU24LE(framePayload, frame.Height - 1);
            WriteU24LE(framePayload, frame.DurationMs);
            var frameFlags = 0;
            if (frame.DisposeToBackground) frameFlags |= 0x01;
            if (!frame.Blend) frameFlags |= 0x02;
            framePayload.WriteByte((byte)frameFlags);

            using (var vp8l = new MemoryStream()) {
                vp8l.Write(frame.Vp8lPayload, 0, frame.Vp8lPayload.Length);
                WriteChunk(framePayload, "VP8L", vp8l.ToArray());
            }

            WriteChunk(ms, "ANMF", framePayload.ToArray());
        }

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    internal readonly struct AnimationFrameSpec {
        public AnimationFrameSpec(
            byte[] vp8lPayload,
            int x,
            int y,
            int width,
            int height,
            int durationMs,
            bool blend,
            bool disposeToBackground) {
            Vp8lPayload = vp8lPayload;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            DurationMs = durationMs;
            Blend = blend;
            DisposeToBackground = disposeToBackground;
        }

        public byte[] Vp8lPayload { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int DurationMs { get; }
        public bool Blend { get; }
        public bool DisposeToBackground { get; }
    }

    internal static bool UsesAlpha(AnimationFrameSpec[] frames) {
        for (var i = 0; i < frames.Length; i++) {
            var payload = frames[i].Vp8lPayload;
            if (payload.Length < 5) continue;
            var bits = ReadU32LE(payload, 1);
            if (((bits >> 28) & 1) != 0) return true;
        }
        return false;
    }

    private static uint ReadU32LE(byte[] buffer, int offset) {
        if (offset < 0 || offset + 4 > buffer.Length) return 0;
        return (uint)(buffer[offset]
            | (buffer[offset + 1] << 8)
            | (buffer[offset + 2] << 16)
            | (buffer[offset + 3] << 24));
    }

    internal static void WriteChunk(Stream stream, string fourCc, byte[] payload) {
        WriteAscii(stream, fourCc);
        WriteU32LE(stream, (uint)payload.Length);
        stream.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0) {
            stream.WriteByte(0);
        }
    }

    internal static void WriteAscii(Stream stream, string text) {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void WriteU16LE(Stream stream, ushort value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }

    internal static void WriteU24LE(Stream stream, int value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
    }

    internal static void WriteU32LE(Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    internal static void WriteU32LE(byte[] buffer, int offset, uint value) {
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
