using System;
using System.Collections.Generic;
using CodeGlyphX.Code128;

namespace CodeGlyphX.CodablockF;

/// <summary>Encodes Latin-1 and GS1 messages as linked Codablock F rows.</summary>
public static class CodablockFEncoder {
    /// <summary>Encodes a message using four through sixty-two data columns and at least two linked rows.</summary>
    public static MatrixSymbol Encode(string value, int columns = 8, int minimumRows = 2, bool isGs1 = false) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) throw new ArgumentException("Codablock F requires a nonempty value.", nameof(value));
        if (columns < 4 || columns > 62) throw new ArgumentOutOfRangeException(nameof(columns));
        if (minimumRows < 2 || minimumRows > 44) throw new ArgumentOutOfRangeException(nameof(minimumRows));
        if (isGs1) value = Gs1.ElementString(value);
        foreach (var character in value)
            if (character > 255) throw new ArgumentException("Codablock F text must contain Latin-1 characters.", nameof(value));
        var rows = new List<List<int>>();
        var at = 0;
        var fnc1Pending = isGs1;
        while (true) {
            if (rows.Count == 44) throw new ArgumentException("The message exceeds Codablock F capacity.", nameof(value));
            var set = Digits(value, at) >= 2 ? 'C' : at < value.Length && (value[at] & 127) < 32 ? 'A' : 'B';
            var words = new List<int>(columns + 4) { 103, set == 'A' ? 98 : set == 'B' ? 100 : 99, 0 };
            while (words.Count < columns + 3 && (at < value.Length || fnc1Pending)) {
                var remaining = columns + 3 - words.Count;
                if (fnc1Pending) { words.Add(102); fnc1Pending = false; continue; }
                var character = value[at];
                if (isGs1 && character == Gs1.GroupSeparator) { words.Add(102); at++; continue; }
                var digits = Digits(value, at);
                if (set == 'C') {
                    if (digits >= 2) { words.Add((value[at] - '0') * 10 + value[at + 1] - '0'); at += 2; continue; }
                    set = (character & 127) < 32 ? 'A' : 'B';
                    words.Add(set == 'A' ? 101 : 100);
                    continue;
                }
                if (digits >= 4) { words.Add(99); set = 'C'; continue; }
                var low = character & 127;
                var wanted = low < 32 ? 'A' : low > 95 ? 'B' : set;
                if (wanted != set) { words.Add(wanted == 'A' ? 101 : 100); set = wanted; continue; }
                if (character > 127) {
                    if (remaining < 2) break;
                    words.Add(set == 'A' ? 101 : 100);
                }
                words.Add(set == 'A' && low < 32 ? low + 64 : low - 32);
                at++;
            }
            var final = at == value.Length && !fnc1Pending && rows.Count + 1 >= minimumRows
                && columns + 3 - words.Count >= 2;
            Pad(words, columns + 3 - (final ? 2 : 0), ref set);
            if (final) {
                CodablockFChecks.Calculate(value, out var first, out var second);
                words.Add(CodablockFChecks.Map(first, set));
                words.Add(CodablockFChecks.Map(second, set));
            }
            words.Add(0); // Row check populated after row-count indicators are known.
            rows.Add(words);
            if (final) break;
        }
        var width = columns * 11 + 57;
        var modules = new BitMatrix(width, rows.Count * 11 + 1);
        for (var x = 0; x < width; x++) { modules[x, 0] = true; modules[x, modules.Height - 1] = true; }
        for (var row = 0; row < rows.Count; row++) {
            var words = rows[row];
            var set = words[1] == 99 ? 'C' : words[1] == 98 ? 'A' : 'B';
            words[2] = CodablockFChecks.Map(row == 0 ? rows.Count - 2 : row + 42, set);
            var check = words[0];
            for (var i = 1; i < words.Count - 1; i++) check = (check + i * words[i]) % 103;
            words[words.Count - 1] = check;
            var bits = new List<bool>(width);
            foreach (var word in words) Append(bits, Code128Tables.GetPattern(word), 6);
            Append(bits, Code128Tables.GetPattern(106), 7);
            for (var repeat = 0; repeat < 10; repeat++)
                for (var x = 0; x < width; x++) modules[x, row * 11 + 1 + repeat] = bits[x];
            if (row + 1 < rows.Count) {
                var separator = row * 11 + 11;
                for (var x = 0; x < width; x++) modules[x, separator] = x < 11 || x >= width - 13 ? bits[x] : true;
            }
        }
        return new MatrixSymbol(SymbolFormat.CodablockF, modules);
    }

    private static void Pad(List<int> words, int count, ref char set) {
        while (words.Count < count) { words.Add(set == 'C' ? 100 : 99); set = set == 'C' ? 'B' : 'C'; }
    }

    private static int Digits(string value, int at) {
        var count = 0;
        while (at + count < value.Length && value[at + count] >= '0' && value[at + count] <= '9') count++;
        return count;
    }

    private static void Append(List<bool> bits, uint pattern, int count) {
        var dark = true;
        for (var i = count - 1; i >= 0; i--) {
            var width = (int)((pattern >> (i * 4)) & 15);
            for (var x = 0; x < width; x++) bits.Add(dark);
            dark = !dark;
        }
    }
}

/// <summary>Codablock F's modulo-86 byte checks and A/B/C indicator character mapping.</summary>
internal static class CodablockFChecks {
    internal static void Calculate(string value, out int first, out int second) {
        first = 0;
        second = 0;
        for (var i = 0; i < value.Length; i++) {
            first = (first + value[i] * (i + 1)) % 86;
            second = (second + value[i] * i) % 86;
        }
    }

    internal static int Map(int value, char set) => set == 'C' ? value : value < 32 ? value + 64 : value < 48 ? value - 32 : value - 22;

    internal static int Unmap(int value, char set) => set == 'C' ? value <= 85 ? value : -1
        : value >= 64 && value <= 95 ? value - 64 : value <= 15 ? value + 32 : value >= 26 && value <= 63 ? value + 22 : -1;
}
