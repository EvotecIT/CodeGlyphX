using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpMetadataWriterTests {
    [Fact]
    public void WebpWriter_WritesMetadataChunks_Lossless() {
        var rgba = new byte[] { 10, 20, 30, 255 };
        var metadata = new WebpMetadata(
            icc: new byte[] { 1, 2, 3 },
            exif: new byte[] { 4, 5, 6, 7 },
            xmp: new byte[] { 8, 9 });

        var webp = WebpWriter.WriteRgba32(1, 1, rgba, 4, metadata);

        AssertChunk(webp, "VP8X");
        AssertChunk(webp, "ICCP");
        AssertChunk(webp, "EXIF");
        AssertChunk(webp, "XMP ");
        AssertChunk(webp, "VP8L");
    }

    [Fact]
    public void WebpWriter_WritesMetadataChunks_Animation() {
        var rgba = new byte[] { 10, 20, 30, 255 };
        var frame = new WebpAnimationFrame(rgba, width: 1, height: 1, stride: 4, durationMs: 100);
        var metadata = new WebpMetadata(
            icc: new byte[] { 1 },
            exif: new byte[] { 2 },
            xmp: new byte[] { 3 });

        var webp = WebpWriter.WriteAnimationRgba32(1, 1, new[] { frame }, new WebpAnimationOptions(), metadata);

        AssertChunk(webp, "VP8X");
        AssertChunk(webp, "ICCP");
        AssertChunk(webp, "EXIF");
        AssertChunk(webp, "XMP ");
        AssertChunk(webp, "ANIM");
        AssertChunk(webp, "ANMF");
    }

    private static void AssertChunk(byte[] webp, string fourCc) {
        Assert.True(ContainsChunk(webp, fourCc), $"Missing chunk {fourCc}");
    }

    private static bool ContainsChunk(byte[] webp, string fourCc) {
        if (webp.Length < 12) return false;
        if (!IsFourCc(webp, 0, "RIFF") || !IsFourCc(webp, 8, "WEBP")) return false;
        var offset = 12;
        while (offset + 8 <= webp.Length) {
            if (IsFourCc(webp, offset, fourCc)) return true;
            var size = ReadU32LE(webp, offset + 4);
            var padded = size + (size & 1);
            offset += 8 + (int)padded;
        }
        return false;
    }

    private static bool IsFourCc(byte[] data, int offset, string fourCc) {
        if (offset + 4 > data.Length) return false;
        return data[offset] == (byte)fourCc[0]
            && data[offset + 1] == (byte)fourCc[1]
            && data[offset + 2] == (byte)fourCc[2]
            && data[offset + 3] == (byte)fourCc[3];
    }

    private static uint ReadU32LE(byte[] data, int offset) {
        if (offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }
}
