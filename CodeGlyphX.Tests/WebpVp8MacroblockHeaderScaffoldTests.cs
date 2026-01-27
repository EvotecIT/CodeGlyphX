using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8MacroblockHeaderScaffoldTests
{
    [Fact]
    public void TryReadMacroblockHeaderScaffold_ValidPayload_UsesMacroblockGridDimensions()
    {
        const int expectedWidth = 33;
        const int expectedHeight = 17;
        const int expectedCols = 3;
        const int expectedRows = 2;
        const int expectedMacroblocks = expectedCols * expectedRows;

        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        WebpVp8TestHelper.ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(expectedWidth, expectedHeight, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 40 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            expectedWidth,
            expectedHeight,
            boolData,
            partitionSizes,
            tokenSeed: 0x61);

        var success = WebpVp8Decoder.TryReadMacroblockHeaderScaffold(payload, out var headers);

        Assert.True(success);
        Assert.Equal(expectedCols, headers.MacroblockCols);
        Assert.Equal(expectedRows, headers.MacroblockRows);
        Assert.Equal(expectedMacroblocks, headers.Macroblocks.Length);
        Assert.InRange(headers.BoolBytesConsumed, 2, boolData.Length);
        Assert.InRange(headers.SkipCount, 0, headers.Macroblocks.Length);

        var segmentSum = 0;
        for (var i = 0; i < headers.SegmentCounts.Length; i++)
        {
            Assert.InRange(headers.SegmentCounts[i], 0, headers.Macroblocks.Length);
            segmentSum += headers.SegmentCounts[i];
        }

        Assert.Equal(headers.Macroblocks.Length, segmentSum);

        for (var i = 0; i < headers.Macroblocks.Length; i++)
        {
            var macroblock = headers.Macroblocks[i];
            Assert.Equal(i, macroblock.Index);
            Assert.InRange(macroblock.X, 0, headers.MacroblockCols - 1);
            Assert.InRange(macroblock.Y, 0, headers.MacroblockRows - 1);
            Assert.InRange(macroblock.SegmentId, 0, headers.SegmentCounts.Length - 1);
            Assert.InRange(macroblock.YMode, 0, 4);
            Assert.InRange(macroblock.UvMode, 0, 3);

            if (macroblock.Is4x4)
            {
                Assert.Equal(16, macroblock.SubblockModes.Length);
                for (var b = 0; b < macroblock.SubblockModes.Length; b++)
                {
                    Assert.InRange(macroblock.SubblockModes[b], 0, 9);
                }
            }
            else
            {
                Assert.Empty(macroblock.SubblockModes);
            }
        }
    }
}
