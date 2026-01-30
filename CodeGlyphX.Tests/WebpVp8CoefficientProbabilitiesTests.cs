using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8CoefficientProbabilitiesTests
{
    [Fact]
    public void TryReadFrameHeader_ParsesCoefficientProbabilityTableShape()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 40, height: 28, boolData);

        var success = WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader);

        Assert.True(success);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Probabilities.Length);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Updated.Length);
        Assert.InRange(frameHeader.CoefficientProbabilities.UpdatedCount, 0, 1056);
        Assert.True(frameHeader.CoefficientProbabilities.BytesConsumed >= frameHeader.ControlHeader.BytesConsumed);
    }
}
