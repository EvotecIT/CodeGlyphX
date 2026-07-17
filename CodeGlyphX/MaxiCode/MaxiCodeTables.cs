// Portions adapted from the Zint backend and ZXing-C++.
// Licensed under BSD-3-Clause and Apache-2.0; see THIRD-PARTY-NOTICES.md.

using System;

namespace CodeGlyphX.MaxiCode;

internal static class MaxiCodeTables {
    internal const int Width = 30;
    internal const int Height = 33;
    internal const int CodewordCount = 144;

    // ISO/IEC 16023 Figure 5 module sequence. Stored as signed little-endian Int16 values.
    private const string ModuleBitsBase64 =
        "eQB4AH8AfgCFAIQAiwCKAJEAkACXAJYAnQCcAKMAogCpAKgArwCuALUAtAC7ALoAwQDAAMcAxgD+//7/ewB6AIEAgACHAIYAjQCMAJMAkgCZAJgAnwCeAKUA" +
        "pACrAKoAsQCwALcAtgC9ALwAwwDCAMkAyAAwA/3/fQB8AIMAggCJAIgAjwCOAJUAlACbAJoAoQCgAKcApgCtAKwAswCyALkAuAC/AL4AxQDEAMsAygAyAzED" +
        "GwEaARUBFAEPAQ4BCQEIAQMBAgH9APwA9wD2APEA8ADrAOoA5QDkAN8A3gDZANgA0wDSAM0AzAAzA/3/HQEcARcBFgERARABCwEKAQUBBAH/AP4A+QD4APMA" +
        "8gDtAOwA5wDmAOEA4ADbANoA1QDUAM8AzgA1AzQDHwEeARkBGAETARIBDQEMAQcBBgEBAQAB+wD6APUA9ADvAO4A6QDoAOMA4gDdANwA1wDWANEA0AA2A/3/" +
        "IQEgAScBJgEtASwBMwEyATkBOAE/AT4BRQFEAUsBSgFRAVABVwFWAV0BXAFjAWIBaQFoAW8BbgE4AzcDIwEiASkBKAEvAS4BNQE0ATsBOgFBAUABRwFGAU0B" +
        "TAFTAVIBWQFYAV8BXgFlAWQBawFqAXEBcAE5A/3/JQEkASsBKgExATABNwE2AT0BPAFDAUIBSQFIAU8BTgFVAVQBWwFaAWEBYAFnAWYBbQFsAXMBcgE7AzoD" +
        "mQGYAZMBkgGNAYwBhwGGAU8ATgD+//7/DQAMACUAJAACAP//LAArAG0AbACBAYABewF6AXUBdAE8A/3/mwGaAZUBlAGPAY4BiQGIAVEAUAAoAP7/DwAOACcA" +
        "JgADAP////8tAG8AbgCDAYIBfQF8AXcBdgE+Az0DnQGcAZcBlgGRAZABiwGKAVMAUgApAP3//f/9//3//f8FAAQALwAuAHEAcACFAYQBfwF+AXkBeAE/A/3/" +
        "nwGeAaUBpAGrAaoBZwBmADcANgAQAP3//f/9//3//f/9//3/FAATAFUAVACxAbABtwG2Ab0BvAFBA0ADoQGgAacBpgGtAawBaQBoADkAOAD9//3//f/9//3/" +
        "/f/9//3/FgAVAFcAVgCzAbIBuQG4Ab8BvgFCA/3/owGiAakBqAGvAa4BawBqADsAOgD9//3//f/9//3//f/9//3//f8XAFkAWAC1AbQBuwG6AcEBwAFEA0MD" +
        "4QHgAdsB2gHVAdQBMAD+/x4A/f/9//3//f/9//3//f/9//3//f8AADUANADPAc4ByQHIAcMBwgFFA/3/4wHiAd0B3AHXAdYBMQD///7//f/9//3//f/9//3/" +
        "/f/9//3//f/9//7////RAdABywHKAcUBxAFHA0YD5QHkAd8B3gHZAdgBMwAyAB8A/f/9//3//f/9//3//f/9//3//f8BAP7/KgDTAdIBzQHMAccBxgFIA/3/" +
        "5wHmAe0B7AHzAfIBYQBgAD0APAD9//3//f/9//3//f/9//3//f8aAFsAWgD5AfgB/wH+AQUCBAJKA0kD6QHoAe8B7gH1AfQBYwBiAD8APgD9//3//f/9//3/" +
        "/f/9//3/HAAbAF0AXAD7AfoBAQIAAgcCBgJLA/3/6wHqAfEB8AH3AfYBZQBkAEEAQAARAP3//f/9//3//f/9//3/EgAdAF8AXgD9AfwBAwICAgkCCAJNA0wD" +
        "LwIuAikCKAIjAiICHQIcAkkASAAgAP3//f/9//3//f/9/woAQwBCAHMAcgAXAhYCEQIQAgsCCgJOA/3/MQIwAisCKgIlAiQCHwIeAksASgD+////BwAGACMA" +
        "IgALAP7/RQBEAHUAdAAZAhgCEwISAg0CDAJQA08DMwIyAi0CLAInAiYCIQIgAk0ATAD+/yEACQAIABkAGAD///7/RwBGAHcAdgAbAhoCFQIUAg8CDgJRA/3/" +
        "NQI0AjsCOgJBAkACRwJGAk0CTAJTAlICWQJYAl8CXgJlAmQCawJqAnECcAJ3AnYCfQJ8AoMCggJTA1IDNwI2Aj0CPAJDAkICSQJIAk8CTgJVAlQCWwJaAmEC" +
        "YAJnAmYCbQJsAnMCcgJ5AngCfwJ+AoUChAJUA/3/OQI4Aj8CPgJFAkQCSwJKAlECUAJXAlYCXQJcAmMCYgJpAmgCbwJuAnUCdAJ7AnoCgQKAAocChgJWA1UD" +
        "1wLWAtEC0ALLAsoCxQLEAr8CvgK5ArgCswKyAq0CrAKnAqYCoQKgApsCmgKVApQCjwKOAokCiAJXA/3/2QLYAtMC0gLNAswCxwLGAsECwAK7AroCtQK0Aq8C" +
        "rgKpAqgCowKiAp0CnAKXApYCkQKQAosCigJZA1gD2wLaAtUC1ALPAs4CyQLIAsMCwgK9ArwCtwK2ArECsAKrAqoCpQKkAp8CngKZApgCkwKSAo0CjAJaA/3/" +
        "3QLcAuMC4gLpAugC7wLuAvUC9AL7AvoCAQMAAwcDBgMNAwwDEwMSAxkDGAMfAx4DJQMkAysDKgNcA1sD3wLeAuUC5ALrAuoC8QLwAvcC9gL9AvwCAwMCAwkD" +
        "CAMPAw4DFQMUAxsDGgMhAyADJwMmAy0DLANdA/3/4QLgAucC5gLtAuwC8wLyAvkC+AL/Av4CBQMEAwsDCgMRAxADFwMWAx0DHAMjAyIDKQMoAy8DLgNfA14D";

