using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8RgbaScaffoldTests
{
    [Fact]
    public void TryReadMacroblockRgbaScaffold_ValidPayload_ProducesRgbaBuffer()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 100, height: 68, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 28 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(100, 68, boolData, partitionSizes);

        Assert.True(WebpVp8Decoder.TryReadMacroblockScaffold(payload, out var macroblock));
        var expected = ConvertYuvToRgb(macroblock.YPlane[0], macroblock.UPlane[0], macroblock.VPlane[0]);

        var success = WebpVp8Decoder.TryReadMacroblockRgbaScaffold(payload, out var rgba);

        Assert.True(success);
        Assert.Equal(16, rgba.Width);
        Assert.Equal(16, rgba.Height);
        Assert.Equal(16 * 16 * 4, rgba.Rgba.Length);
        Assert.InRange(rgba.BlocksPlaced, 1, 4);

        Assert.Equal(expected.R, rgba.Rgba[0]);
        Assert.Equal(expected.G, rgba.Rgba[1]);
        Assert.Equal(expected.B, rgba.Rgba[2]);
        Assert.Equal(255, rgba.Rgba[3]);
    }

    private static (byte R, byte G, byte B) ConvertYuvToRgb(byte y, byte u, byte v)
    {
        var yy = y;
        var uu = u - 128;
        var vv = v - 128;

        var r = yy + (int)(1.402 * vv);
        var g = yy - (int)(0.344136 * uu) - (int)(0.714136 * vv);
        var b = yy + (int)(1.772 * uu);

        return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    private static byte ClampToByte(int value)
    {
        if (value < byte.MinValue) return byte.MinValue;
        if (value > byte.MaxValue) return byte.MaxValue;
        return (byte)value;
    }
}
