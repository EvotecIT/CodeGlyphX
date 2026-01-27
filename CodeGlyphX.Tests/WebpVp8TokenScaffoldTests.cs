using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8TokenScaffoldTests
{
    [Fact]
    public void TryReadTokenScaffold_ValidPayload_ReadsScaffoldTokensPerPartition()
    {
        const int expectedTokensPerPartition = 4;
        var boolData = CreateBoolData(length: 4096);
        var firstPartitionOnly = BuildKeyframePayload(width: 72, height: 48, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 8 + i;
        }

        var payload = BuildKeyframePayloadWithPartitionsAndTokens(72, 48, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryReadTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(dctCount, scaffold.Partitions.Length);
        Assert.Equal(dctCount * expectedTokensPerPartition, scaffold.TotalTokensRead);

        for (var i = 0; i < scaffold.Partitions.Length; i++)
        {
            var partition = scaffold.Partitions[i];
            Assert.Equal(partitionSizes[i], partition.Size);
            Assert.Equal(expectedTokensPerPartition, partition.TokensRead);
            Assert.Equal(expectedTokensPerPartition, partition.Tokens.Length);
            Assert.InRange(partition.BytesConsumed, 2, partition.Size);
        }
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
                payload[cursor + j] = (byte)((0xC3 + (i * 29) + (j * 13)) & 0xFF);
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

