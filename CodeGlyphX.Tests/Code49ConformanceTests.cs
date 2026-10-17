using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Code49;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code49ConformanceTests {
    [Fact]
    public void FullAsciiAndNumericModes_DecodeIndependentBwippAcrossCapacityBoundaries() {
        using var stream = typeof(Code49ConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "code49") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < matrix.Height; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
            Assert.True(Code49Decoder.TryDecode(matrix, out var decoded), value);
            Assert.Equal(value, decoded.Text);
            var encoded = Code49Encoder.Encode(value);
            Assert.True(Code49Decoder.TryDecode(encoded.Modules, out decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(10, count);
    }

    [Fact]
    public void EncodedRows_MatchIndependentZintAndRejectChangedPairPatterns() {
        var encoded = Code49Encoder.Encode("HELLO");
        var hexRows = new[] { "A08CAE23BF76B4DFBC", "B3CBB72620C5B049BC" };
        for (var row = 0; row < hexRows.Length; row++) {
            var bits = string.Concat(hexRows[row].Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0'))).Substring(0, 70);
            Assert.Equal(bits, string.Concat(Enumerable.Range(0, 70).Select(x => encoded.Modules[x, row * 9 + 1] ? '1' : '0')));
        }
        var damaged = encoded.Modules.Clone();
        for (var y = 1; y <= 8; y++) damaged[10, y] = !damaged[10, y];
        Assert.False(Code49Decoder.TryDecode(damaged, out _));
    }
}
