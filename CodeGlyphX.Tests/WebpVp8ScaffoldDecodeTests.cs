using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8ScaffoldDecodeTests
{
    [Fact]
    public void TryDecode_ScaffoldSignature_SucceedsWithRgbaOutput()
    {
        const int expectedWidth = 13;
        const int expectedHeight = 9;
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        WebpVp8TestHelper.ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(expectedWidth, expectedHeight, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 32 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            expectedWidth,
            expectedHeight,
            boolData,
            partitionSizes,
            tokenSeed: 0x59);

        var success = WebpVp8Decoder.TryDecode(payload, out var rgba, out var width, out var height);

        Assert.True(success);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expectedWidth * expectedHeight * 4, rgba.Length);

        for (var i = 3; i < rgba.Length; i += 4)
        {
            Assert.Equal(255, rgba[i]);
        }
    }
}
