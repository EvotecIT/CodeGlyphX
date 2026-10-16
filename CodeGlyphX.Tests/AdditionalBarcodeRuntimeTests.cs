using CodeGlyphX.Mailmark;
using CodeGlyphX.Postal;
using CodeGlyphX.Code16K;
using CodeGlyphX.ChannelCode;
using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

// These vectors also run in the explicit .NET Framework test target, without
// adding a JSON runtime dependency to its compatibility qualification.
public sealed class AdditionalBarcodeRuntimeTests {
    [Fact]
    public void CheckedApplicationFields_RejectMalformedValuesOnEveryRuntime() {
        Assert.Throws<ArgumentException>(() => SwissQrCodePayload.Contact.CreateStructured("Test", "Street", "1", "8000", "Town", "AA"));
        Assert.Throws<ArgumentException>(() => Mailmark2D.Prepare("1001234", "12345678", "AAAAAAAAA"));
        Assert.Throws<ArgumentException>(() => ChannelCodeEncoder.Encode("5"));
        var partialDestination = Mailmark2D.Prepare("1001234", "12345678", "NE");
        Assert.True(Mailmark2D.TryDecode(Mailmark2D.Encode(partialDestination).Modules, out var mail, out _));
        Assert.Equal("NE", mail.Destination);
    }

    [Fact]
    public void SwissPaymentAndExtendedSequence_PreserveRuntimeTextAndMetadata() {
        var payment = new SwissQrCodePayload(
            new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban), SwissQrCurrency.CHF,
            SwissQrCodePayload.Contact.CreateStructured("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH"),
            new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF18539007547034"), amount: 50);
        var swiss = SwissQrCode.Encode(payment);
        Assert.True(SwissQrCode.TryDecode(swiss.Modules, out var parsed));
        Assert.Equal(payment.ToString(), parsed.ToString());
        Assert.True(SymbolDecoder.TryDecode(swiss.Modules, out var common, SymbolFormat.QrCode));
        Assert.Equal(SymbolPayloadProfile.SwissQr, common.PayloadProfile);
        Assert.Equal(payment.ToString(), common.Text);
        var append = new Code16KStructuredAppend(2, 5);
        var part = Code16KEncoder.Encode("EXTENDED", new Code16KEncodingOptions { StructuredAppend = append });
        Assert.True(SymbolDecoder.TryDecode(part.Modules, out common, SymbolFormat.Code16K));
        Assert.Equal("EXTENDED", common.Text);
        Assert.Equal(append, Assert.IsType<Code16KSymbolMetadata>(common.Metadata).StructuredAppend);
    }

    [Theory]
    [InlineData("1100000000000XY11     ", "TTDTTATTDTAATTDTAATTDTAATTDTTDDAATAADDATAATDDFAFTDDTAADDDTAAFDFAFF")]
    [InlineData("11000000000000000XY11     ", "TTDTTATDDTTATTDTAATTDTAATDDTTATTDTTDATFTAATDDTAATDDTATATFAADDAATAATDDTAADFTFTA")]
    public void MailmarkPublicFacade_DecodesRoyalMailStateVectors(string expected, string states) {
        var matrix = DaftCode.Encode(states).Modules;
        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.Mailmark4State, matrix, out var decoded));
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void MailmarkC40Header_IsPreservedByTheRuntimeEncoderAndDetailedDecoder() {
        var payload = Mailmark2D.Prepare("1001234", "12345678", "AB19XY1A", customerContent: "www.xyz.com");
        var symbol = Mailmark2D.Encode(payload);
        Assert.True(Mailmark2D.TryDecode(symbol.Modules, out var decoded, out var type));
        Assert.Equal(payload, decoded.Text);
        Assert.Equal(Mailmark2DType.Type29, type);
    }
}
