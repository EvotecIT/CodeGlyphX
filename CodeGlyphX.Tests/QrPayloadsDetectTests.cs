using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPayloadsDetectTests {
    [Fact]
    public void Detect_BareEmail_UsesMailto() {
        var payload = QrPayloads.Detect("user@example.com");
        Assert.StartsWith("mailto:", payload.Text);
    }

    [Fact]
    public void Detect_BarePhone_UsesTel() {
        var payload = QrPayloads.Detect("+1 555 123 4567");
        Assert.StartsWith("tel:", payload.Text);
    }

    [Fact]
    public void Detect_BareUrl_AddsScheme() {
        var payload = QrPayloads.Detect("example.com");
        Assert.StartsWith("http://", payload.Text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Detect_FormattedPayload_Preserves() {
        var raw = "WIFI:T:WPA;S:Lab;P:Secret;;";
        var payload = QrPayloads.Detect(raw);
        Assert.Equal(raw, payload.Text);
    }

    [Fact]
    public void Detect_Whitespace_PrefersText() {
        var raw = "user@example.com hello";
        var payload = QrPayloads.Detect(raw);
        Assert.Equal(raw, payload.Text);
    }
}
