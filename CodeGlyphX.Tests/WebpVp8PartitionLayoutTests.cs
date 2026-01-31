using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8PartitionLayoutTests
{
    [Fact]
    public void TryReadPartitionLayout_ValidSizes_AreParsedConsistently()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);

        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 48, height: 32, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var explicitSizes = new int[Math.Max(0, dctCount - 1)];
        for (var i = 0; i < explicitSizes.Length; i++)
        {
            explicitSizes[i] = 5 + i;
        }

        const int lastSize = 11;
        var payload = BuildKeyframePayloadWithPartitions(48, 32, boolData, explicitSizes, lastSize);

        var success = WebpVp8Decoder.TryReadPartitionLayout(payload, out var layout);

        Assert.True(success);
        Assert.Equal(3, layout.FirstPartitionOffset);
        Assert.Equal(7 + boolData.Length, layout.FirstPartitionSize);
        Assert.Equal(3 * (dctCount - 1), layout.SizeTableBytes);
        Assert.Equal(layout.FirstPartitionOffset + layout.FirstPartitionSize, layout.SizeTableOffset);
        Assert.Equal(layout.SizeTableOffset + layout.SizeTableBytes, layout.DctDataOffset);

        Assert.Equal(dctCount, layout.DctPartitionSizes.Length);
        for (var i = 0; i < explicitSizes.Length; i++)
        {
            Assert.Equal(explicitSizes[i], layout.DctPartitionSizes[i]);
        }

        Assert.Equal(lastSize, layout.DctPartitionSizes[dctCount - 1]);
        Assert.Equal(Sum(layout.DctPartitionSizes), payload.Length - layout.DctDataOffset);
    }

    private static byte[] BuildKeyframePayloadWithPartitions(
        int width,
        int height,
        byte[] boolData,
        int[] explicitSizes,
        int lastSize)
    {
        var firstPartition = WebpVp8TestHelper.BuildKeyframePayload(width, height, boolData);

        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartition, out var frameHeader));
        var dctCount = frameHeader.DctPartitionCount;
        Assert.Equal(Math.Max(0, dctCount - 1), explicitSizes.Length);

        var sizeTableBytes = 3 * (dctCount - 1);
        var dctDataLength = lastSize + Sum(explicitSizes);
        var payload = new byte[firstPartition.Length + sizeTableBytes + dctDataLength];
        firstPartition.CopyTo(payload, 0);

        var sizeTableOffset = firstPartition.Length;
        for (var i = 0; i < explicitSizes.Length; i++)
        {
            WriteU24LE(payload, sizeTableOffset + (i * 3), explicitSizes[i]);
        }

        return payload;
    }

    private static void WriteU24LE(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static int Sum(int[] values)
    {
        var sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum;
    }
}
