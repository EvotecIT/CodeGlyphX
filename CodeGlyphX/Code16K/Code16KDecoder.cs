using System;
using System.Collections.Generic;
using CodeGlyphX.Code128;

namespace CodeGlyphX.Code16K;

/// <summary>Decodes Code 16K rows and validates both modulo-107 symbol check characters.</summary>
public static class Code16KDecoder {
    /// <summary>Decodes an orthogonal module grid, accepting compact or repeated-height data rows.</summary>
    public static bool TryDecode(BitMatrix modules, out Code16KDecoded decoded) {
        decoded = null!;
        if (modules is null || (modules.Width != 70 && modules.Width != 81) || modules.Height < 2) return false;
        var offset = modules.Width == 81 ? 10 : 0;
        var dataRows = new List<int>();
        for (var y = 0; y < modules.Height; y++) {
            if (y > 0 && SameRow(modules, y, y - 1)) continue;
            var separator = true;
            for (var x = 0; x < 70; x++) if (!modules[offset + x, y]) { separator = false; break; }
            if (separator) continue;
            dataRows.Add(y);
            if (dataRows.Count > 16) return false;
        }
        if (dataRows.Count < 2) return false;
        var words = new List<int>(dataRows.Count * 5);
        for (var row = 0; row < dataRows.Count; row++) {
            var y = dataRows[row];
            if (!Match(modules, offset, y, Code16KPatterns.Starts[row % 8], true) || !modules[offset + 7, y]) return false;
            for (var at = 0; at < 5; at++) {
                var found = false;
                for (var value = 0; value < 107; value++) {
                    if (!Match(modules, offset + 8 + at * 11, y, Code16KPatterns.Codeword(value), false)) continue;
                    words.Add(value);
                    found = true;
                    break;
                }
                if (!found) return false;
            }
        }
        var bodyCount = words.Count - 2;
        var firstCheck = 0;
        var secondCheck = 0;
        for (var i = 0; i < bodyCount; i++) {
            firstCheck += (i + 2) * words[i];
            secondCheck += (i + 1) * words[i];
        }
        firstCheck %= 107;
        secondCheck = (secondCheck + firstCheck * (bodyCount + 1)) % 107;
        if (words[bodyCount] != firstCheck || words[bodyCount + 1] != secondCheck) return false;
        var extended = words[0] == 105;
        if (extended && dataRows.Count != 16) return false;
        var mode = extended ? 7 : words[0] - (dataRows.Count - 2) * 7;
        if (mode < 0 || !extended && mode > 6) return false;
        while (bodyCount > 1 && words[bodyCount - 1] == 103) bodyCount--;
        var start = 1;
        Code16KStructuredAppend? append = null;
        var messageAppend = false;
        if (extended) {
            if (bodyCount < 4) return false;
            append = new Code16KStructuredAppend(words[1] + 1, words[2] + 1);
            start = 3;
        } else if ((mode == 0 || mode == 1) && bodyCount > 2 && words[2] == 97) {
            append = new Code16KStructuredAppend(words[1] / 10, words[1] % 10, false);
            start = 3;
        } else if ((mode == 0 || mode == 1) && bodyCount > 1 && words[1] == 97) {
            messageAppend = true;
            start = 2;
        }
        if (append.HasValue && !append.Value.IsValid) return false;
        var stops = extended && append!.Value.Index % 2 == 0 ? Code16KPatterns.EvenStops : Code16KPatterns.Stops;
        for (var row = 0; row < dataRows.Count; row++)
            if (!Match(modules, offset + 63, dataRows[row], stops[row], false)) return false;
        if (!TryReadPayload(words, start, bodyCount, mode, out var text, out var readerInitialization, out var otherAppend)) return false;
        if (otherAppend) return false; // FNC2 in any other position is reserved.
        var gs1 = mode == 3 || mode == 4;
        if (!gs1 && start < bodyCount && words[start] == 102) {
            gs1 = true;
            text = text.Substring(1);
        }
        decoded = new Code16KDecoded(text, gs1, dataRows.Count, readerInitialization, messageAppend, append);
        return true;
    }

    private static bool TryReadPayload(List<int> words, int start, int end, int mode, out string text,
        out bool readerInitialization, out bool messageAppend) {
        var set = mode == 0 ? 'A' : mode == 2 || mode >= 4 && mode <= 6 ? 'C' : 'B';
        var temporary = mode == 5 || mode == 6 ? 'B' : '\0';
        var remaining = mode == 5 ? 1 : mode == 6 ? 2 : 0;
        return Code128CharacterReader.TryRead(words, start, end, set, temporary, remaining, out text, out _,
            out readerInitialization, out messageAppend);
    }

    private static bool SameRow(BitMatrix modules, int first, int second) {
        for (var x = 0; x < modules.Width; x++) if (modules[x, first] != modules[x, second]) return false;
        return true;
    }

    private static bool Match(BitMatrix modules, int offset, int y, string widths, bool dark) {
        foreach (var width in widths) {
            for (var x = 0; x < width - '0'; x++) if (modules[offset++, y] != dark) return false;
            dark = !dark;
        }
        return true;
    }
}
