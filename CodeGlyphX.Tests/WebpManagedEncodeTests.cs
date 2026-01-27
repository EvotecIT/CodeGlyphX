using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpManagedEncodeTests {
    [Fact]
    public void Webp_ManagedEncode_Vp8L_LiteralOnly_RoundTripsBinaryImage() {
        const int width = 2;
        const int height = 2;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,       255, 255, 255, 255,
            255, 255, 255, 255, 0, 0, 0, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_LiteralOnly_RoundTripsMultipleChannelValues() {
        const int width = 3;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,
            64, 0, 0, 255,
            128, 0, 0, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_ColorIndexingPalette_RoundTripsSmallPalette() {
        const int width = 5;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 255,
            32, 32, 32, 255,
            64, 64, 64, 255,
            96, 96, 96, 255,
            128, 128, 128, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);

        var payload = ExtractVp8lPayload(webp);
        var reader = new WebpBitReader(payload);
        Assert.True(WebpVp8lDecoder.TryReadHeader(ref reader, out _));
        var hasTransform = reader.ReadBits(1);
        Assert.True(hasTransform == 0 || hasTransform == 1);
        if (hasTransform == 1) {
            var firstTransform = reader.ReadBits(2);
            if (firstTransform == 2) {
                Assert.Equal(1, reader.ReadBits(1)); // another transform follows
                firstTransform = reader.ReadBits(2);
            }
            Assert.True(firstTransform == 0 || firstTransform == 3);
        }
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_FallsBackWhenPaletteTooLarge() {
        const int width = 17;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var v = (byte)(x * 8);
            var i = x * 4;
            rgba[i] = v;
            rgba[i + 1] = v;
            rgba[i + 2] = v;
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_RunLengthBackref_RoundTripsSolidRun() {
        const int width = 12;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var i = x * 4;
            rgba[i] = 10;
            rgba[i + 1] = 20;
            rgba[i + 2] = 30;
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_SmallDistanceBackref_RoundTripsAlternatingPattern() {
        const int width = 14;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var i = x * 4;
            var dark = (x & 1) == 0;
            rgba[i] = dark ? (byte)0 : (byte)200;
            rgba[i + 1] = dark ? (byte)0 : (byte)200;
            rgba[i + 2] = dark ? (byte)0 : (byte)200;
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_MidDistanceBackref_RoundTripsRepeatedBlock() {
        const int block = 12;
        const int repeats = 3;
        const int width = block * repeats;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var i = x * 4;
            var v = (byte)((x % block) * 16);
            rgba[i] = v;
            rgba[i + 1] = (byte)(255 - v);
            rgba[i + 2] = (byte)(v / 2);
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_NoTransform_WhenPaletteTooLarge() {
        const int width = 20;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[height * stride];
        for (var x = 0; x < width; x++) {
            var i = x * 4;
            rgba[i] = (byte)(x * 7);
            rgba[i + 1] = (byte)(255 - (x * 7));
            rgba[i + 2] = (byte)(x * 3);
            rgba[i + 3] = 255;
        }

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        var payload = ExtractVp8lPayload(webp);
        var reader = new WebpBitReader(payload);
        Assert.True(WebpVp8lDecoder.TryReadHeader(ref reader, out _));
        var hasTransform = reader.ReadBits(1);
        Assert.True(hasTransform == 0 || hasTransform == 1);
        if (hasTransform == 1) {
            var firstTransform = reader.ReadBits(2);
            if (firstTransform == 2) {
                Assert.Equal(1, reader.ReadBits(1)); // another transform follows
                Assert.Equal(0, reader.ReadBits(2)); // predictor transform
            } else {
                Assert.Equal(0, firstTransform); // predictor transform
            }
        }
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8L_ColorIndexingPalette_RoundTripsAlphaPalette() {
        const int width = 8;
        const int height = 1;
        const int stride = width * 4;

        var rgba = new byte[] {
            0, 0, 0, 0,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            0, 0, 0, 0,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255
        };

        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(rgba, decoded);
    }

    private static byte[] ExtractVp8lPayload(byte[] webp) {
        Assert.True(WebpReader.IsWebp(webp));
        Assert.True(webp.Length >= 12);

        var riffSize = ReadU32LE(webp, 4);
        var riffLimit = webp.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(webp, offset);
            var chunkSize = ReadU32LE(webp, offset + 4);
            var dataOffset = offset + 8;
            var chunkLength = (int)chunkSize;

            if (fourCc == 0x4C385056) { // "VP8L"
                var payload = new byte[chunkLength];
                Array.Copy(webp, dataOffset, payload, 0, chunkLength);
                return payload;
            }

            var padded = chunkLength + (chunkLength & 1);
            offset = dataOffset + padded;
        }

        throw new InvalidOperationException("VP8L payload not found.");
    }

    private static uint ReadU32LE(byte[] data, int offset) {
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }
}
