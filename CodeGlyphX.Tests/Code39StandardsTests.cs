using System.Collections.Generic;
using CodeGlyphX.Code39;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code39StandardsTests {
    public static IEnumerable<object[]> StandardCharacterPatterns() {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        var patterns = new[] {
            0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064,
            0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C,
            0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016,
            0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8,
            0x0A2, 0x08A, 0x02A
        };

        for (var i = 0; i < alphabet.Length; i++) {
            yield return new object[] { alphabet[i], patterns[i] };
        }
    }

    [Theory]
    [MemberData(nameof(StandardCharacterPatterns))]
    public void Encode_UsesStandardWideNarrowPattern(char character, int expectedPattern) {
        var barcode = Code39Encoder.Encode(character.ToString(), includeChecksum: false, fullAsciiMode: false);
        var modules = ExpandModules(barcode);

        Assert.Equal(38, modules.Length);
        Assert.Equal(ExpandWidePattern(expectedPattern), modules[13..25]);
    }

    [Fact]
    public void Encode_UsesStandardStartStopPattern() {
        var barcode = Code39Encoder.Encode(string.Empty, includeChecksum: false, fullAsciiMode: false);
        var modules = ExpandModules(barcode);
        var expected = ExpandWidePattern(0x094);

        Assert.Equal(25, modules.Length);
        Assert.Equal(expected, modules[..12]);
        Assert.False(modules[12]);
        Assert.Equal(expected, modules[13..]);
    }

    private static bool[] ExpandModules(Barcode1D barcode) {
        var modules = new List<bool>(barcode.TotalModules);
        foreach (var segment in barcode.Segments) {
            for (var i = 0; i < segment.Modules; i++) {
                modules.Add(segment.IsBar);
            }
        }
        return modules.ToArray();
    }

    private static bool[] ExpandWidePattern(int pattern) {
        var modules = new bool[12];
        var offset = 0;
        var isBar = true;

        for (var mask = 1 << 8; mask != 0; mask >>= 1) {
            var width = (pattern & mask) != 0 ? 2 : 1;
            for (var i = 0; i < width; i++) {
                modules[offset++] = isBar;
            }
            isBar = !isBar;
        }

        return modules;
    }
}
