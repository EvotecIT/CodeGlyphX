using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8LoopFilterTests
{
    [Fact]
    public void LoopFilter_ModifiesExpectedEdgePixels()
    {
        const int width = 16;
        const int height = 16;
        const int chromaWidth = 8;
        const int chromaHeight = 8;

        var yPlane = new byte[width * height];
        var uPlane = new byte[chromaWidth * chromaHeight];
        var vPlane = new byte[chromaWidth * chromaHeight];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width) + x;
                yPlane[index] = (byte)(x < 8 ? 100 : 140);
            }
        }

        for (var i = 0; i < uPlane.Length; i++)
        {
            uPlane[i] = 128;
            vPlane[i] = 128;
        }

        var yBefore = (byte[])yPlane.Clone();

        var macroblocks = new[]
        {
            new WebpVp8MacroblockHeaderScaffold(
                index: 0,
                x: 0,
                y: 0,
                segmentId: 0,
                skipCoefficients: false,
                yMode: 0,
                uvMode: 0,
                is4x4: false,
                subblockModes: Array.Empty<int>())
        };

        var macroblockHasCoefficients = new[] { true };

        var loopFilter = new WebpVp8LoopFilter(
            filterType: 0,
            level: 63,
            sharpness: 0,
            deltaEnabled: false,
            deltaUpdate: false,
            refDeltas: new int[4],
            refDeltasUpdated: new bool[4],
            modeDeltas: new int[4],
            modeDeltasUpdated: new bool[4]);

        var segmentation = new WebpVp8Segmentation(
            enabled: false,
            updateMap: false,
            updateData: false,
            absoluteDeltas: false,
            quantizerDeltas: new int[4],
            filterDeltas: new int[4],
            segmentProbabilities: new int[3]);

        WebpVp8Decoder.ApplyLoopFilterForTest(
            loopFilter,
            segmentation,
            macroblocks,
            macroblockHasCoefficients,
            width,
            height,
            yPlane,
            uPlane,
            vPlane,
            chromaWidth,
            chromaHeight,
            isKeyframe: true);

        var changed = false;
        for (var y = 0; y < height; y++)
        {
            var index = (y * width) + 7;
            if (yPlane[index] != yBefore[index] || yPlane[index + 1] != yBefore[index + 1])
            {
                changed = true;
                break;
            }
        }

        Assert.True(changed, "Expected loop filter to modify edge pixels near the 8x8 boundary.");
    }
}
