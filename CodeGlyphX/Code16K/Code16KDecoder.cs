using System;
using System.Collections.Generic;
using System.Text;

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
            if (!Match(modules, offset + 63, y, Code16KPatterns.Stops[row], false)
                && !Match(modules, offset + 63, y, Code16KPatterns.EvenStops[row], false)) return false;
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
        var mode = words[0] - (dataRows.Count - 2) * 7;
        if (mode < 0 || mode > 7) return false;
        while (bodyCount > 1 && words[bodyCount - 1] == 103) bodyCount--;
        if (!TryReadPayload(words, bodyCount, mode, out var text)) return false;
        decoded = new Code16KDecoded(text, mode == 3 || mode == 4, dataRows.Count, mode == 7);
        return true;
    }

    private static bool TryReadPayload(List<int> words, int end, int mode, out string text) {
        text = string.Empty;
        var set = mode == 0 ? 'A' : mode == 2 || mode >= 4 && mode <= 6 ? 'C' : 'B';
        var temporary = mode == 5 || mode == 6 ? 'B' : '\0';
        var remaining = mode == 5 ? 1 : mode == 6 ? 2 : 0;
        var upperMode = false;
        var upperShift = false;
        var result = new StringBuilder();
        for (var i = 1; i < end; i++) {
            var code = words[i];
            var active = remaining > 0 ? temporary : set;
            if ((active == 'C' && code < 100) || (active != 'C' && code < 96)) {
                if (active == 'C') {
                    if (upperShift) return false;
                    result.Append((char)('0' + code / 10)).Append((char)('0' + code % 10));
                } else {
                    var value = active == 'A' && code >= 64 ? code - 64 : code + 32;
                    if (upperMode != upperShift) value += 128;
                    result.Append((char)value);
                    upperShift = false;
                }
                if (remaining > 0) remaining--;
                continue;
            }
            if (remaining > 0 || upperShift) return false;
            if (code == 102) { result.Append(Gs1.GroupSeparator); continue; }
            if (active != 'C' && (code == 96 || code == 97)) continue;
            if (code == 99 && active != 'C') { set = 'C'; continue; }
            if (code == 100 && active != 'B') { set = 'B'; continue; }
            if (code == 101 && active != 'A') { set = 'A'; continue; }
            if ((active == 'A' && code == 101) || (active == 'B' && code == 100)) {
                if (i + 1 < end && words[i + 1] == code) { upperMode = !upperMode; i++; }
                else upperShift = true;
                continue;
            }
            if (active == 'C' && code >= 104 && code <= 106) { temporary = 'B'; remaining = code - 103; continue; }
            if (active != 'C' && (code == 98 || code == 104)) { temporary = active == 'A' ? 'B' : 'A'; remaining = code == 98 ? 1 : 2; continue; }
            if (active != 'C' && (code == 105 || code == 106)) { temporary = 'C'; remaining = code - 103; continue; }
            return false;
        }
        if (remaining != 0 || upperShift) return false;
        text = result.ToString();
        return true;
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
