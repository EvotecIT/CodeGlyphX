using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8BlockTokenScaffoldTests
{
    private static readonly int[] TokenExtraBits = new[] { 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 11 };
    private static readonly int[] TokenBaseMagnitude = new[] { 0, 0, 1, 2, 3, 4, 5, 7, 11, 19, 35, 67 };

    [Fact]
    public void TryReadBlockTokenScaffold_ValidPayload_ProducesBlockShapedCursorData()
    {
        const int blocksPerPartition = 2;
        var boolData = CreateBoolData(length: 4096);
        var firstPartitionOnly = BuildKeyframePayload(width: 80, height: 56, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 12 + i;
        }

        var payload = BuildKeyframePayloadWithPartitionsAndTokens(80, 56, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryReadBlockTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(dctCount, scaffold.Partitions.Length);
        Assert.Equal(dctCount * blocksPerPartition, scaffold.TotalBlocksRead);

        for (var p = 0; p < scaffold.Partitions.Length; p++)
        {
            var partition = scaffold.Partitions[p];
            Assert.Equal(partitionSizes[p], partition.Size);
            Assert.Equal(blocksPerPartition, partition.Blocks.Length);
            Assert.Equal(blocksPerPartition, partition.BlocksRead);
            Assert.InRange(partition.BytesConsumed, 2, partition.Size);
            Assert.InRange(partition.TokensRead, blocksPerPartition, blocksPerPartition * 16);

            for (var b = 0; b < partition.Blocks.Length; b++)
            {
                var block = partition.Blocks[b];
                Assert.Equal(b, block.BlockIndex);
                Assert.InRange(block.BlockType, 0, 3);
                Assert.True(block.DequantFactor > 0);
                Assert.Equal(16, block.Coefficients.Length);
                Assert.Equal(16, block.DequantizedCoefficients.Length);
                Assert.Equal(16, block.Tokens.Length);
                Assert.InRange(block.TokensRead, 1, 16);

                var expectedCoefficientIndex = 0;
                var sawEob = false;
                for (var t = 0; t < block.TokensRead; t++)
                {
                    var token = block.Tokens[t];
                    Assert.Equal(expectedCoefficientIndex, token.CoefficientIndex);
                    Assert.InRange(token.Band, 0, 7);
                    Assert.InRange(token.PrevContextBefore, 0, 2);
                    Assert.InRange(token.PrevContextAfter, 0, 2);
                    Assert.InRange(token.TokenCode, 0, 11);
                    Assert.Equal(block.BlockType, token.BlockType);
                    Assert.Equal(token.TokenCode != 0, token.HasMore);
                    Assert.Equal(token.TokenCode >= 2, token.IsNonZero);
                    Assert.True(token.ExtraBitsValue >= 0);

                    var extraBits = TokenExtraBits[token.TokenCode];
                    if (extraBits > 0)
                    {
                        Assert.InRange(token.ExtraBitsValue, 0, (1 << extraBits) - 1);
                    }
                    else
                    {
                        Assert.Equal(0, token.ExtraBitsValue);
                    }

                    var expectedMagnitude = ComputeExpectedMagnitude(token.TokenCode, token.ExtraBitsValue);
                    Assert.Equal(expectedMagnitude, Math.Abs(token.CoefficientValue));
                    Assert.Equal(token.CoefficientValue, block.Coefficients[token.CoefficientIndex]);
                    Assert.Equal(
                        block.Coefficients[token.CoefficientIndex] * block.DequantFactor,
                        block.DequantizedCoefficients[token.CoefficientIndex]);

                    if (!token.HasMore)
                    {
                        sawEob = true;
                        break;
                    }

                    expectedCoefficientIndex++;
                }

                if (sawEob)
                {
                    Assert.True(block.ReachedEob);
                }
            }
        }
    }

    private static int ComputeExpectedMagnitude(int tokenCode, int extraBitsValue)
    {
        if (tokenCode <= 1)
        {
            return 0;
        }

        if (tokenCode <= 5)
        {
            return TokenBaseMagnitude[tokenCode];
        }

        return TokenBaseMagnitude[tokenCode] + extraBitsValue;
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
                payload[cursor + j] = (byte)((0x91 + (i * 37) + (j * 19)) & 0xFF);
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
