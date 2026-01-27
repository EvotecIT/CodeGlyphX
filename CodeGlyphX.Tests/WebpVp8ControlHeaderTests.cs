using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ControlHeaderTests
{
    [Fact]
    public void TryReadControlHeader_KeyframeBoolData_SucceedsAndConsumesBytes()
    {
        var boolData = new byte[] { 0x9A, 0xBC, 0xDE, 0xF0 };
        var payload = BuildKeyframePayload(width: 16, height: 16, boolData);

        var success = WebpVp8Decoder.TryReadControlHeader(payload, out var control);

        Assert.True(success);
        Assert.InRange(control.ColorSpace, 0, 1);
        Assert.InRange(control.ClampType, 0, 1);
        Assert.InRange(control.BytesConsumed, 2, boolData.Length);
    }

    [Fact]
    public void BoolDecoder_TooShortData_Fails()
    {
        var decoder = new WebpVp8BoolDecoder(new byte[] { 0x00 });

        var success = decoder.TryReadBool(128, out _);

        Assert.False(success);
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

