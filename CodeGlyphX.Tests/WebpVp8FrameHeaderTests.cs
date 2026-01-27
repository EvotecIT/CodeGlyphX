using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8FrameHeaderTests
{
    [Fact]
    public void TryReadFrameHeader_KeyframeBoolData_SucceedsWithValidRanges()
    {
        var boolData = new byte[]
        {
            0x9A, 0xBC, 0xDE, 0xF0, 0x12, 0x34, 0x56, 0x78,
            0x89, 0xAB, 0xCD, 0xEF, 0x10, 0x32, 0x54, 0x76,
        };
        var payload = BuildKeyframePayload(width: 32, height: 24, boolData);

        var success = WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader);

        Assert.True(success);
        Assert.InRange(frameHeader.ControlHeader.ColorSpace, 0, 1);
        Assert.InRange(frameHeader.ControlHeader.ClampType, 0, 1);
        Assert.Contains(frameHeader.DctPartitionCount, new[] { 1, 2, 4, 8 });
        Assert.InRange(frameHeader.LoopFilter.Level, 0, 63);
        Assert.InRange(frameHeader.LoopFilter.Sharpness, 0, 7);
        Assert.InRange(frameHeader.Quantization.BaseQIndex, 0, 127);

        if (frameHeader.NoCoefficientSkip)
        {
            Assert.InRange(frameHeader.SkipProbability, 0, 255);
        }
    }

    private static byte[] BuildKeyframePayload(int width, int height, byte[] boolData)
    {
        const int keyframeHeaderSize = 7;
        var partitionSize = keyframeHeaderSize + boolData.Length;
        var payloadLength = 3 + partitionSize;
        var payload = new byte[payloadLength];

        var frameTag = (partitionSize << 5) | (1 << 4);
        payload[0] = (byte)(frameTag & 0xFF);
        payload[1] = (byte)((frameTag >> 8) & 0xFF);
        payload[2] = (byte)((frameTag >> 16) & 0xFF);

        payload[3] = 0x9D;
        payload[4] = 0x01;
        payload[5] = 0x2A;

        payload[6] = (byte)(width & 0xFF);
        payload[7] = (byte)((width >> 8) & 0x3F);
        payload[8] = (byte)(height & 0xFF);
        payload[9] = (byte)((height >> 8) & 0x3F);

        boolData.CopyTo(payload.AsSpan(10));
        return payload;
    }
}

