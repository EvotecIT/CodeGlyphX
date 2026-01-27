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

        Assert.True(WebpVp8Decoder.TryReadMacroblockTokenScaffold(payload, out var macroblockTokens));
        var sourceMacroblock = macroblockTokens.Macroblocks[0];
        var ySample0 = sourceMacroblock.Blocks[0].BlockPixels.Samples[0];
        var ySample1 = sourceMacroblock.Blocks[1].BlockPixels.Samples[0];
        var uSample0 = sourceMacroblock.Blocks[2].BlockPixels.Samples[0];
        var vSample0 = sourceMacroblock.Blocks[3].BlockPixels.Samples[0];

        var success = WebpVp8Decoder.TryReadMacroblockScaffold(payload, out var macroblock);

        Assert.True(success);
        Assert.Equal(16, macroblock.Width);
        Assert.Equal(16, macroblock.Height);
        Assert.Equal(8, macroblock.ChromaWidth);
        Assert.Equal(8, macroblock.ChromaHeight);
        Assert.Equal(16 * 16, macroblock.YPlane.Length);
        Assert.Equal(8 * 8, macroblock.UPlane.Length);
        Assert.Equal(8 * 8, macroblock.VPlane.Length);
        Assert.InRange(macroblock.BlocksPlacedY, 1, 4);
        Assert.Equal(1, macroblock.BlocksPlacedU);
        Assert.Equal(1, macroblock.BlocksPlacedV);
        Assert.InRange(macroblock.BlocksPlacedTotal, 1, 4);
        Assert.True(macroblock.BlocksAvailable >= macroblock.BlocksPlacedTotal);
        Assert.Equal(ySample0, macroblock.YPlane[0]);
        // Block 1 starts at x=4 in the 8x8 source scaffold; after 2x upscale,
        // it lands at x=8 in the 16x16 macroblock scaffold.
        Assert.Equal(ySample1, macroblock.YPlane[8]);
        Assert.Equal(uSample0, macroblock.UPlane[0]);
        Assert.Equal(vSample0, macroblock.VPlane[0]);

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
