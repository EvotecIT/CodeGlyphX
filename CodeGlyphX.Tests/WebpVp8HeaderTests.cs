using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8HeaderTests {
    [Fact]
    public void Vp8_KeyframeHeader_ParsesDimensions() {
        var payload = BuildKeyframePayload(width: 5, height: 3);

        Assert.True(WebpVp8Decoder.TryReadHeader(payload, out var header));
        Assert.Equal(5, header.Width);
        Assert.Equal(3, header.Height);
        Assert.True(header.ShowFrame);
        Assert.Equal(7, header.PartitionSize);
        Assert.Equal(0, header.HorizontalScale);
        Assert.Equal(0, header.VerticalScale);
        Assert.Equal(80, header.BitsConsumed);
    }

    [Fact]
    public void Vp8_NonKeyframe_IsRejected() {
        var payload = BuildKeyframePayload(width: 5, height: 3);

        // Flip the frame-type bit (bit 0) to mark as an interframe.
        payload[0] = (byte)(payload[0] | 0x01);

        Assert.False(WebpVp8Decoder.TryReadHeader(payload, out _));
    }

    [Fact]
    public void Vp8_FirstPartition_IsExtractedUsingDeclaredSize() {
        var payload = BuildKeyframePayload(width: 5, height: 3, boolData: new byte[] { 0xAA, 0xBB });

        Assert.True(WebpVp8Decoder.TryGetFirstPartition(payload, out var firstPartition));
        Assert.Equal(9, firstPartition.Length);
        Assert.Equal(0x9D, firstPartition[0]);
        Assert.Equal(0x2A, firstPartition[2]);
    }

    [Fact]
    public void Vp8_BoolCodedData_IsSlicedAfterKeyframeHeader() {
        var boolData = new byte[] { 0x11, 0x22, 0x33 };
        var payload = BuildKeyframePayload(width: 6, height: 4, boolData);

        Assert.True(WebpVp8Decoder.TryGetBoolCodedData(payload, out var sliced));
        Assert.Equal(boolData.Length, sliced.Length);
        Assert.Equal(boolData[0], sliced[0]);
        Assert.Equal(boolData[1], sliced[1]);
        Assert.Equal(boolData[2], sliced[2]);
    }

    private static byte[] BuildKeyframePayload(int width, int height, byte[]? boolData = null) {
        var extra = boolData?.Length ?? 0;
        var partitionSize = 7 + extra;
        var payload = new byte[3 + partitionSize];

        // Frame tag (3 bytes, little-endian).
        var frameTag = (partitionSize << 5) | (1 << 4); // show_frame=1
        payload[0] = (byte)(frameTag & 0xFF);
        payload[1] = (byte)((frameTag >> 8) & 0xFF);
        payload[2] = (byte)((frameTag >> 16) & 0xFF);

        // Start code.
        payload[3] = 0x9D;
        payload[4] = 0x01;
        payload[5] = 0x2A;

        // Dimensions (14 bits + 2 scale bits).
        payload[6] = (byte)(width & 0xFF);
        payload[7] = (byte)((width >> 8) & 0x3F);
        payload[8] = (byte)(height & 0xFF);
        payload[9] = (byte)((height >> 8) & 0x3F);

        if (extra > 0) {
            boolData!.CopyTo(payload, 10);
        }

        return payload;
    }
}
