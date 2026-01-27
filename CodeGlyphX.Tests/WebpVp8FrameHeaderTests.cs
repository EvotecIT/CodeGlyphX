using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8FrameHeaderTests
{
    [Fact]
    public void TryReadFrameHeader_KeyframeBoolData_SucceedsWithValidRanges()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 32, height: 24, boolData);

        var success = WebpVp8Decoder.TryReadFrameHeader(payload, out var frameHeader);

        Assert.True(success);
        Assert.InRange(frameHeader.ControlHeader.ColorSpace, 0, 1);
        Assert.InRange(frameHeader.ControlHeader.ClampType, 0, 1);
        Assert.Contains(frameHeader.DctPartitionCount, new[] { 1, 2, 4, 8 });
        Assert.InRange(frameHeader.LoopFilter.Level, 0, 63);
        Assert.InRange(frameHeader.LoopFilter.Sharpness, 0, 7);
        Assert.InRange(frameHeader.Quantization.BaseQIndex, 0, 127);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Probabilities.Length);
        Assert.Equal(1056, frameHeader.CoefficientProbabilities.Updated.Length);
        Assert.InRange(frameHeader.CoefficientProbabilities.UpdatedCount, 0, 1056);

        if (frameHeader.NoCoefficientSkip)
        {
            Assert.InRange(frameHeader.SkipProbability, 0, 255);
        }
    }
}
