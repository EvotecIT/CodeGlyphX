using System;
using System.IO;
using System.Globalization;
using System.Linq;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class HibcStructuredTests {
    [Fact]
    public void IndependentHibcAztecSymbols_DecodeFramingAndSupplierFields() {
        using var stream = typeof(HibcStructuredTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-hibc-aztec.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var records = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(4, records.Length);
        foreach (var record in records) {
            var parts = record.TrimEnd('\r').Split('|');
            var size = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var modules = new BitMatrix(size, size);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++) modules[x, y] = parts[2][y * size + x] == '1';
            Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.Aztec, modules, out var value));
            Assert.Equal(parts[0], value);
            Assert.True(Hibc.TryParseStructured(Hibc.Prepare(value), out var parsed));
            Assert.Equal(parts[0], parsed.Data);
        }
    }

    [Fact]
    public void StandardPrimaryAndSeparateSecondary_AreValidatedAndLinked() {
        const string primary = "+A123BJC5D6E71G";
        const string secondary = "+$$52001510X3GD";
        Assert.True(Hibc.TryParseStructured(primary, out var first));
        Assert.Equal("A123", first.Primary!.LabelerCode);
        Assert.Equal("BJC5D6E7", first.Primary.ProductNumber);
        Assert.Equal(1, first.Primary.UnitOfMeasure);
        Assert.Null(first.Production);
        Assert.True(Hibc.TryParseStructured(secondary, out var second));
        Assert.Null(second.Primary);
        Assert.Equal("20015", second.Production!.Date);
        Assert.Equal(HibcDateFormat.YearJulianDay, second.Production.DateFormat);
        Assert.Equal("10X3", second.Production.LotNumber);
        Assert.Equal('G', second.Production.LinkCharacter);
        Assert.True(Hibc.IsLinked(primary, secondary));
        Assert.False(Hibc.IsLinked(Hibc.PrepareStructured("A999BJC5D6E71"), secondary));
        Assert.False(Hibc.IsLinked(primary, Hibc.PrepareStructured("A123BJC5D6E71/$$52001510X3")));
    }

    [Fact]
    public void StandardConcatenatedSupplementalFields_AreRetained() {
        const string payload = "+A99912349/$10X3/16D20111231/14D20200131/Q500Z";
        Assert.True(Hibc.TryParseStructured(payload, out var result));
        Assert.Equal(payload, Hibc.PrepareStructured(result.Data));
        Assert.Equal(9, result.Primary!.UnitOfMeasure);
        Assert.Equal("10X3", result.Production!.LotNumber);
        Assert.Null(result.Production.LinkCharacter);
        Assert.Equal(new DateTime(2011, 12, 31), result.Production.ManufactureDate);
        Assert.Equal(new DateTime(2020, 1, 31), result.Production.ExpiryDate);
        Assert.Equal(500, result.Production.Quantity);
    }

    [Theory]
    [InlineData("$$09053C001G", HibcDateFormat.MonthYear, "0905", "3C001", null)]
    [InlineData("$$20928053C001G", HibcDateFormat.MonthDayYear, "092805", "3C001", null)]
    [InlineData("$$30509283C001G", HibcDateFormat.YearMonthDay, "050928", "3C001", null)]
    [InlineData("$$4050928223C001G", HibcDateFormat.YearMonthDayHour, "05092822", "3C001", null)]
    [InlineData("$$5052713C001G", HibcDateFormat.YearJulianDay, "05271", "3C001", null)]
    [InlineData("$$605271223C001G", HibcDateFormat.YearJulianDayHour, "0527122", "3C001", null)]
    [InlineData("$$73C001G", HibcDateFormat.None, null, "3C001", null)]
    [InlineData("$+0001G", HibcDateFormat.None, null, null, "0001")]
    [InlineData("$$+09050001G", HibcDateFormat.MonthYear, "0905", null, "0001")]
    [InlineData("$$+20928050001G", HibcDateFormat.MonthDayYear, "092805", null, "0001")]
    [InlineData("$$+30509280001G", HibcDateFormat.YearMonthDay, "050928", null, "0001")]
    [InlineData("$$+4050928200001G", HibcDateFormat.YearMonthDayHour, "05092820", null, "0001")]
    [InlineData("$$+5052710001G", HibcDateFormat.YearJulianDay, "05271", null, "0001")]
    [InlineData("$$+605271200001G", HibcDateFormat.YearJulianDayHour, "0527120", null, "0001")]
    [InlineData("$$+70001G", HibcDateFormat.None, null, null, "0001")]
    public void SecondaryDateFormats_RetainTheEncodedCenturyAndFields(string data, HibcDateFormat format,
        string? date, string? lot, string? serial) {
        var payload = Hibc.PrepareStructured(data);
        Assert.True(Hibc.TryParseStructured(payload, out var parsed));
        Assert.Equal(format, parsed.Production!.DateFormat);
        Assert.Equal(date, parsed.Production.Date);
        Assert.Equal(lot, parsed.Production.LotNumber);
        Assert.Equal(serial, parsed.Production.SerialNumber);
        Assert.Equal('G', parsed.Production.LinkCharacter);
    }

    [Theory]
    [InlineData("A12")]
    [InlineData("A123PRODUCTX")]
    [InlineData("A123PRODUCT.1")]
    [InlineData("A12312345678901234567891")]
    [InlineData("$$2132824LOTG")]
    [InlineData("$$2023023LOTG")]
    [InlineData("$$523366LOTG")]
    [InlineData("$$62436624LOTG")]
    [InlineData("$1234567890123456789G")]
    [InlineData("A123BJC5D6E71/$LOT/16D20230229")]
    [InlineData("A123BJC5D6E71/$LOT/Q1")]
    [InlineData("A123BJC5D6E79/$LOT/Q1/S123")]
    [InlineData("A123BJC5D6E71/$LOT/S1/S2")]
    [InlineData("A123BJC5D6E71/$LOT/UNKNOWN")]
    public void InvalidStructure_IsRejectedWithoutChangingLowLevelFraming(string data) {
        var framed = Hibc.Prepare(data);
        Assert.True(Hibc.TryParse(framed, out var original));
        Assert.Equal(data, original);
        Assert.False(Hibc.TryParseStructured(framed, out _));
        Assert.Throws<ArgumentException>(() => Hibc.PrepareStructured(data));
    }

    [Fact]
    public void DateAndIdentifierBoundaries_AreChecked() {
        Assert.NotNull(Hibc.PrepareStructured("$$524366G"));
        Assert.NotNull(Hibc.PrepareStructured("A123BJC5D6E71/$123456789012345678"));
        Assert.NotNull(Hibc.PrepareStructured("A123BJC5D6E71/$LOT/14D20240229"));
        Assert.False(Hibc.TryParseStructured(Hibc.Prepare("A123BJC5D6E71/$1234567890123456789"), out _));
    }

    [Theory]
    [InlineData("$LOT-1", "LOT-1", null)]
    [InlineData("$$524366LOT.1", "LOT.1", null)]
    [InlineData("$$+7SER-1", null, "SER-1")]
    [InlineData("$+SER.1", null, "SER.1")]
    [InlineData("$LOT-1/SSER.1", "LOT-1", "SER.1")]
    public void SecondaryPunctuation_IsPreservedInCombinedAndSeparateStructures(string production, string? lot, string? serial) {
        var combined = Hibc.PrepareStructured("A123PRODUCT1/" + production);
        Assert.True(Hibc.TryParseStructured(combined, out var combinedFields));
        Assert.Equal(lot, combinedFields.Production!.LotNumber);
        Assert.Equal(serial, combinedFields.Production.SerialNumber);
        var primary = Hibc.PrepareStructured("A123PRODUCT1");
        var split = production.IndexOf('/');
        var secondary = split < 0 ? production + primary[primary.Length - 1]
            : production.Insert(split, primary[primary.Length - 1].ToString());
        var framed = Hibc.PrepareStructured(secondary);
        Assert.True(Hibc.IsLinked(primary, framed));
        Assert.True(Hibc.TryParseStructured(framed, out var separateFields));
        Assert.Equal(lot, separateFields.Production!.LotNumber);
        Assert.Equal(serial, separateFields.Production.SerialNumber);
        Assert.False(Hibc.TryParseStructured(Hibc.Prepare("A123PRO-DUCT1/" + production), out _));
        Assert.False(Hibc.TryParseStructured(Hibc.Prepare("A123PRO.DUCT1/" + production), out _));
    }

    [Fact]
    public void LinkedQuantity_RequiresVariableQuantityPrimaryLikeConcatenation() {
        foreach (var unit in new[] { 1, 9 }) {
            var primaryData = "A123BJC5D6E7" + unit;
            var primary = Hibc.PrepareStructured(primaryData);
            var secondary = Hibc.PrepareStructured("$LOT-1" + primary[primary.Length - 1] + "/SABC.1/16D20240229/Q1");
            Assert.Equal(unit == 9, Hibc.IsLinked(primary, secondary));
            Assert.Equal(unit == 9, Hibc.TryParseStructured(Hibc.Prepare(primaryData + "/$LOT-1/SABC.1/16D20240229/Q1"), out _));
        }
    }

    [Fact]
    public void HistoricBareJulianLot_IsDecodedButNotProducedByStrictPreparation() {
        var payload = Hibc.Prepare("05271OLDLOTG");
        Assert.True(Hibc.TryParseStructured(payload, out var result));
        Assert.Equal("OLDLOT", result.Production!.LotNumber);
        Assert.True(result.Production.HasLegacyLot);
        Assert.Throws<ArgumentException>(() => Hibc.PrepareStructured(result.Data));
    }

    [Fact]
    public void SlashLinkAndSupplementDelimiter_RemainDistinct() {
        var payload = Hibc.Prepare("$LOT//S123");
        Assert.True(Hibc.TryParseStructured(payload, out var result));
        Assert.Equal('/', result.Production!.LinkCharacter);
        Assert.Equal("LOT", result.Production.LotNumber);
        Assert.Equal("123", result.Production.SerialNumber);
    }

    [Fact]
    public void HumanReadableInterpretation_PreservesPayloadWhileFormattingPrintCharacters() {
        const string payload = "+A123BJC5D6E71/$$52001510X3C";
        Assert.Equal("*+A123BJC5D6E71/\n$$52ØØ151ØX3C*", Hibc.HumanReadable(payload));
        var withSpace = Hibc.Prepare("$LOT ");
        Assert.Equal("*+$LOT_K*", Hibc.HumanReadable(withSpace));
        Assert.Throws<ArgumentException>(() => Hibc.HumanReadable(payload + "0"));
    }

    [Fact]
    public void AztecCarrier_EncodesAndDecodesCheckedSupplierPayload() {
        const string data = "A123BJC5D6E71";
        var symbol = Hibc.EncodeMatrix(SymbolFormat.Aztec, data, validateStructure: true);
        Assert.Equal(SymbolFormat.Aztec, symbol.Format);
        Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.Aztec, symbol.Modules, out var decoded));
        Assert.Equal(data, decoded);
        var image = symbol.Render(OutputFormat.Png, new MatrixOptions { ModuleSize = 5 }).ToArray();
        var scan = SymbolScanner.Scan(image, new ScanOptions { Formats = new[] { SymbolFormat.Aztec }, MaxSymbols = 1 });
        Assert.True(Hibc.TryParseStructured(Assert.Single(scan.Symbols).Text, out var parsed));
        Assert.Equal(data, parsed.Data);
        Assert.Throws<ArgumentException>(() => Hibc.EncodeMatrix(SymbolFormat.Aztec, "A12", validateStructure: true));
    }
}
