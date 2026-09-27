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
    [Theory]
    [InlineData(10, -20, 20, 0, false, 10)]
    [InlineData(50, 20, 10, 0, false, 63)]
    [InlineData(0, -20, 30, 0, true, 10)]
    [InlineData(60, 0, 0, 20, false, 63)]
    public void CombinedFilterDeltasMatchEquivalentBoundedLevel(
        int level, int segmentDelta, int referenceDelta, int modeDelta, bool absolute, int expectedLevel) {
        byte[] Filter(int baseLevel, bool segmented, bool useDeltas) {
            var yPlane = new byte[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                yPlane[y * 16 + x] = (byte)(x < 8 ? 100 : expectedLevel == 63 ? 185 : 120);
            var chroma = new byte[64];
            Array.Fill(chroma, (byte)128);
            var macroblock = new WebpVp8MacroblockHeaderScaffold(0, 0, 0, 0, false, 4, 0, true, new int[16]);
            var filter = new WebpVp8LoopFilter(0, baseLevel, 0, useDeltas, false,
                new[] { referenceDelta, 0, 0, 0 }, new bool[4], new[] { modeDelta, 0, 0, 0 }, new bool[4]);
            var segments = new WebpVp8Segmentation(segmented, false, false, absolute,
                new int[4], new[] { segmentDelta, 0, 0, 0 }, new int[3]);
            WebpVp8Decoder.ApplyLoopFilterForTest(filter, segments, new[] { macroblock }, new[] { true },
                16, 16, yPlane, (byte[])chroma.Clone(), chroma, 8, 8, true);
            return yPlane;
        }
        Assert.Equal(Filter(expectedLevel, false, false), Filter(level, true, true));
    }

}
