using System;
using System.Collections.Generic;

namespace CodeGlyphX.AustraliaPost;

internal static class AustraliaPostTables {
    public const int BarcodeHeight = 8;
    public const int TrackerRowTop = 3;
    public const int TrackerRowBottom = 4;

    public const int FccStandard = 11;
    public const int FccCustomer2 = 59;
    public const int FccCustomer3 = 62;

    public const int StartBarAscender = 1;
    public const int StartBarTracker = 3;
    public const int StopBarAscender = 1;
    public const int StopBarTracker = 3;

    public static readonly int[][] NEncodePairs = {
        new[] { 0, 0 },
        new[] { 0, 1 },
        new[] { 0, 2 },
        new[] { 1, 0 },
        new[] { 1, 1 },
        new[] { 1, 2 },
        new[] { 2, 0 },
        new[] { 2, 1 },
        new[] { 2, 2 },
        new[] { 3, 0 }
    };

    public static readonly int[] NDecodePairs = BuildNDecodePairs();
    public static readonly Dictionary<char, int> CEncodeValues = BuildCEncodeValues();
    public static readonly char[] CDecodeValues = BuildCDecodeValues();

    public static readonly int[] GfExp = BuildGfExp();
    public static readonly int[] GfLog = BuildGfLog();
    public static readonly int[] Generator = BuildGenerator();

    private static int[] BuildNDecodePairs() {
        var map = new int[16];
        for (var i = 0; i < map.Length; i++) map[i] = -1;
        for (var digit = 0; digit < NEncodePairs.Length; digit++) {
            var pair = NEncodePairs[digit];
            map[pair[0] * 4 + pair[1]] = digit;
        }
        return map;
    }

    private static Dictionary<char, int> BuildCEncodeValues() {
        var map = new Dictionary<char, int>(64);
        foreach (var entry in GetCEntries()) {
            var value = BarDigitsToValue(entry.barDigits);
            map[entry.symbol] = value;
        }
        return map;
    }

    private static char[] BuildCDecodeValues() {
        var map = new char[64];
        foreach (var entry in GetCEntries()) {
            var value = BarDigitsToValue(entry.barDigits);
            map[value] = entry.symbol;
        }
        return map;
    }

    private static (char symbol, string barDigits)[] GetCEntries() {
        return new[] {
            ('A', "000"), ('B', "001"), ('C', "002"), ('D', "010"), ('E', "011"), ('F', "012"),
            ('G', "020"), ('H', "021"), ('I', "022"), ('J', "100"), ('K', "101"), ('L', "102"),
            ('M', "110"), ('N', "111"), ('O', "112"), ('P', "120"), ('Q', "121"), ('R', "122"),
            ('S', "200"), ('T', "201"), ('U', "202"), ('V', "210"), ('W', "211"), ('X', "212"),
            ('Y', "220"), ('Z', "221"),
            ('a', "023"), ('b', "030"), ('c', "031"), ('d', "032"), ('e', "033"), ('f', "103"),
            ('g', "113"), ('h', "123"), ('i', "130"), ('j', "131"), ('k', "132"), ('l', "133"),
            ('m', "203"), ('n', "213"), ('o', "223"), ('p', "230"), ('q', "231"), ('r', "232"),
            ('s', "233"), ('t', "303"), ('u', "313"), ('v', "323"), ('w', "330"), ('x', "331"),
            ('y', "332"), ('z', "333"),
            ('0', "222"), ('1', "300"), ('2', "301"), ('3', "302"), ('4', "310"), ('5', "311"),
            ('6', "312"), ('7', "320"), ('8', "321"), ('9', "322"),
            (' ', "003"), ('#', "013")
        };
    }

    public static int BarDigitsToValue(string digits) {
        return (digits[0] - '0') * 16 + (digits[1] - '0') * 4 + (digits[2] - '0');
    }

    public static int BarDigitsToValue(int b0, int b1, int b2) {
        return b0 * 16 + b1 * 4 + b2;
    }

    public static void ValueToBarDigits(int value, Span<int> output) {
        output[0] = value / 16;
        output[1] = (value / 4) % 4;
        output[2] = value % 4;
    }

    private static int[] BuildGfExp() {
        var exp = new int[128];
        var x = 1;
        for (var i = 0; i < 63; i++) {
            exp[i] = x;
            x <<= 1;
            if ((x & 0x40) != 0) x ^= 0x43;
        }
        for (var i = 63; i < exp.Length; i++) exp[i] = exp[i - 63];
        return exp;
    }

    private static int[] BuildGfLog() {
        var log = new int[64];
        for (var i = 0; i < 63; i++) {
            log[GfExp[i]] = i;
        }
        return log;
    }

    public static int Multiply(int a, int b) {
        if (a == 0 || b == 0) return 0;
        return GfExp[(GfLog[a] + GfLog[b]) % 63];
    }

    private static int[] BuildGenerator() {
        var gen = new[] { 1 };
        for (var degree = 1; degree <= 4; degree++) {
            var alpha = GfExp[degree];
            var next = new int[gen.Length + 1];
            for (var i = 0; i < gen.Length; i++) {
                next[i] ^= gen[i];
                next[i + 1] ^= Multiply(gen[i], alpha);
            }
            gen = next;
        }
        return gen;
    }

    public static int[] ComputeParity(ReadOnlySpan<int> data) {
        var parity = new int[4];
        for (var i = 0; i < data.Length; i++) {
            var feedback = data[i] ^ parity[0];
            parity[0] = parity[1] ^ Multiply(feedback, Generator[1]);
            parity[1] = parity[2] ^ Multiply(feedback, Generator[2]);
            parity[2] = parity[3] ^ Multiply(feedback, Generator[3]);
            parity[3] = Multiply(feedback, Generator[4]);
        }
        return parity;
    }
}
