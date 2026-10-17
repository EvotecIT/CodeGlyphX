using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class HibcConformanceTests {
    [Fact]
    public void HibcMicroPdfEncoding_MatchesIndependentZintForEveryColumnCount() {
        using var stream = typeof(HibcConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.zint-micropdf.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var text = fixture.GetProperty("text").GetString()!;
            var matrix = MicroPdf417Encoder.Encode(Hibc.Prepare(text), new MicroPdf417EncodeOptions {
                Columns = fixture.GetProperty("columns").GetInt32(), Rows = fixture.GetProperty("height").GetInt32(),
                Compaction = Pdf417Compaction.Text
            });
            Assert.Equal(fixture.GetProperty("width").GetInt32(), matrix.Width);
            Assert.Equal(fixture.GetProperty("modules").GetString(), string.Concat(Enumerable.Range(0, matrix.Height)
                .SelectMany(y => Enumerable.Range(0, matrix.Width).Select(x => matrix[x, y] ? '1' : '0'))));
            Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.MicroPdf417, matrix, out var decoded));
            Assert.Equal(text, decoded);
            count++;
        }
        Assert.Equal(12, count);
    }

    [Fact]
    public void MicroPdfInitialMode_DecodesIndependentImplicitByteSymbols() {
        using var stream = typeof(HibcConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "micropdf417-default-byte") continue;
            var modules = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            var bits = fixture.GetProperty("modules").GetString()!;
            for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) modules[x, y] = bits[y * modules.Width + x] == '1';
            Assert.True(MicroPdf417Decoder.TryDecode(modules, out var text), fixture.GetProperty("text").GetString());
            Assert.Equal(fixture.GetProperty("text").GetString(), text);
            count++;
        }
        Assert.Equal(2, count);
    }

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
