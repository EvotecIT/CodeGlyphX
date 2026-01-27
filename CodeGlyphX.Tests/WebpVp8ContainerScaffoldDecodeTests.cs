using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ContainerScaffoldDecodeTests
{
    [Fact]
    public void ImageReader_Vp8ScaffoldSignature_DecodesViaManagedPath()
    {
        const int expectedWidth = 17;
        const int expectedHeight = 10;
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        WebpVp8TestHelper.ApplyScaffoldSignature(boolData);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(expectedWidth, expectedHeight, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 36 + i;
        }

        var vp8Payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            expectedWidth,
            expectedHeight,
            boolData,
            partitionSizes,
            tokenSeed: 0x53);
        var webp = WebpVp8TestHelper.BuildWebpVp8(vp8Payload);

        var success = ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height);

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
