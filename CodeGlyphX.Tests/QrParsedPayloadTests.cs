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
    public void Parse_BitcoinLike() {
        var raw = QrPayloads.BitcoinLike(QrBitcoinLikeType.Bitcoin, "bc1qexample", amount: 0.015, label: "CodeGlyphX", message: "Thanks").Text;
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Crypto, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Crypto>(out var crypto));
        Assert.Equal("bitcoin", crypto.Scheme);
        Assert.Equal("bc1qexample", crypto.Address);
        Assert.True(crypto.Amount.HasValue);
        Assert.Equal(0.015m, crypto.Amount.Value);
        Assert.Equal("CodeGlyphX", crypto.Label);
        Assert.Equal("Thanks", crypto.Message);
    }

    [Fact]
    public void Parse_Monero() {
        var raw = QrPayloads.Monero("48moneroaddr", amount: 1.25f, recipientName: "Alice", description: "Invoice 42").Text;
        Assert.True(QrPayloadParser.TryParse(raw, out var parsed));
        Assert.Equal(QrPayloadType.Crypto, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.Crypto>(out var crypto));
        Assert.Equal("monero", crypto.Scheme);
        Assert.Equal("48moneroaddr", crypto.Address);
        Assert.True(crypto.Amount.HasValue);
        Assert.Equal(1.25m, crypto.Amount.Value);
        Assert.Equal("Alice", crypto.Label);
        Assert.Equal("Invoice 42", crypto.Message);
    }

    [Fact]
    public void Parse_EmvCoMerchant_WithValidCrc() {
        var payload = BuildEmvCoPayload(
            Tlv("00", "01"),
            Tlv("52", "4111"),
            Tlv("53", "840"),
            Tlv("54", "12.34"),
            Tlv("58", "US"),
            Tlv("59", "Test Store"),
            Tlv("60", "NYC"));

        Assert.True(QrPayloadParser.TryParse(payload, out var parsed));
        Assert.Equal(QrPayloadType.EmvCoMerchant, parsed.Type);
        Assert.True(parsed.TryGet<QrParsedData.EmvCoMerchant>(out var emv));
        Assert.True(emv.CrcValid);
        Assert.Equal("01", emv.PayloadFormatIndicator);
        Assert.Equal("4111", emv.MerchantCategoryCode);
        Assert.Equal("840", emv.TransactionCurrency);
        Assert.True(emv.TransactionAmount.HasValue);
        Assert.Equal(12.34m, emv.TransactionAmount.Value);
        Assert.Equal("US", emv.CountryCode);
        Assert.Equal("Test Store", emv.MerchantName);
        Assert.Equal("NYC", emv.MerchantCity);
    }

    [Fact]
    public void Parse_Strict_InvalidEmail_FailsValidation() {
        var raw = "mailto:invalid";
        var ok = QrPayloadParser.TryParse(raw, new QrPayloadParseOptions { Strict = true }, out _, out var validation);
        Assert.False(ok);
        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
    }

    private static string Tlv(string tag, string value) {
        return tag + value.Length.ToString("00") + value;
    }

    private static string BuildEmvCoPayload(params string[] tlvs) {
        var core = string.Concat(tlvs);
        var withPlaceholder = core + "6304" + "0000";
        var crc = ComputeCrc16CcittFalse(withPlaceholder);
        return core + "6304" + crc;
    }

    private static string ComputeCrc16CcittFalse(string text) {
        const ushort poly = 0x1021;
        ushort crc = 0xFFFF;

        for (var i = 0; i < text.Length; i++) {
            var b = (byte)text[i];
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++) {
                if ((crc & 0x8000) != 0) {
                    crc = (ushort)((crc << 1) ^ poly);
                } else {
                    crc <<= 1;
                }
            }
        }

        return crc.ToString("X4");
    }
}
