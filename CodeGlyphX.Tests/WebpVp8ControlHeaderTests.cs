using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8ControlHeaderTests
{
    [Fact]
    public void TryReadControlHeader_KeyframeBoolData_SucceedsAndConsumesBytes()
    {
        var boolData = new byte[] { 0x9A, 0xBC, 0xDE, 0xF0 };
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width: 16, height: 16, boolData);

        var success = WebpVp8Decoder.TryReadControlHeader(payload, out var control);

        Assert.True(success);
        Assert.InRange(control.ColorSpace, 0, 1);
        Assert.InRange(control.ClampType, 0, 1);
        Assert.InRange(control.BytesConsumed, 2, boolData.Length);
    }

    [Fact]
    public void BoolDecoder_TooShortData_Fails()
    {
        var decoder = new WebpVp8BoolDecoder(new byte[] { 0x00 });

        var success = decoder.TryReadBool(128, out _);

        Assert.False(success);
    }
}
