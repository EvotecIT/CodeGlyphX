using System;
using System.Text;
using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SwissQrCapacityTests {
    [Theory]
    [InlineData('é', "\n")]
    [InlineData('€', "\n")]
    [InlineData('é', "\r\n")]
    [InlineData('€', "\r\n")]
    public void Parser_RejectsOneByteBeyondCarrierCapacity(char fill, string separator) {
        var value = BoundaryText(998, fill, separator);
        Assert.True(value.Length < 997);
        Assert.False(SwissQrCodePayload.TryParse(value, out _));
        Assert.False(SwissQrCodePayload.TryParseBytes(Encoding.UTF8.GetBytes(value), out _, out _));
    }

    [Theory]
    [InlineData('é')]
    [InlineData('€')]
    public void TypedPayload_RejectsCombinedFieldsBeyondCarrierCapacity(char fill) {
        var fields = BoundaryText(998, fill, "\n").Split('\n');
        Assert.Throws<ArgumentException>(() => FromFields(fields));
    }

    [Theory]
    [InlineData('é', "\n")]
    [InlineData('€', "\n")]
    [InlineData('é', "\r\n")]
    public void AcceptedBoundary_EncodesThroughDedicatedAndPayloadFacades(char fill, string separator) {
        var value = BoundaryText(997, fill, separator);
        Assert.True(SwissQrCodePayload.TryParse(value, out var payload));
        var dedicated = SwissQrCode.Encode(payload);
        Assert.InRange(dedicated.Qr.Version, 1, 25);
        Assert.True(SwissQrCode.TryDecode(dedicated.Modules, out var decoded));
        Assert.Equal(payload.ToString(), decoded.ToString());
        var data = payload.ToPayloadData();
        Assert.Equal(25, data.MaxVersion);
        var generic = QR.Encode(data);
        Assert.InRange(generic.Version, 1, 25);
        Assert.Equal(QrErrorCorrectionLevel.M, generic.ErrorCorrectionLevel);
        Assert.True(SwissQrCode.TryDecode(generic.Modules, out decoded));
        Assert.Equal(payload.ToString(), decoded.ToString());
    }

    [Fact]
    public void OversizedBytes_AreNotClassifiedAsAValidatedSwissPayment() {
        var value = BoundaryText(998, 'é', "\n");
        var qr = QrCodeEncoder.EncodeBytes(Encoding.UTF8.GetBytes(value), QrErrorCorrectionLevel.M,
            minVersion: 26, maxVersion: 26);
        Assert.Equal(SymbolPayloadProfile.None, SymbolDecoder.Decode(qr.Modules, SymbolFormat.QrCode).PayloadProfile);
    }

    internal static string BoundaryText(int bytes, char fill, string separator) {
        var fields = new string[34];
        for (var i = 0; i < fields.Length; i++) fields[i] = string.Empty;
        fields[0] = "SPC"; fields[1] = "0200"; fields[2] = "1";
        // A valid Mod97 CH account with an alphanumeric account part also exercises the byte-mode facade.
        fields[3] = "CH0607620AAAAAAAAAAAA";
        fields[4] = fields[20] = "S";
        fields[5] = fields[9] = fields[21] = fields[25] = "A";
        fields[8] = fields[24] = "8000"; fields[10] = fields[26] = "CH";
        fields[19] = "CHF"; fields[27] = "NON"; fields[30] = "EPD";
        fields[32] = "UV;1"; fields[33] = "UV;2";
        var remaining = bytes - Encoding.UTF8.GetByteCount(string.Join(separator, fields));
        var fillBytes = Encoding.UTF8.GetByteCount(fill.ToString());
        foreach (var slot in new[] { (5, 70), (6, 70), (7, 16), (9, 35), (21, 70), (22, 70), (23, 16), (25, 35), (29, 140), (32, 100), (33, 100) }) {
            while (fields[slot.Item1].Length < slot.Item2 && remaining > 0) {
                var next = remaining >= fillBytes ? fill : 'A';
                fields[slot.Item1] += next;
                remaining -= next == fill ? fillBytes : 1;
            }
        }
        Assert.Equal(0, remaining);
        var value = string.Join(separator, fields);
        Assert.Equal(bytes, Encoding.UTF8.GetByteCount(value));
        return value;
    }

    private static SwissQrCodePayload FromFields(string[] fields) => new(
        new SwissQrCodePayload.Iban(fields[3], SwissQrIbanType.Iban), SwissQrCurrency.CHF,
        SwissQrCodePayload.Contact.CreateStructured(fields[5], fields[6], fields[7], fields[8], fields[9], fields[10]),
        new SwissQrCodePayload.Reference(SwissQrReferenceType.NON),
        new SwissQrCodePayload.AdditionalInformation(fields[29], fields[31]),
        SwissQrCodePayload.Contact.CreateStructured(fields[21], fields[22], fields[23], fields[24], fields[25], fields[26]),
        alternativeProcedure1: fields[32], alternativeProcedure2: fields[33]);
}
