using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8TokenScaffoldTests
{
    [Fact]
    public void TryReadTokenScaffold_ValidPayload_ReadsScaffoldTokensPerPartition()
    {
        const int expectedTokensPerPartition = 8;
        var boolData = WebpVp8TestHelper.CreateBoolData(length: 4096);
        var firstPartitionOnly = WebpVp8TestHelper.BuildKeyframePayload(width: 72, height: 48, boolData);
        Assert.True(WebpVp8Decoder.TryReadFrameHeader(firstPartitionOnly, out var frameHeader));

        var dctCount = frameHeader.DctPartitionCount;
        var partitionSizes = new int[dctCount];
        for (var i = 0; i < partitionSizes.Length; i++)
        {
            partitionSizes[i] = 8 + i;
        }

        var payload = WebpVp8TestHelper.BuildKeyframePayloadWithPartitionsAndTokens(72, 48, boolData, partitionSizes);

        var success = WebpVp8Decoder.TryReadTokenScaffold(payload, out var scaffold);

        Assert.True(success);
        Assert.Equal(dctCount, scaffold.Partitions.Length);
        Assert.Equal(dctCount * expectedTokensPerPartition, scaffold.TotalTokensRead);

        for (var i = 0; i < scaffold.Partitions.Length; i++)
        {
            var partition = scaffold.Partitions[i];
            Assert.Equal(partitionSizes[i], partition.Size);
            Assert.Equal(expectedTokensPerPartition, partition.TokensRead);
            Assert.Equal(expectedTokensPerPartition, partition.Tokens.Length);
            Assert.Equal(expectedTokensPerPartition, partition.PrevContexts.Length);
            Assert.Equal(expectedTokensPerPartition, partition.TokenInfos.Length);
            Assert.InRange(partition.BytesConsumed, 2, partition.Size);

            for (var t = 0; t < partition.PrevContexts.Length; t++)
            {
                Assert.InRange(partition.PrevContexts[t], 0, 2);
                Assert.InRange(partition.Tokens[t], 0, 11);

                var info = partition.TokenInfos[t];
                Assert.Equal(partition.Tokens[t], info.TokenCode);
                Assert.InRange(info.Band, 0, 7);
                Assert.InRange(info.PrevContextBefore, 0, 2);
                Assert.InRange(info.PrevContextAfter, 0, 2);
                Assert.Equal(info.TokenCode != 0, info.HasMore);
                Assert.Equal(info.TokenCode >= 2, info.IsNonZero);
            }
        }
    }
}
