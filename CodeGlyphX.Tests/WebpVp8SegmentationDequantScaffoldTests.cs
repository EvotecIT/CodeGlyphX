using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8SegmentationDequantScaffoldTests
{
    private const int CoefficientProbabilityCount = 1056;
    private const int BlockTypeY = 1;

    [Fact]
    public void ComputeSegmentBaseQIndex_RelativeDeltas_AdjustsFromBaseQIndex()
    {
        var frameHeader = CreateFrameHeader(
            baseQIndex: 20,
            segmentationEnabled: true,
            segmentationUpdateData: true,
            absoluteDeltas: false,
            quantizerDeltas: new[] { 0, 10, -5, 0 });

        Assert.Equal(20, WebpVp8Decoder.ComputeSegmentBaseQIndex(frameHeader, segmentId: 0));
        Assert.Equal(30, WebpVp8Decoder.ComputeSegmentBaseQIndex(frameHeader, segmentId: 1));
        Assert.Equal(15, WebpVp8Decoder.ComputeSegmentBaseQIndex(frameHeader, segmentId: 2));
    }

    [Fact]
    public void ComputeSegmentBaseQIndex_AbsoluteDeltas_UsesSegmentValueDirectly()
    {
        var frameHeader = CreateFrameHeader(
            baseQIndex: 40,
            segmentationEnabled: true,
            segmentationUpdateData: true,
            absoluteDeltas: true,
            quantizerDeltas: new[] { 0, 5, 0, 0 });

        Assert.Equal(0, WebpVp8Decoder.ComputeSegmentBaseQIndex(frameHeader, segmentId: 0));
        Assert.Equal(5, WebpVp8Decoder.ComputeSegmentBaseQIndex(frameHeader, segmentId: 1));
    }

    [Fact]
    public void ComputeScaffoldDequantFactorForSegment_UsesSegmentAdjustedQIndex()
    {
        var frameHeader = CreateFrameHeader(
            baseQIndex: 20,
            segmentationEnabled: true,
            segmentationUpdateData: true,
            absoluteDeltas: false,
            quantizerDeltas: new[] { 0, 10, 0, 0 });

        var dequantSegment0 = WebpVp8Decoder.ComputeScaffoldDequantFactorForSegment(BlockTypeY, frameHeader, segmentId: 0);
        var dequantSegment1 = WebpVp8Decoder.ComputeScaffoldDequantFactorForSegment(BlockTypeY, frameHeader, segmentId: 1);

        // Segment 1 has a +10 relative delta, so its dequant factor should be higher.
        Assert.True(dequantSegment1 > dequantSegment0);
        Assert.Equal(25, dequantSegment0);
        Assert.Equal(35, dequantSegment1);
    }

    private static WebpVp8FrameHeader CreateFrameHeader(
        int baseQIndex,
        bool segmentationEnabled,
        bool segmentationUpdateData,
        bool absoluteDeltas,
        int[] quantizerDeltas)
    {
        var control = new WebpVp8ControlHeader(colorSpace: 0, clampType: 0, bytesConsumed: 2);
        var segmentation = new WebpVp8Segmentation(
            segmentationEnabled,
            updateMap: false,
            segmentationUpdateData,
            absoluteDeltas,
            quantizerDeltas,
            filterDeltas: new int[4],
            segmentProbabilities: new[] { -1, -1, -1 });
        var loopFilter = new WebpVp8LoopFilter(
            filterType: 0,
            level: 0,
            sharpness: 0,
            deltaEnabled: false,
            deltaUpdate: false,
            refDeltas: new int[4],
            refDeltasUpdated: new bool[4],
            modeDeltas: new int[4],
            modeDeltasUpdated: new bool[4]);
        var quantization = new WebpVp8Quantization(baseQIndex, new int[5], new bool[5]);
        var coefficients = new WebpVp8CoefficientProbabilities(
            new int[CoefficientProbabilityCount],
            new bool[CoefficientProbabilityCount],
            updatedCount: 0,
            bytesConsumed: 0);

        return new WebpVp8FrameHeader(
            control,
            segmentation,
            loopFilter,
            quantization,
            coefficients,
            dctPartitionCount: 1,
            refreshEntropyProbs: false,
            noCoefficientSkip: false,
            skipProbability: 0,
            bytesConsumed: 0);
    }
}
