using System;
using System.Globalization;
using System.Numerics;
using CodeGlyphX.Internal.ReedSolomon;

namespace CodeGlyphX.Mailmark;

/// <summary>Encodes and decodes Royal Mail Mailmark C and L four-state symbols.</summary>
public static class MailmarkFourState {
    private const string Classes = "0123456789ABCDE";
    private static readonly int[] GroupsC = { 3, 5, 7, 11, 13, 14, 16, 17, 19, 0, 1, 2, 4, 6, 8, 9, 10, 12, 15, 18, 20, 21 };
    private static readonly int[] GroupsL = { 2, 5, 7, 8, 13, 14, 15, 16, 21, 22, 23, 0, 1, 3, 4, 6, 9, 10, 11, 12, 17, 18, 19, 20, 24, 25 };
    private static readonly int[] EvenSymbols = BuildSymbols(0);
    private static readonly int[] OddSymbols = BuildSymbols(1);

    /// <summary>Encodes a 22-character C or 26-character L application string, including trailing postcode padding.</summary>
    /// <remarks>The result is a logical three-row state grid. The version digit must be 1.</remarks>
    public static MatrixSymbol Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!TryConsolidate(value, out var number)) throw new ArgumentException("Invalid Mailmark C or L application string.", nameof(value));
        var large = value.Length == 26;
        var n30 = large ? 11 : 9;
        var n32 = large ? 8 : 7;
        var checks = large ? 7 : 6;
        var groups = large ? GroupsL : GroupsC;
        var words = new int[groups.Length];
        for (var i = n30 + n32 - 1; i >= 0; i--) {
            var radix = i < n30 ? 30 : 32;
            words[i] = (int)(number % radix);
            number /= radix;
        }
        new ReedSolomonEncoder(GenericGf.Mailmark).Encode(words, checks);
        var matrix = new BitMatrix(groups.Length * 6 - 1, 3);
        for (var i = 0; i < words.Length; i++) {
            var symbol = (i < n30 ? EvenSymbols : OddSymbols)[words[i]];
            var group = groups[i];
            var ascenders = (group & 1) == 0 ? symbol >> 3 : symbol & 7;
            var descenders = (group & 1) == 0 ? symbol & 7 : symbol >> 3;
            for (var bar = 0; bar < 3; bar++) {
                var x = group * 6 + bar * 2;
                matrix[x, 0] = (ascenders & (4 >> bar)) != 0;
                matrix[x, 1] = true;
                matrix[x, 2] = (descenders & (4 >> bar)) != 0;
            }
        }
        return new MatrixSymbol(SymbolFormat.Mailmark4State, matrix);
    }

    /// <summary>Decodes an exact C or L state grid and corrects up to three damaged GF(32) symbols.</summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        value = string.Empty;
        if (modules is null || modules.Height != 3 || (modules.Width != 131 && modules.Width != 155)) return false;
        var large = modules.Width == 155;
        var n30 = large ? 11 : 9;
        var dataCount = large ? 19 : 16;
        var checks = large ? 7 : 6;
        var groups = large ? GroupsL : GroupsC;
        for (var x = 0; x < modules.Width; x++) {
            if ((x & 1) == 0) { if (!modules[x, 1]) return false; }
            else if (modules[x, 0] || modules[x, 1] || modules[x, 2]) return false;
        }
        var words = new int[groups.Length];
        for (var i = 0; i < groups.Length; i++) {
            var group = groups[i];
            var ascenders = 0;
            var descenders = 0;
            for (var bar = 0; bar < 3; bar++) {
                ascenders = (ascenders << 1) | (modules[group * 6 + bar * 2, 0] ? 1 : 0);
                descenders = (descenders << 1) | (modules[group * 6 + bar * 2, 2] ? 1 : 0);
            }
            var symbol = (group & 1) == 0 ? (ascenders << 3) | descenders : (descenders << 3) | ascenders;
            var found = Array.IndexOf(i < n30 ? EvenSymbols : OddSymbols, symbol);
            // An invalid parity pattern is an erasure, but this decoder does not
            // currently supply erasure positions to the shared RS engine.
            if (found < 0) return false;
            words[i] = found;
        }
        try { new ReedSolomonDecoder(GenericGf.Mailmark).Decode(words, checks); }
        catch (ReedSolomonException) { return false; }
        var expected = (int[])words.Clone();
        new ReedSolomonEncoder(GenericGf.Mailmark).Encode(expected, checks);
        for (var i = dataCount; i < words.Length; i++) if (words[i] != expected[i]) return false;
        var number = BigInteger.Zero;
        for (var i = 0; i < dataCount; i++) {
            var radix = i < n30 ? 30 : 32;
            if (words[i] >= radix) return false;
            number = number * radix + words[i];
        }
        var version = (int)(number % 4); number /= 4;
        var format = (int)(number % 5); number /= 5;
        var itemClass = (int)(number % 15); number /= 15;
        var supplyRadix = large ? 1000000 : 100;
        var supply = (int)(number % supplyRadix); number /= supplyRadix;
        var item = (int)(number % 100000000); number /= 100000000;
        if (version != 0 || !MailmarkDestination.TryUnrank(number, out var destination)) return false;
        value = format.ToString(CultureInfo.InvariantCulture) + "1" + Classes[itemClass]
            + supply.ToString(large ? "D6" : "D2", CultureInfo.InvariantCulture)
            + item.ToString("D8", CultureInfo.InvariantCulture) + destination;
        return true;
    }

    private static bool TryConsolidate(string value, out BigInteger number) {
        number = BigInteger.Zero;
        if (value.Length != 22 && value.Length != 26) return false;
        if (value[0] < '0' || value[0] > '4' || value[1] != '1') return false;
        var itemClass = Classes.IndexOf(value[2]);
        if (itemClass < 0) return false;
        var supplyLength = value.Length == 22 ? 2 : 6;
        var supply = 0;
        var item = 0;
        for (var i = 3; i < 3 + supplyLength + 8; i++) {
            if (value[i] < '0' || value[i] > '9') return false;
            if (i < 3 + supplyLength) supply = supply * 10 + value[i] - '0';
            else item = item * 10 + value[i] - '0';
        }
        if (!MailmarkDestination.TryRank(value.Substring(3 + supplyLength + 8), out number)) return false;
        number = (number * 100000000 + item) * (supplyLength == 2 ? 100 : 1000000) + supply;
        number = ((number * 15 + itemClass) * 5 + value[0] - '0') * 4;
        return true;
    }

    private static int[] BuildSymbols(int parity) {
        var result = new int[parity == 0 ? 30 : 32];
        var index = 0;
        for (var value = 1; value < 64; value++) {
            var bits = 0;
            for (var n = value; n != 0; n >>= 1) bits += n & 1;
            // The all-ones even symbol is reserved.
            if ((bits & 1) == parity && value != 63) result[index++] = value;
        }
        return result;
    }
}
