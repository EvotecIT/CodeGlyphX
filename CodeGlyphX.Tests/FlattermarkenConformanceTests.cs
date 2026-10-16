using System.Linq;
using System.Text.Json;
using CodeGlyphX.Flattermarken;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class FlattermarkenConformanceTests {
    [Fact]
    public void CollationCells_MatchIndependentBwippIncludingWhiteEdgeCells() {
        using var stream = typeof(FlattermarkenConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "flattermarken") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = Barcode.Encode(SymbolFormat.Flattermarken, value);
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.Flattermarken, out var decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(3, count);
    }

    [Fact]
    public void CollationCells_RejectMultipleBarsWithinOneDigitCell() {
        Assert.False(FlattermarkenCode.TryDecode(new[] { true, false, true, false, false, false, false, false, false }, out _));
    }
}
