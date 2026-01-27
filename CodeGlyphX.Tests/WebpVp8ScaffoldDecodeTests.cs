using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ScaffoldDecodeTests
{
    [Fact]
    public void TryDecode_ScaffoldSignature_SucceedsWithRgbaOutput()
    {
        var boolData = CreateBoolData(length: 4096);
        ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = BuildKeyframePayload(width: 8, height: 8, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 32 + i;
        }

        var payload = BuildKeyframePayloadWithPartitionsAndTokens(8, 8, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryDecode(payload, out var rgba, out var width, out var height);

        Assert.True(success);
        Assert.Equal(8, width);
        Assert.Equal(8, height);
        Assert.Equal(8 * 8 * 4, rgba.Length);

        for (var i = 3; i < rgba.Length; i += 4)
        {
            Assert.Equal(255, rgba[i]);
        }
    }

    private static void ApplyScaffoldSignature(byte[] data)
    {
        data[0] = (byte)'S';
        data[1] = (byte)'C';
        data[2] = (byte)'F';
        data[3] = (byte)'0';
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
                payload[cursor + j] = (byte)((0x59 + (i * 41) + (j * 31)) & 0xFF);
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
