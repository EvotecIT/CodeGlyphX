using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.CodablockF;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodablockFConformanceTests {
    [Fact]
    public void LinkedRows_DecodeIndependentBwippAcrossCharacterSetsAndRows() {
        using var stream = typeof(CodablockFConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "codablockf") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < matrix.Height; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
            Assert.True(CodablockFDecoder.TryDecode(matrix, out var decoded), value);
            Assert.Equal(value, decoded.Text);
            var encoded = CodablockFEncoder.Encode(value);
            Assert.True(CodablockFDecoder.TryDecode(encoded.Modules, out decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(6, count);
    }

    [Fact]
    public void EncodedRows_MatchIndependentZintAndRejectRowCorruption() {
        var encoded = CodablockFEncoder.Encode("HELLO");
        var hexRows = new[] {
            "D097BA866288D11BA37476BBD7BAEF7BAC758",
            "D097BB125DEBDD77AF75DEBDD793A3590C758"
        };
        for (var row = 0; row < hexRows.Length; row++) {
            var bits = string.Concat(hexRows[row].Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0'))).Substring(0, 145);
            Assert.Equal(bits, string.Concat(Enumerable.Range(0, 145).Select(x => encoded.Modules[x, row * 11 + 1] ? '1' : '0')));
        }
        var damaged = encoded.Modules.Clone();
        for (var y = 1; y <= 10; y++) damaged[40, y] = !damaged[40, y];
        Assert.False(CodablockFDecoder.TryDecode(damaged, out _));
    }
}
