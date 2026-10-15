using System;
using System.Linq;
using CodeGlyphX.Postal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PostalMarkConformanceTests {
    [Theory]
    [InlineData("A", "A0828")]
    [InlineData("B", "8A288")]
    [InlineData("C", "A2228")]
    [InlineData("D", "A88A8")]
    [InlineData("E", "88088")]
    public void FacingMarks_MatchAndDecodeIndependentZintPatterns(string value, string hex) {
        // Zint 2.13.0: zint -b FIM -d <letter> --dump; three trailing pad bits.
        var bits = string.Concat(hex.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0'))).Substring(0, 17);
        var barcode = Barcode.Encode(SymbolFormat.FacingIdentificationMark, value);
        Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
        Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.FacingIdentificationMark, out var decoded));
        Assert.Equal(value, decoded.Text);
    }

    [Fact]
    public void DaftStates_MatchIndependentZintAndDecodeViaUnifiedModuleFacade() {
        // Zint 2.13.0: zint -b DAFT -d DAFT --dump => 28 / AA / 88, seven columns.
        var external = new BitMatrix(7, 3);
        var rows = new[] { "0010100", "1010101", "1000100" };
        for (var y = 0; y < 3; y++) for (var x = 0; x < 7; x++) external[x, y] = rows[y][x] == '1';
        var symbol = DaftCode.Encode("DAFT");
        for (var y = 0; y < 3; y++) for (var x = 0; x < 7; x++) Assert.Equal(external[x, y], symbol.Modules[x, y]);
        Assert.True(SymbolDecoder.TryDecode(external, out var decoded, SymbolFormat.Daft));
        Assert.Equal("DAFT", decoded.Text);
        Assert.Equal(SymbolFormat.Daft, decoded.Format);
        external[0, 1] = false;
        Assert.False(DaftCode.TryDecode(external, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("DAFX")]
    [InlineData("daft")]
    public void DaftStates_RejectUnsupportedStateStrings(string value) {
        Assert.Throws<ArgumentException>(() => DaftCode.Encode(value));
    }
}
