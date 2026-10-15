using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Mailmark;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MailmarkConformanceTests {
    [Fact]
    public void All2DSizes_DecodeIndependentHeadersAndRespectBasicC40AndCapacity() {
        using var stream = typeof(MailmarkConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "mailmark") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var type = (Mailmark2DType)fixture.GetProperty("type").GetInt32();
            var bits = fixture.GetProperty("modules").GetString()!;
            var external = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < external.Height; y++)
                for (var x = 0; x < external.Width; x++) external[x, y] = bits[y * external.Width + x] == '1';
            Assert.True(Mailmark2D.TryDecode(external, out var parsed, out var actualType), $"{type}: {value}");
            Assert.Equal(type, actualType);
            Assert.Equal(value, parsed.Text);
            var encoded = Mailmark2D.Encode(value, type);
            Assert.Equal(external.Width, encoded.Width);
            Assert.Equal(external.Height, encoded.Height);
            Assert.True(Mailmark2D.TryDecode(encoded.Modules, out parsed, out actualType));
            Assert.Equal(value, parsed.Text);
            Assert.Equal("1001234", parsed.SupplyChainId);
            Assert.Equal("12345678", parsed.ItemId);
            Assert.Equal("AB19XY1A", parsed.Destination);
            count++;
        }
        Assert.Equal(8, count);
        var header = Mailmark2D.Prepare("1001234", "12345678", "AB19XY1A");
        Assert.Equal(45, header.Length);
        var ascii = DataMatrixEncoder.Encode(header, new DataMatrixEncodingOptions { Mode = DataMatrixEncodingMode.Ascii, Rows = 24, Columns = 24 });
        Assert.True(DataMatrixDecoder.TryDecode(ascii, out _));
        Assert.False(Mailmark2D.TryDecode(ascii, out _, out _));
        Assert.Throws<ArgumentException>(() => Mailmark2D.Encode(header + new string('A', 7), Mailmark2DType.Type7));
        Assert.Throws<ArgumentException>(() => Mailmark2D.Prepare("123", "12345678", "AB19XY1A"));
        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.EncodeBytes(new byte[3], new DataMatrixEncodingOptions { C40BasicHeaderLength = 3 }));
    }

    [Fact]
    public void CAndLApplications_ConformToIndependentBwippAndRoyalMailExamples() {
        using var stream = typeof(MailmarkConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "mailmark4state") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var expected = fixture.GetProperty("modules").GetString()!;
            var encoded = MailmarkFourState.Encode(value).Modules;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), 3);
            for (var y = 0; y < 3; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = expected[y * matrix.Width + x] == '1';
            Assert.Equal(expected, string.Concat(Enumerable.Range(0, 3).SelectMany(y =>
                Enumerable.Range(0, encoded.Width).Select(x => encoded[x, y] ? '1' : '0'))));
            Assert.True(MailmarkFourState.TryDecode(matrix, out var decoded), value);
            Assert.Equal(value, decoded);
            Assert.Equal(value, SymbolDecoder.Decode(matrix, SymbolFormat.Mailmark4State).Text);
            count++;
        }
        Assert.Equal(17, count);
    }

    [Theory]
    [InlineData("21B2254800659JW5O9QA6Y")]
    [InlineData("41038422416563762EF61AH8T ")]
    public void ValidParitySymbolDamage_IsCorrectedAndInvalidFieldsAreRejected(string value) {
        var damaged = MailmarkFourState.Encode(value).Modules.Clone();
        damaged[0, 0] = !damaged[0, 0];
        damaged[2, 0] = !damaged[2, 0];
        Assert.True(MailmarkFourState.TryDecode(damaged, out var decoded));
        Assert.Equal(value, decoded);
        Assert.Throws<ArgumentException>(() => MailmarkFourState.Encode("51" + value.Substring(2)));
        Assert.Throws<ArgumentException>(() => MailmarkFourState.Encode(value.Substring(0, 1) + "2" + value.Substring(2)));
        Assert.Throws<ArgumentException>(() => MailmarkFourState.Encode(value.Substring(0, value.Length - 9) + "XY12     "));
    }
}
