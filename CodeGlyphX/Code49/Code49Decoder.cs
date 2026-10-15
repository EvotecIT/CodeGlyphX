using System;
using System.Collections.Generic;

namespace CodeGlyphX.Code49;

/// <summary>Decodes Code 49's row parity, symbol checks, Full ASCII shifts, and numeric compaction.</summary>
public static class Code49Decoder {
    /// <summary>Decodes a compact or repeated-height module grid and returns validated payload metadata.</summary>
    public static bool TryDecode(BitMatrix modules, out Code49Decoded decoded) {
        decoded = null!;
        if (modules is null || (modules.Width != 70 && modules.Width != 81) || modules.Height < 2) return false;
        var offset = modules.Width == 81 ? 10 : 0;
        var rows = new List<int>();
        for (var y = 0; y < modules.Height; y++) {
            if (y > 0 && SameRow(modules, y, y - 1)) continue;
            var separator = true;
            for (var x = 0; x < 70; x++) if (!modules[offset + x, y]) { separator = false; break; }
            if (separator) continue;
            rows.Add(y);
            if (rows.Count > 8) return false;
        }
        if (rows.Count < 2) return false;
        var words = new int[rows.Count, 8];
        for (var row = 0; row < rows.Count; row++) {
            var y = rows[row];
            if (!modules[offset, y] || modules[offset + 1, y]) return false;
            for (var x = 66; x < 70; x++) if (!modules[offset + x, y]) return false;
            for (var pair = 0; pair < 4; pair++) {
                ushort pattern = 0;
                for (var bit = 0; bit < 16; bit++) pattern = (ushort)((pattern << 1) | (modules[offset + 2 + pair * 16 + bit, y] ? 1 : 0));
                var even = row == rows.Count - 1 || Code49Tables.RowParity[row][pair] == 'E';
                var value = Array.IndexOf(even ? Code49Tables.Even : Code49Tables.Odd, pattern);
                if (value < 0) return false;
                words[row, pair * 2] = value / 49;
                words[row, pair * 2 + 1] = value % 49;
            }
        }
        var mode = words[rows.Count - 1, 6] - (rows.Count - 2) * 7;
        if (mode < 0 || mode > 5) return false;
        var expected = (int[,])words.Clone();
        Code49Checks.Apply(expected, rows.Count);
        for (var row = 0; row < rows.Count; row++)
            for (var at = 0; at < 8; at++) if (words[row, at] != expected[row, at]) return false;
        var data = new List<int>();
        for (var row = 0; row < rows.Count - 1; row++)
            for (var at = 0; at < 7; at++) data.Add(words[row, at]);
        if (rows.Count <= 6) { data.Add(words[rows.Count - 1, 0]); data.Add(words[rows.Count - 1, 1]); }
        while (data.Count > 0 && data[data.Count - 1] == 48) data.RemoveAt(data.Count - 1);
        Code49StructuredAppend? append = null;
        if (mode == 3) {
            if (data.Count < 2 || !Code49StructuredAppend.TryRead(data[0], out var sequence)) return false;
            append = sequence;
        }
        if (!Code49DataCodec.TryDecode(data, mode, out var text, out var gs1)) return false;
        decoded = new Code49Decoded(text, gs1, rows.Count, mode, append);
        return true;
    }

    private static bool SameRow(BitMatrix modules, int first, int second) {
        for (var x = 0; x < modules.Width; x++) if (modules[x, first] != modules[x, second]) return false;
        return true;
    }
}

/// <summary>A validated Code 49 payload and its start-mode metadata.</summary>
public sealed class Code49Decoded {
    /// <summary>Gets decoded ASCII text.</summary>
    public string Text { get; }
    /// <summary>Gets whether leading FNC1 identifies GS1 semantics.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the symbol's start mode, including concatenation modes.</summary>
    public int Mode { get; }
    /// <summary>Gets whether mode 1 marks this as a continuation symbol.</summary>
    public bool IsContinuation => Mode == 1;
    /// <summary>Gets the ordered-group position and total when present.</summary>
    public Code49StructuredAppend? StructuredAppend { get; }
    internal Code49Decoded(string text, bool gs1, int rows, int mode, Code49StructuredAppend? append) {
        Text = text; IsGs1 = gs1; Rows = rows; Mode = mode; StructuredAppend = append;
    }
}
