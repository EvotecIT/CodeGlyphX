using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8CoefficientProbabilitiesTests
{
    [Fact]
    public void TryReadFrameHeader_ParsesCoefficientProbabilityTableShape()
    {
        var boolData = CreateBoolData(length: 4096);
        var payload = BuildKeyframePayload(width: 40, height: 28, boolData);

        var success = WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader);

        Assert.True(success);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Probabilities.Length);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Updated.Length);
        Assert.InRange(frameHeader.CoefficientProbabilities.UpdatedCount, 0, 1056);
        Assert.True(frameHeader.CoefficientProbabilities.BytesConsumed >= frameHeader.ControlHeader.BytesConsumed);
    }

    private static byte[] CreateBoolData(int length)
    {
        var data = new byte[length];
        var value = 0x5A;
        for (var i = 0; i < data.Length; i++)
        {
            value = (value * 73 + 41) & 0xFF;
            data[i] = (byte)value;
        }

        return data;
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

