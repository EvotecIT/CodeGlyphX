using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8MacroblockScaffoldTests
{
    [Fact]
    public void TryReadMacroblockScaffold_ValidPayload_TilesBlockPixelsIntoYPlane()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 96, height: 64, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 28 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(96, 64, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryReadMacroblockScaffold(payload, out var macroblock);

        Assert.True(success);
        Assert.Equal(16, macroblock.Width);
        Assert.Equal(16, macroblock.Height);
        Assert.Equal(8, macroblock.ChromaWidth);
        Assert.Equal(8, macroblock.ChromaHeight);
        Assert.Equal(16 * 16, macroblock.YPlane.Length);
        Assert.Equal(8 * 8, macroblock.UPlane.Length);
        Assert.Equal(8 * 8, macroblock.VPlane.Length);
        Assert.InRange(macroblock.BlocksPlacedY, 1, 16);
        Assert.InRange(macroblock.BlocksPlacedU, 1, 4);
        Assert.InRange(macroblock.BlocksPlacedV, 1, 4);
        Assert.InRange(macroblock.BlocksPlacedTotal, 1, 24);
        Assert.True(macroblock.BlocksAvailable >= macroblock.BlocksPlacedTotal);
        for (var i = 0; i < macroblock.YPlane.Length; i++)
        {
            Assert.InRange(macroblock.YPlane[i], 0, 255);
        }

        for (var i = 0; i < macroblock.UPlane.Length; i++)
        {
            Assert.InRange(macroblock.UPlane[i], 0, 255);
            Assert.InRange(macroblock.VPlane[i], 0, 255);
        }
    }

}
