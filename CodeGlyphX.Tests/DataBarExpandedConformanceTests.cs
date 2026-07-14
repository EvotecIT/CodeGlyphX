using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataBarExpandedConformanceTests {
    // These module fixtures were generated independently with Zint 2.13.0 and
    // checked against the ISO/IEC 24724 symbol-character and checksum rules.
    public static IEnumerable<object[]> LinearFixtures {
        get {
            yield return Fixture("(01)12345678901231", "42 98 2F F0 B0 EE 5C 6C 4B C0 E3 C2 B2 87 D8 FC 28");
            yield return Fixture("(90)ABC123", "5E 22 2F F0 A2 20 D7 E7 AB C0 E3 BE 56 C5 F8 FC 2D 08 44");
            yield return Fixture("(01)98898765432106(3103)000001", "51 8E 2F F0 A2 70 DB EC 4B C0 EF 4C 76 7C 48 FC 28 0C 54");
            yield return Fixture("(01)98898765432106(3202)001234", "5C 98 2F F0 A1 E2 9B EC 4B C0 EF 4C 76 7C 48 FC 28 E6 84");
            yield return Fixture("(01)98898765432106(3922)12345", "4B 06 6F F0 B0 6C 9A 06 2B F0 E9 C0 94 FB 98 FC 2B 0B E5 C8 42 FF 3A");
            yield return Fixture("(01)98898765432106(3932)84012345", "4C 2C 6F F0 AC 13 9A 06 2B F0 E9 C0 94 FB 98 FC 2D 97 84 F8 BA FF 3B 86 35");
            yield return Fixture("(01)98898765432106(3103)000001(11)260714", "48 E8 6F F0 BB 10 9A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3203)000001(11)260714", "4C 22 EF F0 AC 43 9A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3103)000001(13)260714", "48 2E 6F F0 B2 C1 9A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3203)000001(13)260714", "48 58 EF F0 A9 E0 9A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3103)000001(15)260714", "46 C8 6F F0 B0 6D 1A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3203)000001(15)260714", "45 C3 2F F0 B8 65 1A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3103)000001(17)260714", "48 51 EF F0 A1 97 1A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
            yield return Fixture("(01)98898765432106(3203)000001(17)260714", "4C 1B 2F F0 B1 1B 1A 06 2B F0 E9 C0 94 FB 98 FC 2B C8 46 50 62 FF 39 33 C5");
        }
    }

    [Theory]
    [MemberData(nameof(LinearFixtures))]
    public void LinearSymbol_MatchesIndependentModuleFixture(string input, string expectedModules) {
        var barcode = DataBarExpandedEncoder.EncodeExpanded(input);

        Assert.Equal(expectedModules, Dump(barcode));
        Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(barcode, out var decoded));
        Assert.Equal(Gs1.ElementString(input), decoded);
    }

    [Fact]
    public void StackedSymbol_MatchesIndependentModuleFixture() {
        const string input = "(01)98898765432106(3103)000001";
        const string expected =
            "51 8E 2F F0 A2 70 DB EC 4B C0 EF 4C 74\n" +
            "0E 71 D0 0A 5D 8F 24 13 B4 2A 10 B3 80\n" +
            "05 55 55 55 55 55 55 55 55 55 55 55 40\n" +
            "03 07 6A 05 2F E7 40 00 00 00 00 00 00\n" +
            "2C F8 91 F8 50 18 A8 00 00 00 00 00 00";

        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked(input, columns: 2);

        Assert.Equal(102, matrix.Width);
        Assert.Equal(5, matrix.Height);
        Assert.Equal(expected, Dump(matrix));
        Assert.True(DataBarExpandedDecoder.TryDecodeExpandedStacked(matrix, out var decoded));
        Assert.Equal(Gs1.ElementString(input), decoded);
    }

    [Fact]
    public void StackedSymbol_OddPartialBlock_RoundTripsWithoutUnnecessaryPadding() {
        var input = "(91)" + new string('A', 20);

        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked(input, columns: 4);

        Assert.Equal(200, matrix.Width);
        Assert.Equal(5, matrix.Height);
        Assert.True(DataBarExpandedDecoder.TryDecodeExpandedStacked(matrix, out var decoded));
        Assert.Equal(Gs1.ElementString(input), decoded);
    }

    [Fact]
    public void LinearSymbol_SupportsMaximumAlphabeticCapacity() {
        var maximum = "(91)" + new string('A', 39);
        var tooLong = "(91)" + new string('A', 40);

        var barcode = DataBarExpandedEncoder.EncodeExpanded(maximum);

        Assert.Equal(543, barcode.TotalModules);
        Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(barcode, out var decoded));
        Assert.Equal(Gs1.ElementString(maximum), decoded);
        Assert.Throws<ArgumentException>(() => DataBarExpandedEncoder.EncodeExpanded(tooLong));
    }

    [Theory]
    [InlineData("(91)Hello World!")]
    [InlineData("(10)abc_123(17)260714")]
    [InlineData("(90)12345678901234567890")]
    public void GeneralField_RoundTripsSupportedIso646Characters(string input) {
        var barcode = DataBarExpandedEncoder.EncodeExpanded(input);

        Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(barcode, out var decoded));
        Assert.Equal(Gs1.ElementString(input), decoded);
    }

    [Fact]
    public void GtinMethod_RejectsInvalidCheckDigit() {
        Assert.Throws<FormatException>(() => DataBarExpandedEncoder.EncodeExpanded("(01)12345678901230"));
    }

    private static object[] Fixture(string input, string modules) => new object[] { input, modules };

    private static string Dump(Barcode1D barcode) {
        var bits = new List<bool>(barcode.TotalModules);
        foreach (var segment in barcode.Segments) {
            for (var i = 0; i < segment.Modules; i++) bits.Add(segment.IsBar);
        }
        return Dump(bits);
    }

    private static string Dump(BitMatrix matrix) {
        var rows = new string[matrix.Height];
        for (var y = 0; y < matrix.Height; y++) {
            var bits = new bool[matrix.Width];
            for (var x = 0; x < matrix.Width; x++) bits[x] = matrix[x, y];
            rows[y] = Dump(bits);
        }
        return string.Join("\n", rows);
    }

    private static string Dump(IReadOnlyList<bool> bits) {
        var output = new StringBuilder();
        for (var offset = 0; offset < bits.Count; offset += 8) {
            if (output.Length > 0) output.Append(' ');
            var count = Math.Min(8, bits.Count - offset);
            var value = 0;
            for (var i = 0; i < count; i++) {
                value = (value << 1) | (bits[offset + i] ? 1 : 0);
            }

            if (count <= 4) {
                value <<= 4 - count;
                output.Append(value.ToString("X1"));
            } else {
                value <<= 8 - count;
                output.Append(value.ToString("X2"));
            }
        }
        return output.ToString();
    }
}
