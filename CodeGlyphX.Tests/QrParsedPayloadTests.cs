using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrParsedPayloadTests {
    [Fact]
    public void Parse_Wifi() {
        var raw = QrPayload.Wifi("Lab", "secret", "WPA", hidden: true);
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Wifi, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Wifi>(out var wifi));
        Assert.Equal("Lab", wifi.Ssid);
        Assert.Equal("WPA", wifi.AuthType);
        Assert.True(wifi.Hidden);
    }

    [Fact]
    public void Parse_Email() {
        var raw = QrPayload.Email("hello@example.com", subject: "Hello", body: "Hi");
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Email, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Email>(out var email));
        Assert.Equal("hello@example.com", email.Address);
        Assert.Equal("Hello", email.Subject);
        Assert.Equal("Hi", email.Body);
    }

    [Fact]
    public void Parse_Geo() {
        var raw = QrPayload.Geo(52.1, 21.0);
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Geo, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Geo>(out var geo));
        Assert.Equal(52.1, geo.Latitude, 3);
        Assert.Equal(21.0, geo.Longitude, 3);
    }

    [Fact]
    public void Parse_OtpAuth() {
        var uri = OtpAuthTotp.Create("CodeGlyphX", "alice@example.com", OtpAuthSecret.FromBase32("JBSWY3DPEHPK3PXP"));
        Assert.True(QrPayloadParser.TryParse(uri, out var parsed));
        Assert.Equal(QrPayloadType.Otp, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Otp>(out var otp));
        Assert.Equal("CodeGlyphX", otp.Payload.Issuer);
        Assert.Equal("alice@example.com", otp.Payload.Account);
    }

    [Fact]
    public void Parse_Smsto() {
        var raw = "SMSTO:+14155550198:Hello";
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Sms, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Sms>(out var sms));
        Assert.Equal("+14155550198", sms.Number);
        Assert.Equal("Hello", sms.Body);
    }

    [Fact]
    public void Parse_Bookmark() {
        var raw = QrPayloads.Bookmark("example.com", "Example").Text;
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Bookmark, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Bookmark>(out var bookmark));
        Assert.Equal("http://example.com", bookmark.Url);
        Assert.Equal("Example", bookmark.Title);
    }

    [Fact]
    public void Parse_Strict_InvalidEmail_FailsValidation() {
        var raw = "mailto:invalid";
        var ok = QrPayloadParser.TryParse(raw, new QrPayloadParseOptions { Strict = true }, out _, out var validation);
        Assert.False(ok);
        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
    }
}
