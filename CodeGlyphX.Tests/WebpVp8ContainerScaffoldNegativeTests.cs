using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ContainerScaffoldNegativeTests
{
    [Fact]
    public void WebpManagedDecoder_Vp8WithoutSignature_ReturnsFalse()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 8, height: 8, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 36 + i;
        }

        var vp8Payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
            8,
            8,
            boolData,
            partitionSizes,
            tokenSeed: 0x53);
        var webp = WebpVp8TestHelper.BuildWebpVp8(vp8Payload);

        var success = WebpManagedDecoder.TryDecodeRgba32(webp, out var rgba, out var width, out var height);

        Assert.True(success);
        Assert.Equal(8, width);
        Assert.Equal(8, height);
        Assert.Equal(8 * 8 * 4, rgba.Length);
    }
}
