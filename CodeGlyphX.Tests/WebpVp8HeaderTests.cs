using System;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8HeaderTests {
    [Fact]
    public void Vp8_KeyframeHeader_ParsesDimensions() {
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 5, height: 3, boolData: Array.Empty<byte>());

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
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 5, height: 3, boolData: Array.Empty<byte>());

        // Flip the frame-type bit (bit 0) to mark as an interframe.
        payload[0] = (byte)(payload[0] | 0x01);

        Assert.False(WebpVp8Decoder.TryReadHeader(payload, out _));
    }

    [Fact]
    public void Vp8_FirstPartition_IsExtractedUsingDeclaredSize() {
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 5, height: 3, boolData: new byte[] { 0xAA, 0xBB });

        Assert.True(WebpVp8Decoder.TryGetFirstPartition(payload, out var firstPartition));
        Assert.Equal(9, firstPartition.Length);
        Assert.Equal(0x9D, firstPartition[0]);
        Assert.Equal(0x2A, firstPartition[2]);
    }

    [Fact]
    public void Vp8_BoolCodedData_IsSlicedAfterKeyframeHeader() {
        var boolData = new byte[] { 0x11, 0x22, 0x33 };
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 6, height: 4, boolData);

        Assert.True(WebpVp8Decoder.TryGetBoolCodedData(payload, out var sliced));
        Assert.Equal(boolData.Length, sliced.Length);
        Assert.Equal(boolData[0], sliced[0]);
        Assert.Equal(boolData[1], sliced[1]);
        Assert.Equal(boolData[2], sliced[2]);
    }
}
