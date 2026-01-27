using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8BlockTokenScaffoldTests
{
    private static readonly int[] TokenExtraBits = new[] { 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 11 };
    private static readonly int[] TokenBaseMagnitude = new[] { 0, 0, 1, 2, 3, 4, 5, 7, 11, 19, 35, 67 };
    private static readonly int[] ZigZagToNaturalOrder = new[]
    {
        0,  1,  4,  8,
        5,  2,  3,  6,
        9, 12, 13, 10,
        7, 11, 14, 15,
    };

    [Fact]
    public void TryReadBlockTokenScaffold_ValidPayload_ProducesBlockShapedCursorData()
    {
        const int blocksPerPartition = 4;
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 80, height: 56, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 24 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(80, 56, boolData, partitionSizes);

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
                Assert.Equal(16, block.CoefficientsNaturalOrder.Length);
                Assert.Equal(16, block.DequantizedCoefficientsNaturalOrder.Length);
                Assert.Equal(16, block.Tokens.Length);
                Assert.InRange(block.TokensRead, 1, 16);

                var result = block.Result;
                Assert.Equal(block.BlockType, result.BlockType);
                Assert.Equal(block.DequantFactor, result.DequantFactor);
                Assert.Equal(block.CoefficientsNaturalOrder[0], result.Dc);
                Assert.Equal(block.DequantizedCoefficientsNaturalOrder[0], result.DequantDc);
                Assert.Equal(15, result.Ac.Length);
                Assert.Equal(15, result.DequantAc.Length);

                var pixels = block.BlockPixels;
                Assert.Equal(4, pixels.Width);
                Assert.Equal(4, pixels.Height);
                Assert.Equal(16, pixels.Samples.Length);

                Assert.InRange(pixels.BaseSample, (byte)0, (byte)255);
                for (var i = 0; i < pixels.Samples.Length; i++)
                {
                    Assert.InRange(pixels.Samples[i], 0, 255);
                }

                var expectedHasNonZeroAc = false;
                for (var i = 1; i < 16; i++)
                {
                    var acIndex = i - 1;
                    Assert.Equal(block.CoefficientsNaturalOrder[i], result.Ac[acIndex]);
                    Assert.Equal(block.DequantizedCoefficientsNaturalOrder[i], result.DequantAc[acIndex]);
                    if (block.CoefficientsNaturalOrder[i] != 0)
                    {
                        expectedHasNonZeroAc = true;
                    }
                }

                Assert.Equal(expectedHasNonZeroAc, result.HasNonZeroAc);
                Assert.Equal(block.ReachedEob, result.ReachedEob);
                Assert.Equal(block.TokensRead, result.TokensRead);

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
                    Assert.InRange(token.NaturalIndex, 0, 15);
                    Assert.Equal(ZigZagToNaturalOrder[token.CoefficientIndex], token.NaturalIndex);
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
                    Assert.Equal(token.CoefficientValue, block.CoefficientsNaturalOrder[token.NaturalIndex]);
                    Assert.Equal(
                        token.CoefficientValue * block.DequantFactor,
                        block.DequantizedCoefficientsNaturalOrder[token.NaturalIndex]);

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

    private static byte ClampToByte(int value)
    {
        if (value < byte.MinValue)
        {
            return byte.MinValue;
        }

        if (value > byte.MaxValue)
        {
            return byte.MaxValue;
        }

        return (byte)value;
    }
}
