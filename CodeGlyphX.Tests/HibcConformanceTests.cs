using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class HibcConformanceTests {
    [Fact]
    public void HibcCarriers_DecodeIndependentBwippPayloadsWithCheckedFraming() {
        using var stream = typeof(HibcConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var carrier = fixture.GetProperty("bcid").GetString() switch {
                "hibccode39" => SymbolFormat.Code39,
                "hibccode128" => SymbolFormat.Code128,
                "hibcdatamatrix" or "hibcdatamatrixrectangular" => SymbolFormat.DataMatrix,
                "hibcqrcode" => SymbolFormat.QrCode,
                "hibcpdf417" => SymbolFormat.Pdf417,
                "hibcmicropdf417" => SymbolFormat.MicroPdf417,
                "hibccodablockf" => SymbolFormat.CodablockF,
                _ => (SymbolFormat?)null
            };
            if (!carrier.HasValue) continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            if (fixture.TryGetProperty("width", out var width)) {
                var matrix = new BitMatrix(width.GetInt32(), fixture.GetProperty("height").GetInt32());
                for (var y = 0; y < matrix.Height; y++)
                    for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
                Assert.True(Hibc.TryDecodeMatrix(carrier.Value, matrix, out var decoded), fixture.GetProperty("bcid").GetString());
                Assert.Equal(value, decoded);
            } else {
                Assert.True(Hibc.TryDecodeLinear(carrier.Value, bits.Select(bit => bit == '1').ToArray(), out var decoded));
                Assert.Equal(value, decoded);
            }
            count++;
        }
        Assert.Equal(8, count);
    }

    [Theory]
    [InlineData(SymbolFormat.Code39)]
    [InlineData(SymbolFormat.Code128)]
    public void HibcLinearEncoding_PreservesLiteralPlusAndCheckedPayload(SymbolFormat carrier) {
        var barcode = Hibc.EncodeLinear(carrier, "A999BJC5D6E71");
        var bits = barcode.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
        Assert.True(Hibc.TryDecodeLinear(carrier, bits, out var value));
        Assert.Equal("A999BJC5D6E71", value);
    }

    [Fact]
    public void HibcRectangularEncoding_UsesExistingDataMatrixOptions() {
        var symbol = Hibc.EncodeDataMatrix("A999BJC5D6E71", new DataMatrixEncodingOptions { Shape = DataMatrixShape.Rectangular });
        Assert.NotEqual(symbol.Width, symbol.Height);
        Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.DataMatrix, symbol.Modules, out var value));
        Assert.Equal("A999BJC5D6E71", value);
        Assert.False(Hibc.TryParse(Hibc.Prepare(value) + "0", out _));
    }

    [Fact]
    public void HibcFraming_RejectsUnsupportedCharactersAndIncorrectCheckCharacters() {
        Assert.Throws<ArgumentException>(() => Hibc.Prepare("lowercase"));
        Assert.False(Hibc.TryParse("+A999BJC5D6E710", out _));
    }
}
