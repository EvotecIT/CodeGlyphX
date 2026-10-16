using System;
using System.Text;
using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SwissQrValidationBoundaryTests {
    [Fact]
    public void InvalidUtf8PaymentBytes_RemainTextWithoutASwissProfile() {
        var text = SwissQrCapacityTests.BoundaryText(200, 'é', "\n");
        Assert.True(SwissQrCodePayload.TryParse(text, out _));
        var bytes = Encoding.GetEncoding(28591).GetBytes(text);
        Assert.False(SwissQrCodePayload.TryParseBytes(bytes, out _, out _));
        var qr = QrCodeEncoder.EncodeBytes(bytes, QrErrorCorrectionLevel.M);
        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(QrPayloadType.Text, decoded.Parsed.Type);
        Assert.Equal(SymbolPayloadProfile.None, SymbolDecoder.Decode(qr.Modules, SymbolFormat.QrCode).PayloadProfile);
        Assert.False(SwissQrCode.TryDecode(qr.Modules, out _));
    }

    [Fact]
    public void UnicodeReference_IsRejectedBeforeCaseNormalization() {
        const string reference = "RF24ſ";
        Assert.False(QrPayloadValidation.IsValidCreditorReference(reference));
        Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, reference));
        var fields = SwissQrCapacityTests.BoundaryText(200, 'é', "\n").Split('\n');
        fields[27] = "SCOR"; fields[28] = reference;
        Assert.False(SwissQrCodePayload.TryParse(string.Join("\n", fields), out _));
    }

    [Fact]
    public void UnicodeAccount_IsRejectedBeforeCaseNormalization() {
        const string account = "CH0507620AAAAAAAAAAAſ";
        Assert.False(QrPayloadValidation.IsValidIban(account));
        Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Iban(account, SwissQrIbanType.Iban));
        var fields = SwissQrCapacityTests.BoundaryText(200, 'é', "\n").Split('\n');
        fields[3] = account;
        Assert.False(SwissQrCodePayload.TryParse(string.Join("\n", fields), out _));
    }

    [Fact]
    public void AsciiIdentifiers_PreserveLowercaseAndTypedAccountFormattingSupport() {
        var account = new SwissQrCodePayload.Iban("ch05-07620 aaaaaaaaaaas", SwissQrIbanType.Iban);
        Assert.Equal("CH0507620AAAAAAAAAAAS", account.ToString());
        var reference = new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "rf24s");
        Assert.Equal("RF24S", reference.ReferenceText);
    }

    [Fact]
    public void LegacyCombinedAddress_RequiresANonemptyCleanedSecondLine() {
        Assert.Throws<ArgumentException>(() => SwissQrCodePayload.Contact.CreateCombined("Test", "Street 1", "\r\n", "CH"));
    }
}
