using System;
using System.Linq;
using System.Text;
using CodeGlyphX.Aztec;
using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecConformanceTests {
    [Theory]
    [InlineData("é")]
    [InlineData("Zażółć gęślą jaźń")]
    [InlineData("😀 日本語")]
    public void UnicodeText_RoundTrips(string text) {
        Assert.True(AztecCode.TryDecode(AztecCode.Encode(text), out var decoded));
        Assert.Equal(text, decoded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(32)]
    public void FullSymbols_HaveReferenceGrid_AndDecode(int layers) {
        var modules = AztecCode.Encode("HELLO", new AztecEncodeOptions { Layers = layers, Compact = false });
        var center = modules.Width / 2;
        for (int offset = 0, raw = 0; raw < (14 + layers * 4) / 2; offset += 16, raw += 15) {
            for (var k = center & 1; k < modules.Width; k += 2) {
                Assert.True(modules[center - offset, k]);
                Assert.True(modules[center + offset, k]);
                Assert.True(modules[k, center - offset]);
                Assert.True(modules[k, center + offset]);
            }
        }
        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal("HELLO", text);
    }

    [Theory]
    [InlineData(4, "Zażółć")]
    [InlineData(20, "日本語")]
    [InlineData(25, "Aé😀")]
    [InlineData(26, "Aé😀")]
    public void Eci_TextAndBinaryPathsAgree(int eci, string text) {
        Assert.True(EncodingUtils.TryGetEncoding(eci, out var encoding));
        var options = new AztecEncodeOptions { EciAssignmentNumber = eci };
        Assert.True(AztecCode.TryDecode(AztecCode.Encode(text, options), out var decoded));
        Assert.Equal(text, decoded);
        Assert.True(AztecCode.TryDecode(AztecCode.Encode(encoding.GetBytes(text), options), out decoded));
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void ExplicitLatin1Alias_AgreesWithTextEncoding() {
        var modules = AztecCode.Encode("é", new AztecEncodeOptions { TextEncoding = EncodingUtils.Latin1, EciAssignmentNumber = 1 });
        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal("é", text);
        Assert.Throws<InvalidOperationException>(() => AztecCode.Encode("é", new AztecEncodeOptions { TextEncoding = Encoding.UTF8, EciAssignmentNumber = 3 }));
    }

    [Fact]
    public void IndependentUtf16EciStream_DecodesAcrossTableAndBinaryModes() {
        // ECI 25, BINARY byte 00, table character A, then BINARY bytes 00 E9.
        const string bits = "00000" + "00000" + "010" + "0100" + "0111"
            + "11111" + "00001" + "00000000" + "00010"
            + "11111" + "00010" + "00000000" + "11101001";
        Assert.Equal("Aé", AztecDecoder.GetEncodedData(bits.Select(c => c == '1').ToArray()));
    }

    [Fact]
    public void IndependentLatin2Symbol_DecodesEci() {
        // Generated with ZXing-C++ 3.0.0 (Zint writer), text Zażółć, default charset selection.
        // This fixture is independent of CodeGlyphX's encoder and includes the ECI declaration.
        var rows = new[] {
            "0000001110110010001",
            "0010011000100110101",
            "0110000001110111100",
            "0011101110111101111",
            "0011110100110011110",
            "0100111111111111110",
            "0011010000000100100",
            "1001110111110111010",
            "1110110100010110111",
            "0110010101010101101",
            "1000010100010100110",
            "1100010111110101011",
            "1101010000000100000",
            "1000011111111111100",
            "0010001111100000110",
            "0011100000101010111",
            "1000001000101100000",
            "1101001110101110111",
            "1110011111110101011",
        };
        var modules = new BitMatrix(rows[0].Length, rows.Length);
        for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < rows[y].Length; x++) modules[x, y] = rows[y][x] == '1';
        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal("Zażółć", text);
    }

    [Fact]
    public void InvalidAndUnknownEci_AreNotSilentlyDecodedAsLatin1() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AztecCode.Encode(new byte[] { 65 }, new AztecEncodeOptions { EciAssignmentNumber = 1000000 }));
        var modules = AztecCode.Encode(new byte[] { 65 }, new AztecEncodeOptions { EciAssignmentNumber = 999999 });
        Assert.False(AztecCode.TryDecode(modules, out _));
        Assert.Throws<InvalidOperationException>(() => AztecCode.Encode("é", new AztecEncodeOptions { TextEncoding = Encoding.UTF8, EciAssignmentNumber = 3 }));
    }
}
