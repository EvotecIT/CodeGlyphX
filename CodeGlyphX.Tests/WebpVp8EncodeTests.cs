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
    public void Webp_ManagedEncode_Vp8_Lossy_Quality_ChangesQuantization() {
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
        Assert.Equal(width * height * 4, decodedLow.Length);
        Assert.Equal(width * height * 4, decodedHigh.Length);

        Assert.True(TryExtractChunk(webpLow, "VP8 ", out var lowPayload));
        Assert.True(TryExtractChunk(webpHigh, "VP8 ", out var highPayload));
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(lowPayload, out var lowHeader));
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(highPayload, out var highHeader));
        Assert.True(highHeader.Quantization.BaseQIndex <= lowHeader.Quantization.BaseQIndex);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_UsesSegmentationForMixedVariance() {
        const int width = 32;
        const int height = 32;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                var value = x < 16
                    ? (byte)30
                    : (byte)(((x + y) & 1) == 0 ? 0 : 255);
                rgba[offset] = value;
                rgba[offset + 1] = value;
                rgba[offset + 2] = value;
                rgba[offset + 3] = 255;
            }
        }

        var webp = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 40);
        Assert.True(TryExtractChunk(webp, "VP8 ", out var payload));

        Assert.True(WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader));
        Assert.True(frameHeader.Segmentation.Enabled);
        Assert.True(frameHeader.Segmentation.UpdateMap);
        Assert.True(frameHeader.Segmentation.UpdateData);
        Assert.Contains(frameHeader.Segmentation.QuantizerDeltas, delta => delta != 0);

        Assert.Contains(frameHeader.Segmentation.SegmentProbabilities, prob => prob >= 0);
    }

    [Fact]
    public void Webp_ManagedEncode_Vp8_Lossy_DisablesSegmentationForFlatImage() {
        const int width = 32;
        const int height = 32;
        var stride = width * 4;
        var rgba = new byte[checked(height * stride)];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var offset = row + x * 4;
                rgba[offset] = 42;
                rgba[offset + 1] = 42;
                rgba[offset + 2] = 42;
                rgba[offset + 3] = 255;
            }
        }

        var webp = WebpWriter.WriteRgba32Lossy(width, height, rgba, stride, quality: 40);
        Assert.True(TryExtractChunk(webp, "VP8 ", out var payload));

        Assert.True(WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader));
        Assert.False(frameHeader.Segmentation.Enabled);
        Assert.True(frameHeader.NoCoefficientSkip);
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

    private static bool TryExtractChunk(byte[] data, string fourCc, out byte[] chunkData) {
        chunkData = Array.Empty<byte>();
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
            if (chunkFourCc == target) {
                if (dataOffset + chunkSize > data.Length) return false;
                chunkData = new byte[chunkSize];
                Buffer.BlockCopy(data, dataOffset, chunkData, 0, (int)chunkSize);
                return true;
            }

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

}
