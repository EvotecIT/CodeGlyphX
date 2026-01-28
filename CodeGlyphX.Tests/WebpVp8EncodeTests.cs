using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8EncodeTests {
    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_Decodes() {
        const int width = 16;
        const int height = 12;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                rgba[offset] = (byte)(x * 12);
                rgba[offset + 1] = (byte)(y * 18);
                rgba[offset + 2] = (byte)(x * 8 + y * 3);
                rgba[offset + 3] = 255;
            }
        }

        var webp = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 50);

        Assert.True(ContainsChunk(webp, "VP8 "));

        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decoded.Length);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_WithAlpha_UsesAlphChunk() {
        const int width = 12;
        const int height = 10;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                rgba[offset] = (byte)(x * 9);
                rgba[offset + 1] = (byte)(y * 11);
                rgba[offset + 2] = (byte)(x * 5 + y * 7);
                rgba[offset + 3] = (byte)(x * 17 + y * 13);
            }
        }

        var webp = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 55);

        Assert.True(ContainsChunk(webp, "VP8 "));
        Assert.True(ContainsChunk(webp, "ALPH"));

        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);

        Assert.Equal(rgba[3], decoded[3]);
        var mid = (height / 2 * width + width / 2) * 4 + 3;
        Assert.Equal(rgba[mid], decoded[mid]);
        var last = (height * width - 1) * 4 + 3;
        Assert.Equal(rgba[last], decoded[last]);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_WithAlpha_RoundTripsAlpha() {
        const int width = 16;
        const int height = 14;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                rgba[offset] = (byte)(x * 4 + y * 3);
                rgba[offset + 1] = (byte)(x * 7 + y * 2);
                rgba[offset + 2] = (byte)(x * 5 + y * 9);
                rgba[offset + 3] = (byte)((x * 17 + y * 13) & 0xFF);
            }
        }

        var webp = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 60);
        var decoded = WebpReader.DecodeRgba32(webp, out var decodedWidth, out var decodedHeight);

        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4 + 3;
                Assert.Equal(rgba[offset], decoded[offset]);
            }
        }
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_Quality_ImprovesError() {
        const int width = 24;
        const int height = 18;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                rgba[offset] = (byte)(x * 7 + y * 3);
                rgba[offset + 1] = (byte)(x * 5 + y * 11);
                rgba[offset + 2] = (byte)(x * 9 + y * 2);
                rgba[offset + 3] = (byte)(255 - (x * 3 + y * 5));
            }
        }

        var webpLow = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 20);
        var webpHigh = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 80);

        var decodedLow = WebpReader.DecodeRgba32(webpLow, out var lowW, out var lowH);
        var decodedHigh = WebpReader.DecodeRgba32(webpHigh, out var highW, out var highH);

        Assert.Equal(width, lowW);
        Assert.Equal(height, lowH);
        Assert.Equal(width, highW);
        Assert.Equal(height, highH);

        var errorLow = ComputeMeanAbsoluteError(rgba, decodedLow, width, height);
        var errorHigh = ComputeMeanAbsoluteError(rgba, decodedHigh, width, height);
        Assert.True(errorHigh <= errorLow);
    }

    private static bool ContainsChunk(byte[] data, string fourCc) {
        if (data.Length < 12) return false;
        if (ReadU32LE(data, 0) != 0x46464952) return false; // RIFF
        if (ReadU32LE(data, 8) != 0x50424557) return false; // WEBP

        var riffSize = ReadU32LE(data, 4);
        var riffLimit = data.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }

        var target = BitConverter.ToUInt32(System.Text.Encoding.ASCII.GetBytes(fourCc), 0);
        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var chunkFourCc = ReadU32LE(data, offset);
            var chunkSize = ReadU32LE(data, offset + 4);
            var dataOffset = offset + 8;
            if (chunkFourCc == target) return true;
            var padded = (int)chunkSize + ((int)chunkSize & 1);
            offset = dataOffset + padded;
        }

        return false;
    }

    private static uint ReadU32LE(byte[] data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

    private static double ComputeMeanAbsoluteError(byte[] original, byte[] decoded, int width, int height) {
        var pixels = width * height;
        var stride = width * 4;
        long sum = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                var dr = original[offset] - decoded[offset];
                var dg = original[offset + 1] - decoded[offset + 1];
                var db = original[offset + 2] - decoded[offset + 2];
                if (dr < 0) dr = -dr;
                if (dg < 0) dg = -dg;
                if (db < 0) db = -db;
                sum += dr + dg + db;
            }
        }

        return (double)sum / (pixels * 3);
    }
}
