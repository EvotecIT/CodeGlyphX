using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpMetadataWriterTests {
    [Theory]
    [InlineData("ICCP", 0x20)]
    [InlineData("EXIF", 0x08)]
    [InlineData("XMP ", 0x04)]
    public void WebpWriter_AdvertisesEachMetadataFeature(string feature, int expectedFlag) {
        var metadata = new WebpMetadata(
            icc: feature == "ICCP" ? new byte[] { 1 } : null,
            exif: feature == "EXIF" ? new byte[] { 1 } : null,
            xmp: feature == "XMP " ? new byte[] { 1 } : null);
        byte[] webp = WebpWriter.WriteRgba32(1, 1, new byte[] { 10, 20, 30, 255 }, 4, metadata);

        Assert.Equal(expectedFlag, webp[20]); // First VP8X payload byte.
        AssertChunk(webp, feature);
    }

    [Theory]
    [InlineData("ICCP", 0x0C)]
    [InlineData("EXIF", 0x24)]
    [InlineData("XMP ", 0x28)]
    public void WebpWriter_OmitsEmptyMetadataChunksAndTheirFeatureFlags(string emptyFeature, int expectedFlags) {
        var metadata = new WebpMetadata(
            icc: emptyFeature == "ICCP" ? Array.Empty<byte>() : new byte[] { 1 },
            exif: emptyFeature == "EXIF" ? Array.Empty<byte>() : new byte[] { 2 },
            xmp: emptyFeature == "XMP " ? Array.Empty<byte>() : new byte[] { 3 });
        byte[] webp = WebpWriter.WriteRgba32(1, 1, new byte[] { 10, 20, 30, 255 }, 4, metadata);

        Assert.Equal(expectedFlags, webp[20]);
        foreach (var feature in new[] { "ICCP", "EXIF", "XMP " }) {
            Assert.Equal(feature != emptyFeature, ContainsChunk(webp, feature));
        }
    }

    [Theory]
    [InlineData(false, 0x02)]
    [InlineData(true, 0x12)]
    public void WebpWriter_AdvertisesAnimationAndTransparency(bool transparent, int expectedFlags) {
        var rgba = new byte[] { 10, 20, 30, transparent ? (byte)128 : (byte)255 };
        var frame = new WebpAnimationFrame(rgba, width: 1, height: 1, stride: 4, durationMs: 100);
        byte[] webp = WebpWriter.WriteAnimationRgba32Lossy(1, 1, new[] { frame }, default, quality: 85);

        Assert.Equal(expectedFlags, webp[20]);
        AssertChunk(webp, "ANIM");
        AssertChunk(webp, "ANMF");

        Assert.True(WebpReader.TryDecodeAnimationFrames(webp, out var frames, out int width, out int height, out _));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        var decodedFrame = Assert.Single(frames);
        Assert.Equal(100, decodedFrame.DurationMs);
        Assert.Equal(rgba.Length, decodedFrame.Rgba.Length);
        Assert.Equal(rgba[3], decodedFrame.Rgba[3]);
    }

    [Fact]
    public void WebpReader_DoesNotDecodeAnimationWithoutAnimationFeature() {
        var frame = new WebpAnimationFrame(new byte[] { 10, 20, 30, 255 }, 1, 1, 4, durationMs: 100);
        byte[] webp = WebpWriter.WriteAnimationRgba32(1, 1, new[] { frame }, default);
        webp[20] &= 0xFD; // The ANIM/ANMF chunks cannot activate animation without the VP8X flag.

        Assert.False(WebpReader.TryDecodeAnimationFrames(webp, out _, out _, out _, out _));
        Assert.False(WebpReader.TryDecodeAnimationCanvasFrames(webp, out _, out _, out _, out _));
        Assert.Throws<FormatException>(() => WebpReader.DecodeRgba32(webp, out _, out _));
    }

    [Fact]
    public void WebpReader_RejectsAnimationWithTruncatedExtendedHeader() {
        var frame = new WebpAnimationFrame(new byte[] { 10, 20, 30, 255 }, 1, 1, 4, durationMs: 100);
        byte[] webp = WebpWriter.WriteAnimationRgba32(1, 1, new[] { frame }, default);

        // Keep the RIFF/chunk framing valid while shortening the ten-byte VP8X payload to eight bytes.
        var malformed = new byte[webp.Length - 2];
        webp.AsSpan(0, 28).CopyTo(malformed);
        webp.AsSpan(30).CopyTo(malformed.AsSpan(28));
        malformed[16] = 8;
        var riffSize = malformed.Length - 8;
        malformed[4] = (byte)riffSize;
        malformed[5] = (byte)(riffSize >> 8);
        malformed[6] = (byte)(riffSize >> 16);
        malformed[7] = (byte)(riffSize >> 24);

        AssertChunk(malformed, "VP8X");
        AssertChunk(malformed, "ANIM");
        AssertChunk(malformed, "ANMF");
        Assert.False(WebpReader.TryDecodeAnimationFrames(malformed, out _, out _, out _, out _));
        Assert.False(WebpReader.TryDecodeAnimationCanvasFrames(malformed, out _, out _, out _, out _));
        Assert.Throws<FormatException>(() => WebpReader.DecodeRgba32(malformed, out _, out _));
    }

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
