using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.PosiCode;

/// <summary>Validates and decodes PosiCode module rows and linked stacked symbols.</summary>
public static class PosiCodeDecoder {
    private const int MaxRowCodewords = 2002;
    // At most eight start runs, twelve check runs and eleven stop runs.
    private const int MaxRowRuns = MaxRowCodewords * 6 + 31;
    /// <summary>Decodes an exact module row, including optional white margins.</summary>
    public static bool TryDecode(bool[] modules, out PosiCodeDecoded decoded) {
        decoded = null!;
        if (modules is null || !TryReadRow(modules, 0, out var words, out var variant)) return false;
        return TryReadText(words, variant, out decoded);
    }

    /// <summary>Decodes one row or two through five linked rows, with repeated physical rows and white separators.</summary>
    public static bool TryDecode(BitMatrix modules, out PosiCodeDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Width == 0 || modules.Height == 0) return false;
        var rows = new List<bool[]>();
        bool[]? previous = null;
        for (var y = 0; y < modules.Height; y++) {
            var row = new bool[modules.Width];
            var dark = false;
            var same = previous != null;
            for (var x = 0; x < modules.Width; x++) {
                row[x] = modules[x, y];
                dark |= row[x];
                if (previous != null && row[x] != previous[x]) same = false;
            }
            if (!dark) { previous = null; continue; }
            if (!same) rows.Add(row);
            previous = row;
            if (rows.Count > 5) return false;
        }
        if (rows.Count == 0) return false;
        var all = new List<int>();
        var variant = PosiCodeVariant.A;
        for (var row = 0; row < rows.Count; row++) {
            if (!TryReadRow(rows[row], rows.Count == 1 ? 0 : row + 1 + rows.Count * 10, out var words, out var found)) return false;
            if (row > 0 && found != variant) return false;
            variant = found;
            all.AddRange(words);
        }
        return TryReadText(all, variant, out decoded);
    }

    private static bool TryReadRow(bool[] modules, int sequence, out List<int> words, out PosiCodeVariant variant) {
        words = new List<int>();
        variant = PosiCodeVariant.A;
        var first = 0;
        var last = modules.Length - 1;
        while (first <= last && !modules[first]) first++;
        while (last >= first && !modules[last]) last--;
        if (first > last) return false;
        var runs = new List<int>();
        var dark = true;
        var length = 0;
        for (var x = first; x <= last; x++) {
            if (modules[x] == dark) length++;
            else {
                if (runs.Count == MaxRowRuns) return false;
                runs.Add(length); length = 1; dark = !dark;
            }
        }
        if (runs.Count == MaxRowRuns) return false;
        runs.Add(length);
        for (var version = 0; version < 4; version++) {
            var start = PosiCodeTables.Starts[version];
            var stop = PosiCodeTables.Stops[version];
            if (!Matches(runs, 0, start) || !Matches(runs, runs.Count - stop.Length, stop)) continue;
            var count = runs.Count - start.Length - stop.Length - 12;
            if (count < 6 || count % 6 != 0 || count / 6 > MaxRowCodewords) continue;
            words.Clear();
            var valid = true;
            for (var i = start.Length; i < start.Length + count; i += 6) {
                var found = -1;
                for (var c = 0; c < PosiCodeTables.Patterns[version].Length; c++)
                    if (Matches(runs, i, PosiCodeTables.Patterns[version][c])) { found = c; break; }
                if (found < 0) { valid = false; break; }
                words.Add(found);
            }
            if (!valid) continue;
            var widths = PosiCodeTables.CheckWidths(PosiCodeTables.CheckValue(words, 0, words.Count, (PosiCodeVariant)version, sequence), (PosiCodeVariant)version);
            for (var i = 0; i < widths.Length; i++) if (runs[start.Length + count + i] != widths[i]) valid = false;
            if (!valid) continue;
            variant = (PosiCodeVariant)version;
            return true;
        }
        return false;
    }

    private static bool Matches(List<int> runs, int start, string pattern) {
        if (start < 0 || start + pattern.Length > runs.Count) return false;
        for (var i = 0; i < pattern.Length; i++) if (runs[start + i] != pattern[i] - '0') return false;
        return true;
    }

    private static bool TryReadText(List<int> words, PosiCodeVariant variant, out PosiCodeDecoded decoded) {
        decoded = null!;
        var tokens = new List<int>();
        var set = 0;
        for (var i = 0; i < words.Count; i++) {
            var word = words[i];
            if ((int)variant >= 2) { tokens.Add(PosiCodeTables.Set0[word]); continue; }
            if (word == 43) { set = 1 - set; continue; }
            var readSet = set;
            if (word >= 44) {
                readSet = word == 45 ? 2 : 1 - set;
                if (++i == words.Count) return false;
                word = words[i];
            }
            if (readSet == 2 && word >= 42) tokens.Add(41 - word);
            else {
                var alphabet = readSet == 0 ? PosiCodeTables.Set0 : readSet == 1 ? PosiCodeTables.Set1 : PosiCodeTables.Set2;
                if (word >= alphabet.Length) return false;
                tokens.Add(alphabet[word]);
            }
        }
        var text = new StringBuilder(tokens.Count);
        var upper = false;
        var shift = false;
        var gs1 = false;
        var readerInitialization = false;
        var messageAppend = false;
        for (var i = 0; i < tokens.Count; i++) {
            var token = tokens[i];
            if (token == -4) {
                if (i + 1 < tokens.Count && tokens[i + 1] == -4) { upper = !upper; i++; }
                else { if (shift) return false; shift = true; }
            } else if (token == -1) {
                if (i == 0) gs1 = true;
                else text.Append(Gs1.GroupSeparator);
            } else if (token == -2) messageAppend = true;
            else if (token == -3) readerInitialization = true;
            else {
                text.Append((char)(token + (upper != shift ? 128 : 0)));
                shift = false;
            }
        }
        if (shift) return false;
        decoded = new PosiCodeDecoded(text.ToString(), variant, gs1, readerInitialization, messageAppend);
        return true;
    }
}

/// <summary>The text, gap variant and function metadata recovered from a PosiCode symbol.</summary>
public sealed class PosiCodeDecoded {
    /// <summary>Gets the decoded Latin-1 text, with non-leading FNC1 represented by the group separator.</summary>
    public string Text { get; }
    /// <summary>Gets the physical gap variant.</summary>
    public PosiCodeVariant Variant { get; }
    /// <summary>Gets whether the first function character declares GS1 data.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets whether FNC3 requests reader initialization.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets whether FNC2 requests message append.</summary>
    public bool MessageAppend { get; }
    internal PosiCodeDecoded(string text, PosiCodeVariant variant, bool gs1, bool readerInitialization, bool messageAppend) {
        Text = text;
        Variant = variant;
        IsGs1 = gs1;
        ReaderInitialization = readerInitialization;
        MessageAppend = messageAppend;
    }
}
