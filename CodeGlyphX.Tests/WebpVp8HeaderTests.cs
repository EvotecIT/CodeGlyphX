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
        Assert.Equal(80, header.BitsConsumed);
    }

    [Fact]
    public void Vp8_NonKeyframe_IsRejected() {
        var payload = BuildKeyframePayload(width: 5, height: 3);

        // Flip the frame-type bit (bit 0) to mark as an interframe.
        payload[0] = (byte)(payload[0] | 0x01);

        Assert.False(WebpVp8Decoder.TryReadHeader(payload, out _));
    }

    private static byte[] BuildKeyframePayload(int width, int height) {
        var payload = new byte[10];

        // Frame tag (3 bytes, little-endian):
        // frame_type=0 (keyframe), version=0, show_frame=1, partition_size=0
        // => value has bit 4 set.
        payload[0] = 0x10;
        payload[1] = 0x00;
        payload[2] = 0x00;

        // Start code.
        payload[3] = 0x9D;
        payload[4] = 0x01;
        payload[5] = 0x2A;

        // Dimensions (14 bits + 2 scale bits).
        payload[6] = (byte)(width & 0xFF);
        payload[7] = (byte)((width >> 8) & 0x3F);
        payload[8] = (byte)(height & 0xFF);
        payload[9] = (byte)((height >> 8) & 0x3F);

        return payload;
    }
}

