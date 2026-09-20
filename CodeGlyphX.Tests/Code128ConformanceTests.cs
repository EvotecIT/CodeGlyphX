using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Code128;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code128ConformanceTests {
    [Fact]
    public void SetA_UsesStandardCodewordValues() {
        Assert.Equal(new[] { 103, 65, 33, 34, 35, 64, 106 }, Code128Encoder.EncodeCodeValues("\u0001ABC"));
        var expected = new string(Enumerable.Range(32, 64).Concat(Enumerable.Range(0, 32)).Select(v => (char)v).ToArray());
        AssertDecoded(expected, new[] { 103 }.Concat(Enumerable.Range(0, 96)).ToArray());
    }

    [Theory]
    [InlineData("a\u0001b", new[] { 104, 65, 98, 65, 66 })]
    [InlineData("AaB", new[] { 103, 33, 98, 65, 34 })]
    [InlineData("á", new[] { 104, 100, 65 })]
    [InlineData("áâcã", new[] { 104, 100, 100, 65, 66, 100, 67, 67 })]
    [InlineData("áa", new[] { 104, 100, 100, 65, 100, 100, 65 })]
    [InlineData("\u0081", new[] { 103, 101, 65 })]
    [InlineData("\u0081a", new[] { 104, 100, 98, 65, 65 })]
    [InlineData("A\u001dB", new[] { 104, 33, 102, 34 })]
    [InlineData("\u007f", new[] { 104, 95 })]
    public void ExternalControlSequences_PreserveMeaning(string expected, int[] codewords) => AssertDecoded(expected, codewords);

    [Fact]
    public void Gs1_RequiresLeadingFnc1_AndPreservesLaterSeparators() {
        Assert.True(BarcodeDecoder.TryDecode(Modules(104, 102, 33, 102, 34), BarcodeType.GS1_128, out var gs1));
        Assert.Equal("A\u001dB", gs1.Text);
        AssertDecoded("A\u001dB", new[] { 104, 33, 102, 34 });
    }

    [Theory]
    [InlineData("\u001dABC")]
    [InlineData("ABC\u007f")]
    [InlineData("\u0000XYZ\u001f")]
    public void AsciiEncoder_RoundTripsControlAndDelCharacters(string text) {
        var codes = Code128Encoder.EncodeCodeValues(text);
        AssertDecoded(text, codes.Take(codes.Length - 2).ToArray());
    }

    [Theory]
    [InlineData(new[] { 104, 65, 98 })]
    [InlineData(new[] { 104, 65, 100 })]
    [InlineData(new[] { 104, 98, 99, 10 })]
    public void IncompleteOrInvalidControls_AreRejected(int[] codewords) {
        Assert.False(BarcodeDecoder.TryDecode(Modules(codewords), BarcodeType.Code128, out _));
    }

    [Fact]
    public void MissingStop_IsRejected() {
        var modules = Modules(104, 33, 34);
        Assert.False(BarcodeDecoder.TryDecode(modules.Take(modules.Length - 13).ToArray(), BarcodeType.Code128, out _));
    }

    private static void AssertDecoded(string expected, int[] codes) {
        Assert.True(BarcodeDecoder.TryDecode(Modules(codes), BarcodeType.Code128, out var result));
        Assert.Equal(BarcodeType.Code128, result.Type);
        Assert.Equal(expected, result.Text);
    }

    // Input codewords are independent ISO/IEC 15417 vectors, not produced by the encoder under test.
    private static bool[] Modules(params int[] data) {
        var codes = data.ToList();
        var checksum = data[0];
        for (var i = 1; i < data.Length; i++) checksum += i * data[i];
        codes.Add(checksum % 103);
        codes.Add(106);
        var modules = new List<bool>();
        foreach (var code in codes) {
            var pattern = Code128Tables.GetPattern(code);
            var black = true;
            for (var n = code == 106 ? 6 : 5; n >= 0; n--) {
                var width = (int)((pattern >> (4 * n)) & 15);
                modules.AddRange(Enumerable.Repeat(black, width));
                black = !black;
            }
        }
        return modules.ToArray();
    }
}
