using System;
using System.Text.Json;
using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SwissQrConformanceTests {
    [Fact]
    public void IndependentBwippSymbols_DecodeUtf8PaymentsWithAllReferenceTypes() {
        using var stream = typeof(SwissQrConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "swissqrcode") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < matrix.Height; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
            Assert.True(SwissQrCode.TryDecode(matrix, out var payload));
            Assert.Equal(value, payload.ToString());
            Assert.Equal("Max Muster & Söhne", payload.Creditor.Name);
            Assert.Equal(50m, payload.Amount);
            Assert.True(QrDecoder.TryDecode(matrix, out var qr));
            Assert.Equal(value, qr.Text);
            Assert.Equal(QrPayloadType.SwissQr, qr.Parsed.Type);
            Assert.Equal(SymbolPayloadProfile.SwissQr, SymbolDecoder.Decode(matrix, SymbolFormat.QrCode).PayloadProfile);
            var encoded = SwissQrCode.Encode(payload);
            Assert.Equal(QrErrorCorrectionLevel.M, encoded.Qr.ErrorCorrectionLevel);
            Assert.True(SwissQrCode.TryDecode(encoded.Modules, out var decoded));
            Assert.Equal(value, decoded.ToString());
            count++;
        }
        Assert.Equal(3, count);
    }

    [Fact]
    public void OptionalFields_PreserveTheirPositionsAndLegacyAddressesRequireAnExplicitChoice() {
        var payload = Create(alternative2: "UV;12345");
        var fields = payload.ToString().Split('\n');
        Assert.Equal(34, fields.Length);
        Assert.Equal("", fields[31]);
        Assert.Equal("", fields[32]);
        Assert.Equal("UV;12345", fields[33]);
        Assert.True(SwissQrCodePayload.TryParse(payload.ToString(), out var parsed));
        Assert.Equal("UV;12345", parsed.AlternativeProcedure2);
        Assert.True(SwissQrCodePayload.TryParse(payload.ToString().Replace("\n", "\r\n"), out parsed));
        Assert.Equal(payload.ToString(), parsed.ToString());
        var legacy = new SwissQrCodePayload(new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban), SwissQrCurrency.CHF,
            SwissQrCodePayload.Contact.CreateCombined("Test", "Main Street 1", "8000 Zurich", "CH"), new SwissQrCodePayload.Reference(SwissQrReferenceType.NON));
        Assert.False(SwissQrCodePayload.TryParse(legacy.ToString(), out _));
        Assert.True(SwissQrCodePayload.TryParse(legacy.ToString(), out _, allowLegacyCombinedAddresses: true));
        Assert.Throws<ArgumentException>(() => SwissQrCode.Encode(legacy));
    }

    [Fact]
    public void InvalidPaymentFields_AreRejectedBeforeEncodingOrReturningDecodedData() {
        Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF19539007547034"));
        Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Reference(SwissQrReferenceType.QRR, "00"));
        Assert.Throws<ArgumentException>(() => Create(amount: -1));
        Assert.Throws<ArgumentException>(() => Create(amount: 1.001m));
        var value = Create().ToString();
        var fields = value.Split('\n');
        fields[11] = "S";
        Assert.False(SwissQrCodePayload.TryParse(string.Join("\n", fields), out _));
        fields = value.Split('\n'); fields[30] = "WRONG";
        Assert.False(SwissQrCodePayload.TryParse(string.Join("\n", fields), out _));
        fields = value.Split('\n'); fields[18] = "01.00";
        Assert.False(SwissQrCodePayload.TryParse(string.Join("\n", fields), out _));
        Assert.False(SwissQrCodePayload.TryParse(value.Replace("SPC\n", "SPC\r\n"), out _));
    }

    [Fact]
    public void CrossOverlay_RenderedPixelsRemainDecodableAndPreserveThePayment() {
        var symbol = SwissQrCode.Encode(Create());
        var pixels = symbol.RenderPixels(out var width, out var height, out var stride);
        Assert.True(SwissQrCode.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(symbol.Payload.ToString(), decoded.ToString());
        var center = (height / 2) * stride + width / 2 * 4;
        Assert.Equal(255, pixels[center]);
        var blackArm = (height / 2 - 2) * stride + (width / 2 - 20) * 4;
        Assert.Equal(0, pixels[blackArm]);
    }

    [Theory]
    [InlineData("AA")]
    [InlineData("ZZ")]
    [InlineData("UK")]
    [InlineData("XK")]
    [InlineData("EU")]
    [InlineData("AN")]
    [InlineData("ſG")]
    public void UnassignedCountries_AreRejectedForBothAddressKindsAndDecodedContacts(string country) {
        Assert.Throws<ArgumentException>(() => SwissQrCodePayload.Contact.CreateStructured("Test", "Street", "1", "8000", "Zurich", country));
        Assert.Throws<ArgumentException>(() => SwissQrCodePayload.Contact.CreateCombined("Test", "Street 1", "8000 Zurich", country));
        foreach (var countryField in new[] { 10, 26 }) {
            var fields = Create().ToString().Split('\n');
            Array.Copy(fields, 4, fields, 20, 7);
            fields[countryField] = country;
            var text = string.Join("\n", fields);
            Assert.False(SwissQrCodePayload.TryParse(text, out _));
            Assert.False(SwissQrCode.TryDecode(QR.Encode(text).Modules, out _));
        }
    }

    [Theory]
    [InlineData("ch", "CH")]
    [InlineData("LI", "LI")]
    [InlineData("GB", "GB")]
    [InlineData("SS", "SS")]
    [InlineData("BQ", "BQ")]
    public void AssignedCountries_RemainValidAndNormalizeToUppercase(string country, string expected) {
        var contact = SwissQrCodePayload.Contact.CreateStructured("Test", "Street", "1", "8000", "Town", country);
        Assert.Equal(expected, contact.Country);
        var fields = Create().ToString().Split('\n');
        Array.Copy(contact.ToString().TrimEnd('\n').Split('\n'), 0, fields, 20, 7);
        Assert.True(SwissQrCodePayload.TryParse(string.Join("\n", fields), out var parsed));
        Assert.Equal(expected, parsed.Debtor!.Country);
    }

    internal static SwissQrCodePayload Create(string? alternative2 = null, decimal amount = 50) => new(
        new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban), SwissQrCurrency.CHF,
        SwissQrCodePayload.Contact.CreateStructured("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH"),
        new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF18539007547034"), amount: amount, alternativeProcedure2: alternative2);
}
