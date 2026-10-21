using System;
using System.IO;
using System.Linq;
using CodeGlyphX.ChannelCode;
using CodeGlyphX.Msi;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class LinearControlConformanceTests {
    [Fact]
    public void ShortChannelFinders_MatchIndependentFirstAndLastValuesAtEveryCapacity() {
        var count = 0;
        foreach (var fixture in Fixtures()) {
            if (fixture[0] != "channelcode") continue;
            var value = fixture[1];
            var bits = fixture[2];
            var encoded = ChannelCodeEncoder.Encode(value, shortFinder: true);
            Assert.Equal(bits, Bits(encoded));
            Assert.Equal(Bits(ChannelCodeEncoder.Encode(value)).Substring(4), bits);
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(c => c == '1').ToArray(), BarcodeType.ChannelCode, out var decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(12, count);
    }

    [Theory]
    [InlineData("00", 0)]
    [InlineData("00", 90)]
    [InlineData("00", 180)]
    [InlineData("00", 270)]
    [InlineData("26", 0)]
    [InlineData("7742862", 0)]
    [InlineData("7742862", 90)]
    [InlineData("7742862", 180)]
    [InlineData("7742862", 270)]
    public void ShortChannelFinders_AreAvailableThroughNormalImageScanning(string value, int rotation) {
        var layout = BarcodePrint.Layout(ChannelCodeEncoder.Encode(value, shortFinder: true), new BarcodePrintOptions {
            IncludeHumanReadable = false, Dpi = 300, XDimensionMm = .508
        });
        var result = SymbolScanner.Scan(layout.Render(OutputFormat.Png, new BarcodePrintPlacement { RotationDegrees = rotation }).ToArray(), new ScanOptions {
            Formats = new[] { SymbolFormat.ChannelCode }, TimeoutMilliseconds = 5000
        });
        Assert.Equal(value, Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void IssnIssueVariants_MatchIndependentBarsAndSurviveDetailedInverseValidation() {
        var count = 0;
        foreach (var fixture in Fixtures()) {
            if (fixture[0] != "issn") continue;
            var value = fixture[1];
            var bits = fixture[2];
            var encoded = BarcodeProfiles.Encode(LinearBarcodeProfile.Issn, value);
            Assert.Equal(bits, Bits(encoded));
            Assert.Equal("03178471", BarcodeProfiles.Prepare(LinearBarcodeProfile.Issn, value));
            Assert.Equal("ISSN 0317-8471", BarcodeProfiles.HumanReadable(LinearBarcodeProfile.Issn, value));
            Assert.True(BarcodeProfiles.TryDecodeDetailed(LinearBarcodeProfile.Issn, bits.Select(c => c == '1').ToArray(), out var data));
            Assert.Equal(value.Substring(value.Length - 2), data.IssueVariant);
            Assert.Equal("03178471", data.Identifier);
            Assert.Equal("9770317847" + data.IssueVariant, data.CarrierText.Substring(0, 12));
            Assert.Equal("ISSN 0317-8471", data.HumanReadable);
            count++;
        }
        Assert.Equal(3, count);
        Assert.Equal(Bits(BarcodeProfiles.Encode(LinearBarcodeProfile.Issn, "0317-8471 00")),
            Bits(BarcodeProfiles.Encode(LinearBarcodeProfile.Issn, "03 17 84 71")));
        Assert.True(BarcodeProfiles.TryParseDetailed(LinearBarcodeProfile.Isbn, "9780306406157", out var isbn));
        Assert.Null(isbn.IssueVariant);
        Assert.Equal("9780306406157", isbn.Identifier);
        Assert.False(BarcodeProfiles.TryParseDetailed(LinearBarcodeProfile.Issn, "9770317847996", out _));
    }

    [Theory]
    [InlineData("0317-8472 12")]
    [InlineData("0317-8471 A1")]
    [InlineData("0317-8471 1")]
    [InlineData("0317-8471 123")]
    [InlineData("0317-8471 １２")]
    public void IssnIssueVariants_RejectInvalidIdentifiersAndNonCanonicalVariants(string value) =>
        Assert.Throws<ArgumentException>(() => BarcodeProfiles.Encode(LinearBarcodeProfile.Issn, value));

    [Fact]
    public void PublishingIdentifiers_DoNotClassifyTheIsmnPrefixAsIsbn() {
        const string music = "9790230671187";
        Assert.Throws<ArgumentException>(() => BarcodeProfiles.Encode(LinearBarcodeProfile.Isbn, music));
        Assert.False(BarcodeProfiles.TryParseDetailed(LinearBarcodeProfile.Isbn, music, out _));
        Assert.True(BarcodeProfiles.TryParseDetailed(LinearBarcodeProfile.Ismn, music, out var ismn));
        Assert.Equal(music, ismn.Identifier);
    }

    [Fact]
    public void MsiAlgorithms_MatchIndependentCheckedModulesAndValidateTheSelectedConvention() {
        var count = 0;
        foreach (var fixture in Fixtures()) {
            if (fixture[0] != "msi") continue;
            var type = MsiAlgorithm(fixture[3]);
            var value = fixture[1]; var bits = fixture[2];
            Assert.Equal(bits, Bits(MsiEncoder.Encode(value, type)));
            var modules = bits.Select(bit => bit == '1').ToArray();
            Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, out var raw));
            Assert.StartsWith(value, raw.Text);
            if (value == "6" && type is MsiChecksumType.Mod11 or MsiChecksumType.NcrMod11) Assert.Equal("610", raw.Text);
            var options = new BarcodeDecodeOptions().WithMsiChecksum(MsiChecksumPolicy.RequireValid, type, value.Length);
            Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, options, out var decoded));
            Assert.Equal(value, decoded.Text);
            var wrong = raw.Text.Substring(0, raw.Text.Length - 1) + (char)('0' + (raw.Text[raw.Text.Length - 1] - '0' + 1) % 10);
            var wrongModules = Bits(MsiEncoder.Encode(wrong, MsiChecksumType.None)).Select(bit => bit == '1').ToArray();
            Assert.False(BarcodeDecoder.TryDecode(wrongModules, BarcodeType.MSI, options, out _));
            count++;
        }
        Assert.Equal(18, count);
    }

    [Fact]
    public void AmbiguousMsiMod11_DoesNotDiscardDataWithoutAnApplicationLengthHint() {
        var modules = Bits(MsiEncoder.Encode("7110", MsiChecksumType.None)).Select(bit => bit == '1').ToArray();
        var options = new BarcodeDecodeOptions().WithMsiChecksum(MsiChecksumPolicy.RequireValid, MsiChecksumType.Mod11);
        Assert.False(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, options, out _));
        options.MsiChecksum = MsiChecksumPolicy.StripIfValid;
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, options, out var unchanged));
        Assert.Equal("7110", unchanged.Text);
        options.MsiChecksum = MsiChecksumPolicy.RequireValid;
        options.MsiExpectedDataLength = 2;
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, options, out var shorter));
        Assert.Equal("71", shorter.Text);
        options.MsiExpectedDataLength = 3;
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.MSI, options, out var longer));
        Assert.Equal("711", longer.Text);
    }

    [Theory]
    [InlineData(MsiChecksumType.Mod11)]
    [InlineData(MsiChecksumType.Mod11Mod10)]
    [InlineData(MsiChecksumType.NcrMod11)]
    [InlineData(MsiChecksumType.NcrMod11Mod10)]
    public void MsiImageScan_RetainsTheSelectedAlgorithmAndApplicationLength(MsiChecksumType type) {
        var layout = BarcodePrint.Layout(MsiEncoder.Encode("12345678", type));
        var result = SymbolScanner.Scan(layout.Render(OutputFormat.Png).ToArray(), new ScanOptions {
            Formats = new[] { SymbolFormat.Msi }, TimeoutMilliseconds = 5000,
            Barcode = new BarcodeDecodeOptions().WithMsiChecksum(MsiChecksumPolicy.RequireValid, type, 8)
        });
        Assert.Equal("12345678", Assert.Single(result.Symbols).Text);
    }

    private static MsiChecksumType MsiAlgorithm(string options) {
        foreach (var item in new[] { ("mod10", MsiChecksumType.Mod10), ("mod1010", MsiChecksumType.Mod10Mod10),
            ("mod11", MsiChecksumType.Mod11), ("mod1110", MsiChecksumType.Mod11Mod10),
            ("ncrmod11", MsiChecksumType.NcrMod11), ("ncrmod1110", MsiChecksumType.NcrMod11Mod10) })
            if (options.Contains("\"checktype\":\"" + item.Item1 + "\"")) return item.Item2;
        throw new InvalidOperationException("Fixture must select an explicit MSI algorithm.");
    }

    private static string Bits(Barcode1D barcode) => string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules)));
    private static string[][] Fixtures() {
        using var stream = typeof(LinearControlConformanceTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear-controls.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').Select(line => line.TrimEnd('\r').Split('\t')).ToArray();
    }
}
