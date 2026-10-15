using System;
using System.Collections.Generic;

namespace CodeGlyphX.PosiCode;

/// <summary>Encodes normal, limited and stacked PosiCode symbols.</summary>
public static class PosiCodeEncoder {
    /// <summary>Encodes Latin-1 text, or the limited character alphabet, with a mandatory check character.</summary>
    public static Barcode1D Encode(string value, PosiCodeVariant variant = PosiCodeVariant.A, bool isGs1 = false,
        bool readerInitialization = false, bool messageAppend = false) {
        var words = EncodeWords(value, variant, isGs1, readerInitialization, messageAppend);
        return EncodeRow(words, 0, words.Count, variant, 0);
    }

    /// <summary>Encodes two through five linked rows. Character-set and upper-shift state continues across rows.</summary>
    public static MatrixSymbol EncodeStacked(string value, int rows = 2, PosiCodeVariant variant = PosiCodeVariant.A,
        bool isGs1 = false, int? rowHeight = null, bool readerInitialization = false, bool messageAppend = false) {
        if (rows < 2 || rows > 5) throw new ArgumentOutOfRangeException(nameof(rows));
        if (rowHeight.HasValue && rowHeight.Value < 1) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        var words = EncodeWords(value, variant, isGs1, readerInitialization, messageAppend);
        if (words.Count < rows) throw new ArgumentException("Each stacked PosiCode row requires a codeword.", nameof(value));
        var encoded = new Barcode1D[rows];
        var width = 0;
        var size = words.Count / rows;
        for (var row = 0; row < rows; row++) {
            var count = row == rows - 1 ? words.Count - row * size : size;
            encoded[row] = EncodeRow(words, row * size, count, variant, row + 1 + 10 * rows);
            width = Math.Max(width, encoded[row].TotalModules);
        }
        var height = rowHeight ?? Math.Max(1, (int)Math.Round(width * 0.15, MidpointRounding.AwayFromZero));
        var matrix = new BitMatrix(width, checked(height * rows + 2 * (rows - 1)));
        for (var row = 0; row < rows; row++) {
            var x = 0;
            foreach (var segment in encoded[row].Segments) {
                for (var repeat = 0; repeat < segment.Modules; repeat++, x++)
                    if (segment.IsBar) for (var y = 0; y < height; y++) matrix[x, row * (height + 2) + y] = true;
            }
        }
        return new MatrixSymbol(SymbolFormat.PosiCode, matrix);
    }

    private static List<int> EncodeWords(string value, PosiCodeVariant variant, bool gs1, bool readerInitialization, bool messageAppend) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        PosiCodeTables.Validate(variant);
        if ((gs1 ? 1 : 0) + (readerInitialization ? 1 : 0) + (messageAppend ? 1 : 0) > 1)
            throw new ArgumentException("GS1, reader initialization and message append are separate PosiCode modes.");
        if ((int)variant >= 2 && (readerInitialization || messageAppend))
            throw new ArgumentException("Limited PosiCode has no function characters.", nameof(variant));
        if (gs1) {
            if ((int)variant >= 2) throw new ArgumentException("Limited PosiCode has no FNC1 character.", nameof(variant));
            value = Gs1.ElementString(value);
        }
        if (value.Length == 0 || value.Length > 500) throw new ArgumentException("PosiCode requires 1 through 500 characters.", nameof(value));
        // Function tokens are negative so they cannot collide with Latin-1 data.
        var tokens = new List<int>(value.Length * 2 + 1);
        if (gs1) tokens.Add(-1);
        if (readerInitialization) tokens.Add(-3);
        if (messageAppend) tokens.Add(-2);
        foreach (var c in value) {
            if (c > 255) throw new ArgumentException("PosiCode text must contain Latin-1 characters.", nameof(value));
            if (gs1 && c == Gs1.GroupSeparator) { tokens.Add(-1); continue; }
            if (c > 127) tokens.Add(-4);
            tokens.Add(c & 127);
        }
        var words = new List<int>(tokens.Count * 2);
        var set = 0;
        for (var i = 0; i < tokens.Count; i++) {
            var token = tokens[i];
            var current = set == 0 ? PosiCodeTables.Set0 : PosiCodeTables.Set1;
            var direct = token >= 0 ? current.IndexOf((char)token) : -1;
            if ((int)variant >= 2) {
                if (direct < 0 || direct >= 38) throw new ArgumentException("Limited PosiCode requires digits, uppercase letters, hyphen or period.", nameof(value));
                words.Add(direct);
                continue;
            }
            if (direct >= 0) { words.Add(direct); continue; }
            var control = token < 0 ? 41 - token : PosiCodeTables.Set2.IndexOf((char)token);
            if (control >= 0) { words.Add(45); words.Add(control); continue; }
            var nextInCurrent = i + 1 < tokens.Count && tokens[i + 1] >= 0 && current.IndexOf((char)tokens[i + 1]) >= 0;
            if (!nextInCurrent) { words.Add(43); set = 1 - set; i--; }
            else {
                words.Add(44);
                words.Add((set == 0 ? PosiCodeTables.Set1 : PosiCodeTables.Set0).IndexOf((char)token));
            }
        }
        return words;
    }

    private static Barcode1D EncodeRow(List<int> words, int start, int count, PosiCodeVariant variant, int sequence) {
        var bars = new List<BarSegment>(count * 6 + 31);
        PosiCodeTables.Append(bars, PosiCodeTables.Starts[(int)variant]);
        for (var i = start; i < start + count; i++) PosiCodeTables.Append(bars, PosiCodeTables.Patterns[(int)variant][words[i]]);
        foreach (var width in PosiCodeTables.CheckWidths(PosiCodeTables.CheckValue(words, start, count, variant, sequence), variant))
            bars.Add(new BarSegment((bars.Count & 1) == 0, width));
        PosiCodeTables.Append(bars, PosiCodeTables.Stops[(int)variant]);
        return new Barcode1D(bars, SymbolFormat.PosiCode);
    }
}
