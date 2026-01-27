using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8RgbaScaffoldTests
{
    [Fact]
    public void TryReadMacroblockRgbaScaffold_ValidPayload_ProducesRgbaBuffer()
    {
        var boolData = CreateBoolData(length: 4096);
        var firstPartitionOnly = BuildKeyframePayload(width: 100, height: 68, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 28 + i;
        }

        var payload = BuildKeyframePayloadWithPartitionsAndTokens(100, 68, boolData, partitionSizes);

        Assert.True(WebpVp8Decoder.TryReadMacroblockScaffold(payload, out var macroblock));
        var expected = ConvertYuvToRgb(macroblock.YPlane[0], macroblock.UPlane[0], macroblock.VPlane[0]);

        var success = WebpVp8Decoder.TryReadMacroblockRgbaScaffold(payload, out var rgba);

        Assert.True(success);
        Assert.Equal(8, rgba.Width);
        Assert.Equal(8, rgba.Height);
        Assert.Equal(8 * 8 * 4, rgba.Rgba.Length);
        Assert.InRange(rgba.BlocksPlaced, 1, 4);

        Assert.Equal(expected.R, rgba.Rgba[0]);
        Assert.Equal(expected.G, rgba.Rgba[1]);
        Assert.Equal(expected.B, rgba.Rgba[2]);
        Assert.Equal(255, rgba.Rgba[3]);
    }

    private static (byte R, byte G, byte B) ConvertYuvToRgb(byte y, byte u, byte v)
    {
        var yy = y;
        var uu = u - 128;
        var vv = v - 128;

        var r = yy + (int)(1.402 * vv);
        var g = yy - (int)(0.344136 * uu) - (int)(0.714136 * vv);
        var b = yy + (int)(1.772 * uu);

        return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    private static byte ClampToByte(int value)
    {
        if (value < byte.MinValue) return byte.MinValue;
        if (value > byte.MaxValue) return byte.MaxValue;
        return (byte)value;
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

    private static byte[] BuildKeyframePayloadWithPartitionsAndTokens(
        int width,
        int height,
        byte[] boolData,
        int[] partitionSizes)
    {
        var firstPartition = BuildKeyframePayload(width, height, boolData);

        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartition, out var frameHeader));
        var dctCount = frameHeader.DctPartitionCount;
        Assert.Equal(dctCount, partitionSizes.Length);

        var sizeTableBytes = 3 * (dctCount - 1);
        var tokenBytes = Sum(partitionSizes);
        var payload = new byte[firstPartition.Length + sizeTableBytes + tokenBytes];
        firstPartition.CopyTo(payload, 0);

        var sizeTableOffset = firstPartition.Length;
        for (var i = 0; i < dctCount - 1; i++)
        {
            WriteU24LE(payload, sizeTableOffset + (i * 3), partitionSizes[i]);
        }

        var tokenOffset = sizeTableOffset + sizeTableBytes;
        var cursor = tokenOffset;
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            var size = partitionSizes[i];
            for (var j = 0; j < size; j++)
            {
                payload[cursor + j] = (byte)((0x6B + (i * 47) + (j * 29)) & 0xFF);
            }

            cursor += size;
        }

        return payload;
    }

    private static void WriteU24LE(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static int Sum(int[] values)
    {
        var sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum;
    }
}

