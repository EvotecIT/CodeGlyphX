using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CodeGlyphX.Gs1Composite;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Gs1CompositeCompactionTests {
    [Fact]
    public void CompressedMethods_DecodeIndependentAllComponentKinds() {
        using var stream = typeof(Gs1CompositeCompactionTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-composite-compaction.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var records = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(54, records.Length);
        foreach (var record in records) {
            var parts = record.TrimEnd('\r').Split('|');
            var expectedType = parts[0] == "a" ? Gs1CompositeComponent.CcA
                : parts[0] == "b" ? Gs1CompositeComponent.CcB : Gs1CompositeComponent.CcC;
            var width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var height = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var grid = new BitMatrix(width, height);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++) grid[x, y] = parts[4][y * width + x] == '1';
            Assert.True(CompositeComponentCodec.TryDecode(grid, out var independentBits, out var type), parts[0] + parts[1]);
            Assert.Equal(expectedType, type);
            Assert.True(CompositeBitStreamCodec.TryDecode(independentBits, out var decoded), parts[0] + parts[1]);
            var expected = Gs1.ElementString(parts[1]);
            Assert.Equal(expected, decoded);
            Assert.True(Gs1CompositeComponents.TryDecode(grid, out var publicText, out var publicType));
            Assert.Equal(expected, publicText);
            Assert.Equal(expectedType, publicType);
            Assert.True(SymbolDecoder.TryDecode(grid, out var result, SymbolFormat.Gs1Composite));
            var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(result.Metadata);
            Assert.True(metadata.IsStandalone);
            Assert.Null(metadata.Carrier);
            Assert.Equal(expected, result.Text);
            var encoded = CompositeBitStreamCodec.Encode(expected, expectedType, 4, 145, out var columns, out var level);
            var ai90 = parts[1].StartsWith("(90)", StringComparison.Ordinal);
            var generalOnly = parts[1].StartsWith("(90)0", StringComparison.Ordinal)
                || parts[1].StartsWith("(90)1234", StringComparison.Ordinal);
            Assert.Equal(!generalOnly, encoded[0]);
            if (!generalOnly) Assert.Equal(ai90, encoded[1]);
            if (expectedType != Gs1CompositeComponent.CcC
                && (parts[1] == "(17)251231" || parts[1] == "(90)1BABCDEF"
                    || parts[1] == "(90)123A1234567890" || parts[1] == "(90)B12AB"))
                Assert.Equal(independentBits, encoded);
            Assert.True(CompositeBitStreamCodec.TryDecode(encoded, out decoded));
            Assert.Equal(expected, decoded);
            var component = CompositeComponentCodec.Encode(encoded, expectedType, columns, level);
            Assert.True(CompositeComponentCodec.TryDecode(component, out var recovered, out _));
            Assert.Equal(encoded, recovered);
        }
    }

    [Fact]
    public void StandaloneComponents_RespectColumnsAndRetainStandaloneMetadata() {
        foreach (var type in new[] { Gs1CompositeComponent.CcA, Gs1CompositeComponent.CcB, Gs1CompositeComponent.CcC }) {
            for (var columns = type == Gs1CompositeComponent.CcC ? 1 : 2; columns <= (type == Gs1CompositeComponent.CcC ? 30 : 4); columns++) {
                var symbol = Gs1CompositeComponents.Encode("(17)251231", type, columns);
                Assert.Equal(columns, symbol.Columns);
                Assert.Equal(type, symbol.Component);
                Assert.True(Gs1CompositeComponents.TryDecode(symbol.Modules, out var text, out var decodedType));
                Assert.Equal("17251231", text);
                Assert.Equal(type, decodedType);
                Assert.False(Gs1CompositeDecoder.TryDecode(symbol.Modules, out _));
            }
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Gs1CompositeComponents.Encode("(17)251231", Gs1CompositeComponent.Auto));
        Assert.Throws<ArgumentOutOfRangeException>(() => Gs1CompositeComponents.Encode("(17)251231", Gs1CompositeComponent.CcA, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Gs1CompositeComponents.Encode("(17)251231", Gs1CompositeComponent.CcC, 31));
        var larger = "(91)" + new string('A', 80);
        Assert.Throws<ArgumentException>(() => Gs1CompositeComponents.Encode(larger, Gs1CompositeComponent.CcC, 1));
        Assert.Equal(4, Gs1CompositeComponents.Encode(larger, Gs1CompositeComponent.CcC, 4).Columns);
    }

    [Theory]
    [InlineData("(17)251231")]
    [InlineData("(11)240229(10)ABC123")]
    [InlineData("(10)123456")]
    [InlineData("(17)251231(21)123456")]
    [InlineData("(90)1BABCDEF")]
    [InlineData("(90)ABCDEF(21)SERIAL123")]
    [InlineData("(90)12BHELLO(8004)0123456ASSET123")]
    public void PublicCompositeWorkflow_PreservesCompactedData(string text) {
        var symbol = Gs1CompositeEncoder.Encode("(01)03212345678906", text);
        Assert.True(Gs1CompositeDecoder.TryDecode(symbol.Modules, out var result));
        Assert.Contains(Gs1.ElementString(text), result.Text);
    }

    [Theory]
    [InlineData(40000)]
    [InlineData(9279)] // YY=24, February 31
    public void InvalidCompressedDate_IsRejected(int packedDate) {
        var bits = new bool[78];
        bits[0] = true; // method 10
        for (var i = 0; i < 16; i++) bits[2 + i] = (packedDate & (1 << (15 - i))) != 0;
        Assert.False(CompositeBitStreamCodec.TryDecode(bits, out _));
    }
}
