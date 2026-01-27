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

        Assert.True(WebpVp8Decoder.TryReadMacroblockHeaderScaffold(payload, out var headers));
        Assert.True(WebpVp8Decoder.TryReadBlockTokenScaffold(payload, out var blockScaffold));
        var partitionByteTotals = new int[dctCount];
        var partitionTokenTotals = new int[dctCount];
        var rowsPerPartition = new int[dctCount];
        for (var row = 0; row < headers.MacroblockRows; row++)
        {
            var partitionIndex = row % dctCount;
            rowsPerPartition[partitionIndex]++;
        }

        var partitionRowIndexByGlobalRow = new int[headers.MacroblockRows];
        var partitionRowCursor = new int[dctCount];
        for (var row = 0; row < headers.MacroblockRows; row++)
        {
            var partitionIndex = row % dctCount;
            partitionRowIndexByGlobalRow[row] = partitionRowCursor[partitionIndex];
            partitionRowCursor[partitionIndex]++;
        }

        var partitionRowBudgets = new int[dctCount][];
        var partitionRowNonSkippedCounts = new int[dctCount][];
        var totalPartitionBytes = 0;
        for (var p = 0; p < dctCount; p++)
        {
            partitionByteTotals[p] = blockScaffold.Partitions[p].BytesConsumed;
            totalPartitionBytes += partitionByteTotals[p];

            var rowCount = rowsPerPartition[p];
            if (rowCount <= 0)
            {
                partitionRowBudgets[p] = System.Array.Empty<int>();
                partitionRowNonSkippedCounts[p] = System.Array.Empty<int>();
                continue;
            }

            var budgets = new int[rowCount];
            var baseBytesPerRow = partitionByteTotals[p] / rowCount;
            var remainder = partitionByteTotals[p] % rowCount;
            for (var r = 0; r < budgets.Length; r++)
            {
                budgets[r] = baseBytesPerRow + (r < remainder ? 1 : 0);
            }

            partitionRowBudgets[p] = budgets;
            partitionRowNonSkippedCounts[p] = new int[rowCount];
        }

        for (var i = 0; i < headers.Macroblocks.Length; i++)
        {
            var header = headers.Macroblocks[i];
            var partitionIndex = header.Y % dctCount;
            var rowIndex = partitionRowIndexByGlobalRow[header.Y];
            if (!header.SkipCoefficients)
            {
                partitionRowNonSkippedCounts[partitionIndex][rowIndex]++;
            }
        }

        var usedPartitionBytes = 0;
        var usedPartitionBytesPerPartition = new int[dctCount];
        var nonSkippedMacroblocksPerPartition = new int[dctCount];
        for (var p = 0; p < dctCount; p++)
        {
            var tokensPerMacroblock = blockScaffold.Partitions[p].TokensRead * 6;

            var nonSkippedCount = 0;
            var rowBudgets = partitionRowBudgets[p];
            var rowNonSkipped = partitionRowNonSkippedCounts[p];
            for (var r = 0; r < rowNonSkipped.Length; r++)
            {
                nonSkippedCount += rowNonSkipped[r];
                if (rowNonSkipped[r] > 0)
                {
                    usedPartitionBytesPerPartition[p] += rowBudgets[r];
                }
            }

            nonSkippedMacroblocksPerPartition[p] = nonSkippedCount;
            partitionTokenTotals[p] = tokensPerMacroblock * nonSkippedCount;
            usedPartitionBytes += usedPartitionBytesPerPartition[p];
        }

        var success = WebpVp8Decoder.TryReadMacroblockTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(headers.MacroblockCols, scaffold.MacroblockCols);
        Assert.Equal(headers.MacroblockRows, scaffold.MacroblockRows);
        Assert.Equal(dctCount, scaffold.PartitionCount);
        Assert.Equal(expectedMacroblocks, scaffold.Macroblocks.Length);
        Assert.Equal(expectedMacroblocks * 24, scaffold.TotalBlocksAssigned);
        Assert.InRange(scaffold.TotalTokensRead, 0, scaffold.TotalBlocksAssigned * 16);
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

            var rowIndex = partitionRowIndexByGlobalRow[macroblock.Header.Y];
            var rowNonSkipped = partitionRowNonSkippedCounts[macroblock.PartitionIndex][rowIndex];
            var rowBudget = partitionRowBudgets[macroblock.PartitionIndex][rowIndex];

            Assert.Equal(lastBytesAfter[macroblock.PartitionIndex], macroblock.PartitionBytesBefore);
            Assert.Equal(lastTokensAfter[macroblock.PartitionIndex], macroblock.PartitionTokensBefore);
            Assert.Equal(24, macroblock.Blocks.Length);

            if (macroblock.Header.SkipCoefficients || rowNonSkipped == 0)
            {
                Assert.Equal(0, macroblock.TokensRead);
                Assert.Equal(0, macroblock.PartitionBytesConsumed);
                Assert.Equal(macroblock.PartitionBytesBefore, macroblock.PartitionBytesAfter);
                Assert.Equal(macroblock.PartitionTokensBefore, macroblock.PartitionTokensAfter);

                for (var b = 0; b < macroblock.Blocks.Length; b++)
                {
                    Assert.Equal(0, macroblock.Blocks[b].TokensRead);
                    Assert.True(macroblock.Blocks[b].ReachedEob);
                }
            }
            else
            {
                Assert.InRange(macroblock.PartitionBytesConsumed, 0, rowBudget);
                Assert.InRange(macroblock.PartitionBytesAfter, macroblock.PartitionBytesBefore, usedPartitionBytesPerPartition[macroblock.PartitionIndex]);
                Assert.InRange(macroblock.PartitionTokensAfter, macroblock.PartitionTokensBefore, partitionTokenTotals[macroblock.PartitionIndex]);
                Assert.InRange(macroblock.TokensRead, macroblock.Blocks.Length, macroblock.Blocks.Length * 16);
            }

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
            Assert.Equal(usedPartitionBytesPerPartition[p], lastBytesAfter[p]);
            if (nonSkippedMacroblocksPerPartition[p] > 0)
            {
                Assert.Equal(partitionTokenTotals[p], lastTokensAfter[p]);
            }
            else
            {
                Assert.Equal(0, lastTokensAfter[p]);
            }
        }
    }
}
