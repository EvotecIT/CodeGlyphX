using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeProfileConformanceTests {
    [Fact]
    public void IdentifierProfiles_MatchIndependentBwippAndValidateExternalCarrierPayloads() {
        using var stream = typeof(BarcodeProfileConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var profile = fixture.GetProperty("bcid").GetString() switch {
                "ean14" => LinearBarcodeProfile.Ean14,
                "sscc18" => LinearBarcodeProfile.Sscc18,
                "isbn" => LinearBarcodeProfile.Isbn,
                "ismn" => LinearBarcodeProfile.Ismn,
                "issn" => LinearBarcodeProfile.Issn,
                "pzn" => fixture.GetProperty("options").TryGetProperty("pzn8", out _) ? LinearBarcodeProfile.Pzn8 : LinearBarcodeProfile.Pzn7,
                "identcode" => LinearBarcodeProfile.Identcode,
                "leitcode" => LinearBarcodeProfile.Leitcode,
                "mands" => LinearBarcodeProfile.MarksAndSpencer,
                _ => (LinearBarcodeProfile?)null
            };
            if (!profile.HasValue) continue;
            var value = fixture.GetProperty("text").GetString()!;
            if (profile == LinearBarcodeProfile.Ean14 || profile == LinearBarcodeProfile.Sscc18) value = value.Substring(4);
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = BarcodeProfiles.Encode(profile.Value, value);
            // Code 39 permits multiple wide:narrow ratios. Compare the same
            // symbol elements at our encoder's ratio, then decode raw BWIPP widths.
            var expectedBits = profile == LinearBarcodeProfile.Pzn7 || profile == LinearBarcodeProfile.Pzn8
                ? string.Concat(fixture.GetProperty("runs").EnumerateArray().Select((run, index) =>
                    new string((index & 1) == 0 ? '1' : '0', run.GetInt32() == 3 ? 2 : run.GetInt32()))).TrimEnd('0')
                : profile == LinearBarcodeProfile.Identcode || profile == LinearBarcodeProfile.Leitcode
                    ? string.Concat(fixture.GetProperty("runs").EnumerateArray().Select((run, index) =>
                        new string((index & 1) == 0 ? '1' : '0', run.GetInt32() == 2 ? 3 : run.GetInt32()))).TrimEnd('0')
                    : bits;
            Assert.Equal(expectedBits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeProfiles.TryDecode(profile.Value, bits.Select(bit => bit == '1').ToArray(), out var decoded));
            Assert.Equal(BarcodeProfiles.Prepare(profile.Value, value), decoded);
            count++;
        }
        Assert.Equal(10, count);
    }

    [Theory]
    [InlineData(LinearBarcodeProfile.Isbn, "0-306-40615-2", "9780306406157")]
    [InlineData(LinearBarcodeProfile.Isbn, "080442957X", "9780804429573")]
    [InlineData(LinearBarcodeProfile.Ismn, "M-2306-7118-7", "9790230671187")]
    [InlineData(LinearBarcodeProfile.Issn, "0317-8471", "03178471")]
    [InlineData(LinearBarcodeProfile.Pzn7, "123456", "1234562")]
    [InlineData(LinearBarcodeProfile.Pzn8, "1234567", "12345678")]
    public void PublishingAndPharmacyIdentifiers_UseTheirOwnCheckDigitRules(
        LinearBarcodeProfile profile, string value, string expected) {
        Assert.Equal(expected, BarcodeProfiles.Prepare(profile, value));
    }

    [Theory]
    [InlineData(LinearBarcodeProfile.Isbn, "0306406153")]
    [InlineData(LinearBarcodeProfile.Isbn, "9780306406158")]
    [InlineData(LinearBarcodeProfile.Ismn, "M230671188")]
    [InlineData(LinearBarcodeProfile.Issn, "03178472")]
    [InlineData(LinearBarcodeProfile.Pzn7, "1234563")]
    [InlineData(LinearBarcodeProfile.Pzn8, "12345679")]
    [InlineData(LinearBarcodeProfile.Ean14, "09506000134353")]
    [InlineData(LinearBarcodeProfile.Sscc18, "376123450000010038")]
    public void IdentifierProfiles_RejectIncorrectApplicationCheckDigits(LinearBarcodeProfile profile, string value) {
        Assert.Throws<ArgumentException>(() => BarcodeProfiles.Encode(profile, value));
    }
}
