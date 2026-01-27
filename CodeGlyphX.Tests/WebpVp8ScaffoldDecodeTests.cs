using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ScaffoldDecodeTests
{
    [Fact]
    public void TryDecode_ScaffoldSignature_SucceedsWithRgbaOutput()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        WebpVp8TestHelper.ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 8, height: 8, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 32 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            8,
            8,
            boolData,
            partitionSizes,
            tokenSeed: 0x59);

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
}