    internal static readonly short[] ModuleBitNumbers = DecodeModuleBits();

    internal static readonly byte[] CodeSetFlags = Convert.FromBase64String(
        "BAQEBAQEBAQEBAQEBAUEBAQEBAQEBAQEBAQEBB8fHwQfAgEBAQEBAQEBAQEDAQMDAQEBAQEBAQEBAQMCAgICAgIBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIICAgICAgICAgIEBAQEBAQEBAQEBAEBAQEBAQEBAQEBAQQBAQEBAQEEAQIEAgEBBAQCAgIEAgEEBAICBAICAgQCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEA==");

    internal static readonly byte[] SymbolValues = Convert.FromBase64String(
        "AAECAwQFBgcICQoLDAAODxAREhMUFRYXGBkaHhwdHiMgNSIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTolJicoKTQBAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZGiorLC0uAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaIDYiIyQwMTIzNDU2Nzg5LzAxMjM0NTY3ODkwMTIzNDU2Nzg5JCUlJicoKSorJiwlJyYtLigpJygpKiovKywrLC0tLi8uAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaICEiIyQAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRogISIjJA==");

    // State order: A, B, E, C, D. Indexed [target, current].
    internal static readonly byte[][][] LatchSequences = {
        new[] { Array.Empty<byte>(), new byte[] { 63 }, new byte[] { 58 }, new byte[] { 58 }, new byte[] { 58 } },
        new[] { new byte[] { 63 }, Array.Empty<byte>(), new byte[] { 63 }, new byte[] { 63 }, new byte[] { 63 } },
        new[] { new byte[] { 62, 62 }, new byte[] { 62, 62 }, Array.Empty<byte>(), new byte[] { 62, 62 }, new byte[] { 62, 62 } },
        new[] { new byte[] { 60, 60 }, new byte[] { 60, 60 }, new byte[] { 60, 60 }, Array.Empty<byte>(), new byte[] { 60, 60 } },
        new[] { new byte[] { 61, 61 }, new byte[] { 61, 61 }, new byte[] { 61, 61 }, new byte[] { 61, 61 }, Array.Empty<byte>() }
    };

