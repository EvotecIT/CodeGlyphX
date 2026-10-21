using System;
using System.IO;
using System.Linq;
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AustraliaPostControlTests {
    [Fact]
    public void ExplicitPostalFields_MatchIndependentVariableLengthStatesAndParity() {
        using var stream = typeof(AustraliaPostControlTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Conformance.bwipp-australia-controls.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var records = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(20, records.Length);
        foreach (var record in records) {
            var parts = record.TrimEnd('\r').Split('\t');
            var table = parts[1] == "numeric" ? AustraliaPostCustomerEncodingTable.N : AustraliaPostCustomerEncodingTable.C;
            var encoded = AustraliaPostEncoder.EncodeSymbol(parts[0], table);
            Assert.Equal(parts[2], States(encoded.Modules));
            Assert.Equal(parts[0], encoded.Data.EncodedValue);
            Assert.Equal(4, encoded.Data.ErrorCorrectionSymbols.Count);
            Assert.All(encoded.Data.ErrorCorrectionSymbols, value => Assert.InRange(value, 0, 63));
            var independent = FromStates(parts[2]);
            Assert.True(AustraliaPostDecoder.TryDecode(independent, table, parts[0].Length - 10, out var decoded), parts[0]);
            Assert.Equal(parts[0], decoded.EncodedValue);
            Assert.False(decoded.IsCustomerInfoAmbiguous);
            Assert.Equal(encoded.Data.ErrorCorrectionSymbols, decoded.ErrorCorrectionSymbols);
            Assert.True(SymbolDecoder.TryDecode(independent, out var symbol, SymbolFormat.AustraliaPost));
            Assert.IsType<AustraliaPostSymbolMetadata>(symbol.Metadata);
        }
    }

    [Fact]
    public void TrailingZAndPadding_RetainAlternativesUntilTheApplicationSuppliesItsLength() {
        var shortField = AustraliaPostEncoder.EncodeSymbol("5956439111AB");
        var longerField = AustraliaPostEncoder.EncodeSymbol("5956439111ABzzz");
        Assert.Equal(States(shortField.Modules), States(longerField.Modules));
        Assert.True(AustraliaPostDecoder.TryDecode(shortField.Modules, AustraliaPostCustomerEncodingTable.C, out var ambiguous));
        Assert.True(ambiguous.IsCustomerInfoAmbiguous);
        Assert.Contains(ambiguous.CustomerInfoAlternatives, item => item.EncodingTable == AustraliaPostCustomerEncodingTable.C && item.Text == "AB");
        Assert.Contains(ambiguous.CustomerInfoAlternatives, item => item.EncodingTable == AustraliaPostCustomerEncodingTable.C && item.Text == "ABzzz");
        Assert.True(ambiguous.TrySelectCustomerInfo(AustraliaPostCustomerEncodingTable.C, 2, out var selected));
        Assert.Equal("AB", selected.CustomerInfo);
        Assert.False(selected.IsCustomerInfoAmbiguous);
        Assert.True(ambiguous.TrySelectCustomerInfo(AustraliaPostCustomerEncodingTable.C, 5, out var full));
        Assert.Equal("ABzzz", full.CustomerInfo);
        Assert.False(ambiguous.TrySelectCustomerInfo(AustraliaPostCustomerEncodingTable.C, 1, out _));
        Assert.Equal("AB", shortField.Data.CustomerInfo);
        Assert.False(shortField.Data.IsCustomerInfoAmbiguous);
    }

    [Fact]
    public void NullCustomerCode_RequiresZeroDpidAndRetainsItsCheckedFields() {
        var encoded = AustraliaPostEncoder.EncodeSymbol("0000000000");
        Assert.Equal(73, encoded.Width); // 37 bars separated by 36 spaces.
        Assert.True(AustraliaPostDecoder.TryDecode(encoded.Modules, null, out var decoded));
        Assert.Equal(0, decoded.FormatControlCode);
        Assert.Equal("00000000", decoded.Dpid);
        Assert.Empty(decoded.CustomerInfo);
        Assert.Equal(encoded.Data.ErrorCorrectionSymbols, decoded.ErrorCorrectionSymbols);
        Assert.Throws<ArgumentException>(() => AustraliaPostEncoder.EncodeSymbol("0000000001"));
        Assert.Throws<ArgumentException>(() => AustraliaPostEncoder.EncodeSymbol("0000000000A"));
        var damaged = encoded.Modules.Clone();
        var parityBar = damaged.Width - 6;
        damaged[parityBar, 0] = !damaged[parityBar, 0];
        damaged[parityBar, 1] = damaged[parityBar, 2] = damaged[parityBar, 0];
        Assert.False(AustraliaPostDecoder.TryDecode(damaged, out _));
    }

    [Theory]
    [InlineData("5956439111AB", AustraliaPostCustomerEncodingTable.C, 0)]
    [InlineData("5956439111ABz", AustraliaPostCustomerEncodingTable.C, 90)]
    [InlineData("595643911112", AustraliaPostCustomerEncodingTable.N, 180)]
    [InlineData("6256439111zz", AustraliaPostCustomerEncodingTable.C, 270)]
    [InlineData("0000000000", AustraliaPostCustomerEncodingTable.C, 0)]
    public void PrintedPostalLine_StaysAboveBarsAndImageHintsRecoverExactCustomerFields(string input, AustraliaPostCustomerEncodingTable table, int rotation) {
        var encoded = AustraliaPostEncoder.EncodeSymbol(input, table);
        var options = new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font(), FontSizePoints = 8 };
        var layout = BarcodePrint.Layout(encoded, options);
        var text = Assert.Single(layout.Text);
        Assert.Equal(encoded.Data.HumanReadable, text.Text);
        Assert.True(text.InkBounds.Bottom < layout.Paths.Where(path => path.Role == "bars").Min(path => path.Bounds.Y));
        Assert.Null(options.HumanReadableText);
        var scanned = SymbolScanner.Scan(layout.Render(OutputFormat.Png, new BarcodePrintPlacement { RotationDegrees = rotation }).ToArray(), new ScanOptions {
            Formats = new[] { SymbolFormat.AustraliaPost }, TimeoutMilliseconds = 5000,
            AustraliaPostCustomerTable = table, AustraliaPostCustomerInfoLength = input.Length - 10
        });
        var result = Assert.Single(scanned.Symbols);
        var metadata = Assert.IsType<AustraliaPostSymbolMetadata>(result.Metadata);
        Assert.Equal(input, metadata.Data.EncodedValue);
        Assert.Equal(input.Substring(2), result.Text);
        Assert.False(metadata.Data.IsCustomerInfoAmbiguous);
        Assert.NotNull(result.Geometry);
    }

    [Theory]
    [InlineData("9956439111", AustraliaPostCustomerEncodingTable.C)]
    [InlineData("1156439111A", AustraliaPostCustomerEncodingTable.C)]
    [InlineData("5956439111ABCDEF", AustraliaPostCustomerEncodingTable.C)]
    [InlineData("5956439111123456789", AustraliaPostCustomerEncodingTable.N)]
    [InlineData("6256439111ABCDEFGHIJK", AustraliaPostCustomerEncodingTable.C)]
    [InlineData("6256439111A", AustraliaPostCustomerEncodingTable.N)]
    public void ExplicitPostalFields_RejectUnsupportedCodesTablesAndCapacity(string input, AustraliaPostCustomerEncodingTable table) =>
        Assert.Throws<ArgumentException>(() => AustraliaPostEncoder.EncodeSymbol(input, table));

    private static string States(BitMatrix modules) => string.Concat(Enumerable.Range(0, (modules.Width + 1) / 2).Select(bar => {
        var top = modules[bar * 2, 0]; var bottom = modules[bar * 2, 7];
        return top && bottom ? '0' : top ? '1' : bottom ? '2' : '3';
    }));
    private static BitMatrix FromStates(string states) {
        var modules = new BitMatrix(states.Length * 2 - 1, 8);
        for (var i = 0; i < states.Length; i++) for (var y = 0; y < 8; y++)
            modules[i * 2, y] = y is 3 or 4 || y < 3 && states[i] is '0' or '1' || y > 4 && states[i] is '0' or '2';
        return modules;
    }
}
