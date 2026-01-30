using System;
using System.Text;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpAnimationEncodeTests {
    [Fact]
    public void Webp_AnimationEncode_FirstFrameDecodes() {
        const int width = 4;
        const int height = 4;

        var frame1 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 255, 0, 0, 255),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var frame2 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 0, 255, 0, 255),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var webp = WebpWriter.WriteAnimationRgba32(
            width,
            height,
            new[] { frame1, frame2 },
            new WebpAnimationOptions(loopCount: 0, backgroundBgra: 0));

        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decoded.Length);
        AssertAllPixels(decoded, 255, 0, 0, 255);
    }

    [Fact]
    public void Webp_AnimationEncode_Lossy_FirstFramePreservesAlpha() {
        const int width = 4;
        const int height = 4;

        var frame1 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 20, 40, 200, 180),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var frame2 = new WebpAnimationFrame(
            CreateSolidRgba(width, height, 10, 200, 40, 255),
            width,
            height,
            width * 4,
            durationMs: 120,
            blend: false);

        var webp = WebpWriter.WriteAnimationRgba32Lossy(
            width,
            height,
            new[] { frame1, frame2 },
            new WebpAnimationOptions(loopCount: 0, backgroundBgra: 0),
            quality: 60);

        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decoded.Length);
        AssertAllAlpha(decoded, 180);
        Assert.True(ContainsAnimationFrameChunk(webp, "VP8 "));
        Assert.True(ContainsAnimationFrameChunk(webp, "ALPH"));
    }

    private static byte[] CreateSolidRgba(int width, int height, byte r, byte g, byte b, byte a) {
        var data = new byte[checked(width * height * 4)];
        for (var i = 0; i < data.Length; i += 4) {
            data[i] = r;
            data[i + 1] = g;
            data[i + 2] = b;
            data[i + 3] = a;
        }
        return data;
    }

    private static void AssertAllPixels(byte[] rgba, byte r, byte g, byte b, byte a) {
        for (var i = 0; i < rgba.Length; i += 4) {
            Assert.Equal(r, rgba[i]);
            Assert.Equal(g, rgba[i + 1]);
            Assert.Equal(b, rgba[i + 2]);
            Assert.Equal(a, rgba[i + 3]);
        }
    }

    private static void AssertAllAlpha(byte[] rgba, byte a) {
        for (var i = 0; i < rgba.Length; i += 4) {
            Assert.Equal(a, rgba[i + 3]);
        }
    }

    private static bool ContainsAnimationFrameChunk(byte[] webp, string fourCc) {
        var target = Encoding.ASCII.GetBytes(fourCc);
        if (target.Length != 4) throw new ArgumentException("FourCC must be 4 characters.", nameof(fourCc));
        if (webp.Length < 12) return false;
        if (!Matches(webp, 0, "RIFF") || !Matches(webp, 8, "WEBP")) return false;

        var offset = 12;
        while (offset + 8 <= webp.Length) {
            var chunkSize = ReadU32LE(webp, offset + 4);
            var dataOffset = offset + 8;
            if (dataOffset + chunkSize > webp.Length) break;

            if (Matches(webp, offset, "ANMF")) {
                var payload = new ReadOnlySpan<byte>(webp, dataOffset, (int)chunkSize);
                if (ContainsFrameChunk(payload, target)) return true;
            }

            offset = dataOffset + (int)chunkSize + ((int)chunkSize & 1);
        }

        return false;
    }

    private static bool ContainsFrameChunk(ReadOnlySpan<byte> anmfPayload, byte[] target) {
        if (anmfPayload.Length < 16 + 8) return false;
        var offset = 16;
        while (offset + 8 <= anmfPayload.Length) {
            var chunkSize = ReadU32LE(anmfPayload, offset + 4);
            var dataOffset = offset + 8;
            if (dataOffset + chunkSize > anmfPayload.Length) break;
            if (Matches(anmfPayload, offset, target)) return true;
            offset = dataOffset + (int)chunkSize + ((int)chunkSize & 1);
        }
        return false;
    }

    private static bool Matches(byte[] data, int offset, string text) {
        var bytes = Encoding.ASCII.GetBytes(text);
        return Matches(new ReadOnlySpan<byte>(data), offset, bytes);
    }

    private static bool Matches(ReadOnlySpan<byte> data, int offset, byte[] bytes) {
        if (offset < 0 || offset + bytes.Length > data.Length) return false;
        for (var i = 0; i < bytes.Length; i++) {
            if (data[offset + i] != bytes[i]) return false;
        }
        return true;
    }

    private static uint ReadU32LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }
}
