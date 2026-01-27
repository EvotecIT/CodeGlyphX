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

        var success = WebpVp8Decoder.TryReadMacroblockTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(expectedCols, scaffold.MacroblockCols);
        Assert.Equal(expectedRows, scaffold.MacroblockRows);
        Assert.Equal(dctCount, scaffold.PartitionCount);
        Assert.Equal(expectedMacroblocks, scaffold.Macroblocks.Length);
        Assert.Equal(expectedMacroblocks * 4, scaffold.TotalBlocksAssigned);
        Assert.InRange(scaffold.TotalTokensRead, scaffold.TotalBlocksAssigned, scaffold.TotalBlocksAssigned * 16);
        Assert.True(scaffold.TotalBytesConsumed > 0);

        for (var i = 0; i < scaffold.Macroblocks.Length; i++)
        {
            var macroblock = scaffold.Macroblocks[i];
            var expectedPartition = macroblock.Header.Y % dctCount;
            Assert.Equal(expectedPartition, macroblock.PartitionIndex);
            Assert.InRange(macroblock.PartitionBytesConsumed, 2, partitionSizes[macroblock.PartitionIndex]);
            Assert.Equal(4, macroblock.Blocks.Length);
            Assert.InRange(macroblock.TokensRead, macroblock.Blocks.Length, macroblock.Blocks.Length * 16);

            for (var b = 0; b < macroblock.Blocks.Length; b++)
            {
                var block = macroblock.Blocks[b];
                Assert.Equal(b, block.BlockIndex);
                Assert.InRange(block.BlockType, 0, 3);
            }
        }
    }
}

