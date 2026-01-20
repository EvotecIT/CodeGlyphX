using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PayPalPayloadTests {
    [Fact]
    public void PayPalMe_Builds_Url_With_Amount_And_Currency() {
        var payload = QrPayloads.PayPalMe("evotec", 12.5m, "usd");
        Assert.Equal("https://paypal.me/evotec/12.5USD", payload.Text);
    }

    [Fact]
    public void PayPalMe_Parser_Extracts_Handle_And_Amount() {
        Assert.True(QrPayloadParser.TryParse("https://paypal.me/evotec/25EUR", out var parsed));
        Assert.Equal(QrPayloadType.PayPal, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.PayPal>(out var paypal));
        Assert.Equal("evotec", paypal!.Handle);
        Assert.Equal(25m, paypal.Amount);
        Assert.Equal("EUR", paypal.Currency);
    }

    [Fact]
    public void Payload_Builders_Reject_Invalid_Email_And_Phone() {
        Assert.Throws<ArgumentException>(() => QrPayloads.Email("not-an-email"));
        Assert.Throws<ArgumentException>(() => QrPayloads.Phone("12"));
        Assert.Throws<ArgumentException>(() => QrPayloads.Sms("12", "hi"));
    }
}
