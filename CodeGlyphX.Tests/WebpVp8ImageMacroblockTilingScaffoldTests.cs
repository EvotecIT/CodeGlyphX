using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ImageMacroblockTilingScaffoldTests
{
    [Fact]
    public void ImageReader_Vp8ScaffoldSignature_UsesMultipleMacroblocksWhenAvailable()
    {
        const int expectedWidth = 33;
        const int expectedHeight = 33;
        const int macroblockPixelSize = 16;
        const int maxSeed = 512;

        var foundDistinctMacroblocks = false;

        for (var seed = 1; seed <= maxSeed; seed++)
        {
            var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096, seed: seed);
            WebpVp8TestHelper.ApplyScaffoldSignature(boolData);

            var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(expectedWidth, expectedHeight, boolData);
            if (!WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader))
            {
                continue;
            }

            var dctCount = frameHeader.DctPartitionCount;
            var partitionSizes = new int[dctCount];
            for (var i = 0; i < partitionSizes.Length; i++)
            {
                partitionSizes[i] = 52 + i;
            }

            var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(
                expectedWidth,
                expectedHeight,
                boolData,
                partitionSizes,
                tokenSeed: 0x73);

            if (!WebpVp8Decoder.TryReadMacroblockTokenScaffold(payload, out var macroblockTokens))
            {
                continue;
            }

            var tileCols = (expectedWidth + macroblockPixelSize - 1) / macroblockPixelSize;
            var tileRows = (expectedHeight + macroblockPixelSize - 1) / macroblockPixelSize;
            Assert.Equal(tileCols, macroblockTokens.MacroblockCols);
            Assert.Equal(tileRows, macroblockTokens.MacroblockRows);

            if (macroblockTokens.MacroblockCols < 3 || macroblockTokens.MacroblockRows < 1)
            {
                continue;
            }

            var macroblock0 = WebpVp8Decoder.BuildMacroblockScaffold(
                macroblockTokens.Macroblocks[0].Blocks,
                macroblockTokens.TotalBlocksAssigned);
            var macroblock1 = WebpVp8Decoder.BuildMacroblockScaffold(
                macroblockTokens.Macroblocks[1].Blocks,
                macroblockTokens.TotalBlocksAssigned);
            var macroblock2 = WebpVp8Decoder.BuildMacroblockScaffold(
                macroblockTokens.Macroblocks[2].Blocks,
                macroblockTokens.TotalBlocksAssigned);
            var rgba0 = WebpVp8Decoder.ConvertMacroblockScaffoldToRgba(macroblock0);
            var rgba1 = WebpVp8Decoder.ConvertMacroblockScaffoldToRgba(macroblock1);
            var rgba2 = WebpVp8Decoder.ConvertMacroblockScaffoldToRgba(macroblock2);
            var rgba0Upscaled = WebpVp8Decoder.UpscaleRgbaNearest(rgba0, macroblock0.Width, macroblock0.Height, macroblockPixelSize, macroblockPixelSize);
            var rgba1Upscaled = WebpVp8Decoder.UpscaleRgbaNearest(rgba1, macroblock1.Width, macroblock1.Height, macroblockPixelSize, macroblockPixelSize);
            var rgba2Upscaled = WebpVp8Decoder.UpscaleRgbaNearest(rgba2, macroblock2.Width, macroblock2.Height, macroblockPixelSize, macroblockPixelSize);

            if (!PixelsDiffer(rgba0Upscaled, rgba1Upscaled) || !PixelsDiffer(rgba0Upscaled, rgba2Upscaled))
            {
                continue;
            }

            var webp = WebpVp8TestHelper.BuildWebpVp8(payload);
            var success = ImageReader.TryDecodeRgba32(webp, out var rgba, out var width, out var height);

            Assert.True(success);
            Assert.Equal(expectedWidth, width);
            Assert.Equal(expectedHeight, height);

            var pixel00 = ReadPixel(rgba, width, x: 0, y: 0);
            var pixel10 = ReadPixel(rgba, width, x: macroblockPixelSize, y: 0);
            var pixel20 = ReadPixel(rgba, width, x: macroblockPixelSize * 2, y: 0);
            var expected00 = ReadPixel(rgba0Upscaled, macroblockPixelSize, x: 0, y: 0);
            var expected10 = ReadPixel(rgba1Upscaled, macroblockPixelSize, x: 0, y: 0);
            var expected20 = ReadPixel(rgba2Upscaled, macroblockPixelSize, x: 0, y: 0);

            Assert.Equal(expected00, pixel00);
            Assert.Equal(expected10, pixel10);
            Assert.Equal(expected20, pixel20);

            foundDistinctMacroblocks = true;
            break;
        }

        Assert.True(foundDistinctMacroblocks, $"Unable to find distinct macroblock seeds in 1..{maxSeed}.");
    }

    private static bool PixelsDiffer(byte[] a, byte[] b)
    {
        if (a.Length < 4 || b.Length < 4)
        {
            return false;
        }

        for (var i = 0; i < 4; i++)
        {
            if (a[i] != b[i])
            {
                return true;
            }
        }

        return false;
    }

    private static (byte R, byte G, byte B, byte A) ReadPixel(byte[] rgba, int width, int x, int y)
    {
        var index = ((y * width) + x) * 4;
        return (rgba[index + 0], rgba[index + 1], rgba[index + 2], rgba[index + 3]);
    }
}
