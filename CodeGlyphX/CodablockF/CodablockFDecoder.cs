using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Code128;

namespace CodeGlyphX.CodablockF;

/// <summary>Decodes linked Codablock F rows, validating row checks and both whole-message check characters.</summary>
public static class CodablockFDecoder {
    /// <summary>Decodes exact module rows, with or without repeated row heights and separator bars.</summary>
    public static bool TryDecode(BitMatrix modules, out CodablockFDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Width < 101 || modules.Width > 739 || (modules.Width - 57) % 11 != 0) return false;
        var columns = (modules.Width - 57) / 11;
        var rows = new List<int>();
        for (var y = 0; y < modules.Height; y++) {
            if (y > 0 && SameRow(modules, y, y - 1)) continue;
            var separator = true;
            for (var x = 11; x < modules.Width - 13; x++) if (!modules[x, y]) { separator = false; break; }
            if (separator) continue;
            rows.Add(y);
            if (rows.Count > 44) return false;
        }
        if (rows.Count < 2) return false;
        var text = new StringBuilder();
        var gs1 = false;
        var firstCheck = -1;
        var secondCheck = -1;
        for (var row = 0; row < rows.Count; row++) {
            var y = rows[row];
            var words = new List<int>(columns + 4);
            for (var at = 0; at < columns + 4; at++) {
                var found = false;
                for (var word = 0; word < 106; word++) {
                    if (!Match(modules, y, at * 11, Code128Tables.GetPattern(word), 6)) continue;
                    words.Add(word);
                    found = true;
                    break;
                }
                if (!found) return false;
            }
            if (words[0] != 103 || !Match(modules, y, modules.Width - 13, Code128Tables.GetPattern(106), 7)) return false;
            var checksum = words[0];
            for (var i = 1; i < words.Count - 1; i++) checksum = (checksum + i * words[i]) % 103;
            if (checksum != words[words.Count - 1]) return false;
            var set = words[1] == 98 ? 'A' : words[1] == 100 ? 'B' : words[1] == 99 ? 'C' : '\0';
            if (set == '\0' || CodablockFChecks.Unmap(words[2], set) != (row == 0 ? rows.Count - 2 : row + 42)) return false;
            var end = words.Count - 1 - (row == rows.Count - 1 ? 2 : 0);
            for (var i = 3; i < end; i++) if (words[i] > 102) return false;
            if (!Code128CharacterReader.TryRead(words, 3, end, set, '\0', 0, out var fragment, out var finalSet)) return false;
            if (row == 0 && words[3] == 102) {
                gs1 = true;
                if (fragment.Length == 0 || fragment[0] != Gs1.GroupSeparator) return false;
                fragment = fragment.Substring(1);
            }
            text.Append(fragment);
            if (row == rows.Count - 1) {
                firstCheck = CodablockFChecks.Unmap(words[end], finalSet);
                secondCheck = CodablockFChecks.Unmap(words[end + 1], finalSet);
            }
        }
        var payload = text.ToString();
        CodablockFChecks.Calculate(payload, out var expectedFirst, out var expectedSecond);
        if (firstCheck != expectedFirst || secondCheck != expectedSecond) return false;
        decoded = new CodablockFDecoded(payload, gs1, rows.Count, columns);
        return true;
    }

    private static bool SameRow(BitMatrix modules, int first, int second) {
        for (var x = 0; x < modules.Width; x++) if (modules[x, first] != modules[x, second]) return false;
        return true;
    }

    private static bool Match(BitMatrix modules, int y, int offset, uint pattern, int count) {
        var dark = true;
        for (var i = count - 1; i >= 0; i--) {
            var width = (int)((pattern >> (i * 4)) & 15);
            for (var x = 0; x < width; x++) if (modules[offset++, y] != dark) return false;
            dark = !dark;
        }
        return true;
    }
}

/// <summary>A checked Codablock F message with its carrier dimensions.</summary>
public sealed class CodablockFDecoded {
    /// <summary>Gets the decoded Latin-1 payload.</summary>
    public string Text { get; }
    /// <summary>Gets whether initial FNC1 declares GS1 semantics.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the number of data-codeword columns per row.</summary>
    public int Columns { get; }
    internal CodablockFDecoded(string text, bool gs1, int rows, int columns) { Text = text; IsGs1 = gs1; Rows = rows; Columns = columns; }
}
