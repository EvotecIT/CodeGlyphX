using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.PosiCode;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PosiCodeConformanceTests {
    [Fact]
    public void AllGapVariantsAndLinkedRows_ConformToIndependentCorrectedBwipp() {
        using var stream = typeof(PosiCodeConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "posicode") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var variant = Enum.Parse<PosiCodeVariant>(fixture.GetProperty("version").GetString()!, true);
            var rows = fixture.GetProperty("rows").GetInt32();
            var gs1 = fixture.TryGetProperty("gs1", out var flag) && flag.GetBoolean();
            var bits = fixture.GetProperty("modules").GetString()!;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < matrix.Height; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
            var context = $"{variant}/{rows}: {JsonSerializer.Serialize(value)}";
            Assert.True(PosiCodeDecoder.TryDecode(matrix, out var decoded), context);
            Assert.Equal(value, decoded.Text);
            Assert.Equal(variant, decoded.Variant);
            Assert.Equal(gs1, decoded.IsGs1);
            BitMatrix encoded;
            if (rows == 1) {
                var barcode = PosiCodeEncoder.Encode(value, variant, gs1);
                var modules = barcode.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
                Assert.True(PosiCodeDecoder.TryDecode(modules, out decoded), context);
                encoded = new BitMatrix(modules.Length, 1);
                for (var x = 0; x < modules.Length; x++) encoded[x, 0] = modules[x];
            } else encoded = PosiCodeEncoder.EncodeStacked(value, rows, variant, gs1, rowHeight: 1).Modules;
            Assert.True(PosiCodeDecoder.TryDecode(encoded, out decoded), context);
            Assert.Equal(value, decoded.Text);
            if (fixture.GetProperty("exact").GetBoolean()) {
                Assert.Equal(matrix.Width, encoded.Width);
                Assert.Equal(matrix.Height, encoded.Height);
                Assert.Equal(bits, string.Concat(Enumerable.Range(0, encoded.Height).SelectMany(y =>
                    Enumerable.Range(0, encoded.Width).Select(x => encoded[x, y] ? '1' : '0'))));
            }
            count++;
        }
        Assert.Equal(46, count);
    }

    [Fact]
    public void CorruptionAndRowReordering_AreRejectedByMandatoryChecks() {
        var encoded = PosiCodeEncoder.EncodeStacked("ABCDEFGHIJKLMNO", rows: 3, rowHeight: 1).Modules;
        var reordered = encoded.Clone();
        for (var x = 0; x < reordered.Width; x++) {
            reordered[x, 0] = encoded[x, 3];
            reordered[x, 3] = encoded[x, 0];
        }
        Assert.False(PosiCodeDecoder.TryDecode(reordered, out _));
        var bits = PosiCodeEncoder.Encode("ABC123").Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
        bits[30] = !bits[30];
        Assert.False(PosiCodeDecoder.TryDecode(bits, out _));
        Assert.Throws<ArgumentException>(() => PosiCodeEncoder.Encode("abc", PosiCodeVariant.LimitedA));
        Assert.Throws<ArgumentException>(() => PosiCodeEncoder.Encode("\u0100"));
    }
}
