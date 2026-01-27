using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8MacroblockTokenScaffoldTests
{
    [Fact]
    public void TryReadMacroblockTokenScaffold_ValidPayload_AssignsPartitionsByRow()
    {
        const int expectedWidth = 33;
        const int expectedHeight = 33;
        const int expectedCols = 3;
        const int expectedRows = 3;
        const int expectedMacroblocks = expectedCols * expectedRows;

        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        WebpVp8TestHelper.ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(expectedWidth, expectedHeight, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 48 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            expectedWidth,
            expectedHeight,
            boolData,
            partitionSizes,
            tokenSeed: 0x6D);

        Assert.True(WebpVp8Decoder.TryReadBlockTokenScaffold(payload, out var blockScaffold));
        var partitionByteTotals = new int[dctCount];
        var partitionTokenTotals = new int[dctCount];
        var macroblocksPerPartition = new int[dctCount];
        for (var y = 0; y < expectedRows; y++)
        {
            var partitionIndex = y % dctCount;
            macroblocksPerPartition[partitionIndex] += expectedCols;
        }
        var totalPartitionBytes = 0;
        var usedPartitionBytes = 0;
        for (var p = 0; p < dctCount; p++)
        {
            partitionByteTotals[p] = blockScaffold.Partitions[p].BytesConsumed;
            var tokensPerMacroblock = blockScaffold.Partitions[p].TokensRead;
            partitionTokenTotals[p] = tokensPerMacroblock * System.Math.Max(1, macroblocksPerPartition[p]);
            totalPartitionBytes += partitionByteTotals[p];
            if (macroblocksPerPartition[p] > 0)
            {
                usedPartitionBytes += partitionByteTotals[p];
            }
        }

        var success = WebpVp8Decoder.TryReadMacroblockTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(expectedCols, scaffold.MacroblockCols);
        Assert.Equal(expectedRows, scaffold.MacroblockRows);
        Assert.Equal(dctCount, scaffold.PartitionCount);
        Assert.Equal(expectedMacroblocks, scaffold.Macroblocks.Length);
        Assert.Equal(expectedMacroblocks * 4, scaffold.TotalBlocksAssigned);
        Assert.InRange(scaffold.TotalTokensRead, scaffold.TotalBlocksAssigned, scaffold.TotalBlocksAssigned * 16);
        Assert.InRange(scaffold.TotalBytesConsumed, 0, totalPartitionBytes);
        Assert.Equal(usedPartitionBytes, scaffold.TotalBytesConsumed);

        var lastBytesAfter = new int[dctCount];
        var lastTokensAfter = new int[dctCount];
        var bytesSum = 0;

        for (var i = 0; i < scaffold.Macroblocks.Length; i++)
        {
            var macroblock = scaffold.Macroblocks[i];
            var expectedPartition = macroblock.Header.Y % dctCount;
            Assert.Equal(expectedPartition, macroblock.PartitionIndex);
            Assert.Equal(lastBytesAfter[macroblock.PartitionIndex], macroblock.PartitionBytesBefore);
            Assert.Equal(lastTokensAfter[macroblock.PartitionIndex], macroblock.PartitionTokensBefore);
            Assert.InRange(macroblock.PartitionBytesConsumed, 0, partitionByteTotals[macroblock.PartitionIndex]);
            Assert.InRange(macroblock.PartitionBytesAfter, macroblock.PartitionBytesBefore, partitionByteTotals[macroblock.PartitionIndex]);
            Assert.InRange(macroblock.PartitionTokensAfter, macroblock.PartitionTokensBefore, partitionTokenTotals[macroblock.PartitionIndex]);
            Assert.Equal(4, macroblock.Blocks.Length);
            Assert.InRange(macroblock.TokensRead, macroblock.Blocks.Length, macroblock.Blocks.Length * 16);

            lastBytesAfter[macroblock.PartitionIndex] = macroblock.PartitionBytesAfter;
            lastTokensAfter[macroblock.PartitionIndex] = macroblock.PartitionTokensAfter;
            bytesSum += macroblock.PartitionBytesConsumed;

            for (var b = 0; b < macroblock.Blocks.Length; b++)
            {
                var block = macroblock.Blocks[b];
                Assert.Equal(b, block.BlockIndex);
                Assert.InRange(block.BlockType, 0, 3);
            }
        }

        Assert.Equal(scaffold.TotalBytesConsumed, bytesSum);

        for (var p = 0; p < dctCount; p++)
        {
            if (macroblocksPerPartition[p] > 0)
            {
                Assert.Equal(partitionByteTotals[p], lastBytesAfter[p]);
            }
            else
            {
                Assert.Equal(0, lastBytesAfter[p]);
            }
        }
    }
}
