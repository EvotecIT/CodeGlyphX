using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Code16K;
using CodeGlyphX.Code49;
using CodeGlyphX.CodablockF;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class StackedModeConformanceTests {
    [Fact]
    public void IndependentStackedSymbols_PreserveControlModesAndExtendedCharacters() {
        using var stream = typeof(StackedModeConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var bcid = fixture.GetProperty("bcid").GetString();
            if (bcid != "code16k" && bcid != "code49" && bcid != "codablockf") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var modules = Read(fixture);
            var context = fixture.GetRawText().Substring(0, Math.Min(200, fixture.GetRawText().Length));
            if (Flag(fixture, "invalidRowGuards")) {
                Assert.False(Code16KDecoder.TryDecode(modules, out _));
                count++;
                continue;
            }
            if (Flag(fixture, "reservedFunction")) {
                Assert.False(Code49Decoder.TryDecode(modules, out _));
                count++;
                continue;
            }
            var gs1 = Flag(fixture, "gs1");
            BitMatrix encoded;
            if (bcid == "code16k") {
                var options = new Code16KEncodingOptions {
                    IsGs1 = gs1, ReaderInitialization = Flag(fixture, "readerInitialization"),
                    MessageAppend = Flag(fixture, "messageAppend")
                };
                if (fixture.TryGetProperty("index", out var index))
                    options.StructuredAppend = new Code16KStructuredAppend(index.GetInt32(), fixture.GetProperty("total").GetInt32());
                if (Flag(fixture, "concatenation")) options.StructuredAppend = new Code16KStructuredAppend(2, 5, false);
                Assert.True(Code16KDecoder.TryDecode(modules, out var decoded), context);
                Assert.Equal(value, decoded.Text);
                Assert.Equal(gs1, decoded.IsGs1);
                Assert.Equal(options.ReaderInitialization, decoded.ReaderInitialization);
                Assert.Equal(options.MessageAppend, decoded.MessageAppend);
                Assert.Equal(options.StructuredAppend, decoded.StructuredAppend);
                encoded = Code16KEncoder.Encode(value, options).Modules;
                Assert.True(Code16KDecoder.TryDecode(encoded, out decoded), context);
                Assert.Equal(value, decoded.Text);
                Assert.True(SymbolDecoder.TryDecode(modules, out var common, SymbolFormat.Code16K));
                var metadata = Assert.IsType<Code16KSymbolMetadata>(common.Metadata);
                Assert.Equal(options.ReaderInitialization, metadata.ReaderInitialization);
                Assert.Equal(options.StructuredAppend, metadata.StructuredAppend);
            } else if (bcid == "code49") {
                var options = new Code49EncodingOptions { IsGs1 = gs1, IsContinuation = Flag(fixture, "continuation") };
                if (fixture.TryGetProperty("index", out var index))
                    options.StructuredAppend = new Code49StructuredAppend(index.GetInt32(), fixture.GetProperty("total").GetInt32());
                Assert.True(Code49Decoder.TryDecode(modules, out var decoded), context);
                Assert.Equal(value, decoded.Text);
                Assert.Equal(gs1, decoded.IsGs1);
                Assert.Equal(options.IsContinuation, decoded.IsContinuation);
                Assert.Equal(options.StructuredAppend, decoded.StructuredAppend);
                if (Flag(fixture, "decodeOnly")) { count++; continue; }
                encoded = Code49Encoder.Encode(value, options).Modules;
                Assert.True(Code49Decoder.TryDecode(encoded, out decoded), context);
                Assert.Equal(value, decoded.Text);
                Assert.True(SymbolDecoder.TryDecode(modules, out var common, SymbolFormat.Code49));
                var metadata = Assert.IsType<Code49SymbolMetadata>(common.Metadata);
                Assert.Equal(options.IsContinuation, metadata.IsContinuation);
                Assert.Equal(options.StructuredAppend, metadata.StructuredAppend);
            } else {
                Assert.True(CodablockFDecoder.TryDecode(modules, out var decoded), context);
                Assert.Equal(value, decoded.Text);
                Assert.Equal(gs1, decoded.IsGs1);
                encoded = CodablockFEncoder.Encode(value, isGs1: gs1).Modules;
                Assert.True(CodablockFDecoder.TryDecode(encoded, out decoded), context);
                Assert.Equal(value, decoded.Text);
            }
            if (Flag(fixture, "exact")) AssertDataRowsEqual(modules, encoded);
            count++;
        }
        Assert.Equal(21, count);
    }

    [Theory]
    [InlineData(2, 5)]
    [InlineData(106, 107)]
    public void ExtendedEvenBlocks_UseUss16KTableC2RowGuards(int index, int total) {
        var encoded = Code16KEncoder.Encode("HELLO", new Code16KEncodingOptions {
            StructuredAppend = new Code16KStructuredAppend(index, total)
        }).Modules;
        // USS-16K Table C-2: stop values for rows 1..16 are 2,3,4,5,6,7,0,1,6,7,0,1,2,3,4,5.
        // The literal bit patterns follow Table 5's space-bar-space-bar widths.
        var stops = new[] { "0010011", "0111101", "0100011", "0110001", "0101111", "0001011",
            "0001101", "0011001", "0101111", "0001011", "0001101", "0011001", "0010011", "0111101", "0100011", "0110001" };
        for (var row = 0; row < 16; row++)
            Assert.Equal(stops[row], string.Concat(Enumerable.Range(63, 7).Select(x => encoded[x, row * 9 + 1] ? '1' : '0')));
        Assert.True(Code16KDecoder.TryDecode(encoded, out var decoded));
        Assert.Equal("HELLO", decoded.Text);
        Assert.False(decoded.ReaderInitialization);
        Assert.Equal(new Code16KStructuredAppend(index, total), decoded.StructuredAppend);
        var mixed = encoded.Clone();
        // Replacing just one guard with an odd-block guard must fail even when payload checks still agree.
        var odd = Code16KEncoder.Encode("HELLO", new Code16KEncodingOptions {
            StructuredAppend = new Code16KStructuredAppend(1, total)
        }).Modules;
        for (var y = 1; y <= 8; y++) for (var x = 63; x < 70; x++) mixed[x, y] = odd[x, y];
        Assert.False(Code16KDecoder.TryDecode(mixed, out _));
    }

    [Fact]
    public void InvalidSequenceMetadataAndIncompatibleControls_AreRejected() {
        Assert.Throws<ArgumentException>(() => Code16KEncoder.Encode("HELLO", new Code16KEncodingOptions {
            StructuredAppend = new Code16KStructuredAppend(3, 2)
        }));
        Assert.Throws<ArgumentException>(() => Code16KEncoder.Encode("HELLO", new Code16KEncodingOptions {
            StructuredAppend = new Code16KStructuredAppend(1, 2), Rows = 2
        }));
        Assert.Throws<ArgumentException>(() => Code16KEncoder.Encode("HELLO", new Code16KEncodingOptions {
            ReaderInitialization = true, MessageAppend = true
        }));
        Assert.Throws<ArgumentException>(() => Code49Encoder.Encode("HELLO", new Code49EncodingOptions {
            StructuredAppend = new Code49StructuredAppend(1, 10)
        }));
        Assert.Throws<ArgumentException>(() => Code49Encoder.Encode("HELLO", new Code49EncodingOptions {
            IsContinuation = true, StructuredAppend = new Code49StructuredAppend(1, 2)
        }));
    }

    private static bool Flag(JsonElement fixture, string key) => fixture.TryGetProperty(key, out var value) && value.GetBoolean();
    private static BitMatrix Read(JsonElement fixture) {
        var modules = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
        var bits = fixture.GetProperty("modules").GetString()!;
        for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) modules[x, y] = bits[y * modules.Width + x] == '1';
        return modules;
    }
    private static void AssertDataRowsEqual(BitMatrix expected, BitMatrix actual) {
        var expectedRows = Rows(expected);
        Assert.Equal(expectedRows, Rows(actual));
    }
    private static string[] Rows(BitMatrix matrix) {
        // BWIPP supplies its 10-module left margin and one-module right margin; the public grid excludes these.
        var offset = matrix.Width == 81 ? 10 : 0;
        return Enumerable.Range(0, matrix.Height)
            .Select(y => string.Concat(Enumerable.Range(offset, 70).Select(x => matrix[x, y] ? '1' : '0')))
            .Where(row => row.Contains('0')).Distinct().ToArray();
    }
}
