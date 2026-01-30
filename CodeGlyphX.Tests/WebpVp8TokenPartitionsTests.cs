using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8TokenPartitionsTests
{
    [Fact]
    public void TryReadTokenPartitions_ValidLayout_ProducesConsistentOffsets()
    {
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 64, height: 40, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 6 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(64, 40, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryReadTokenPartitions(payload, out var partitions);

        Assert.True(success);
        Assert.Equal(dctCount, partitions.Partitions.Length);
        Assert.Equal(Sum(partitionSizes), partitions.TotalBytes);

        var expectedOffset = partitions.DataOffset;
        for (var i = 0; i < partitions.Partitions.Length; i++)
        {
            var info = partitions.Partitions[i];
            Assert.Equal(expectedOffset, info.Offset);
            Assert.Equal(partitionSizes[i], info.Size);
            Assert.InRange(info.HeaderBytesConsumed, 2, info.Size);
            expectedOffset += info.Size;
        }
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