    internal static int FlagForState(int state) => state switch {
        0 => 0x01, // A
        1 => 0x02, // B
        2 => 0x04, // E
        3 => 0x08, // C
        4 => 0x10, // D
        _ => 0
    };

    internal static bool CanEncode(int state, byte value) => (CodeSetFlags[value] & FlagForState(state)) != 0;

    internal static byte SymbolForState(int state, byte value) {
        var flag = FlagForState(state);
        var flags = CodeSetFlags[value];
        if (flags == flag || state == 0) return SymbolValues[value];
        if (state == 1) {
            const string SharedPunctuation = " ,./:";
            var index = SharedPunctuation.IndexOf((char)value);
            if (index >= 0) return (byte)(47 + index);
        }
        if (state == 2 && value is >= 28 and <= 30) return (byte)(value + 4);
        return value == (byte)' ' ? (byte)59 : value;
    }

    internal static bool TryDecodeSymbol(int state, int symbol, out byte value) {
        for (var candidate = 0; candidate < 256; candidate++) {
            var b = (byte)candidate;
            if (CanEncode(state, b) && SymbolForState(state, b) == symbol) {
                value = b;
                return true;
            }
        }
        value = 0;
        return false;
    }

    internal static int GetShiftTarget(int state, int symbol) {
        if (state is 0 or 1) {
            if (symbol == 59) return state == 0 ? 1 : 0;
            if (symbol == 60) return 3;
            if (symbol == 61) return 4;
            if (symbol == 62) return 2;
        } else if (state == 2) {
            if (symbol == 60) return 3;
            if (symbol == 61) return 4;
        } else if (state == 3) {
            if (symbol == 61) return 4;
            if (symbol == 62) return 2;
        } else if (state == 4) {
            if (symbol == 60) return 3;
            if (symbol == 62) return 2;
        }
        return -1;
    }

    internal static int GetShiftSymbol(int currentState, int targetState) {
        if (currentState is 0 or 1) {
            if ((currentState == 0 && targetState == 1) || (currentState == 1 && targetState == 0)) return 59;
            if (targetState == 3) return 60;
            if (targetState == 4) return 61;
            if (targetState == 2) return 62;
        } else if (currentState == 2) {
            if (targetState == 3) return 60;
            if (targetState == 4) return 61;
        } else if (currentState == 3) {
            if (targetState == 4) return 61;
            if (targetState == 2) return 62;
        } else if (currentState == 4) {
            if (targetState == 3) return 60;
            if (targetState == 2) return 62;
        }
        return -1;
    }

    private static short[] DecodeModuleBits() {
        var bytes = Convert.FromBase64String(ModuleBitsBase64);
        if (bytes.Length != Width * Height * 2) throw new InvalidOperationException("Invalid MaxiCode module table.");
        var result = new short[Width * Height];
        for (var i = 0; i < result.Length; i++) {
            result[i] = unchecked((short)(bytes[i * 2] | bytes[i * 2 + 1] << 8));
        }
        return result;
    }
}
