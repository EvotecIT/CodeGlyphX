using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Code128;

/// <summary>Reads the shared A/B/C character sets, Latin-1 shifts, and temporary shifts used by stacked Code 128 relatives.</summary>
internal static class Code128CharacterReader {
    internal static bool TryRead(List<int> words, int start, int end, char initialSet,
        char temporarySet, int temporaryCount, out string text, out char finalSet) {
        text = string.Empty;
        var set = initialSet;
        var temporary = temporarySet;
        var remaining = temporaryCount;
        var upperMode = false;
        var upperShift = false;
        var result = new StringBuilder();
        for (var i = start; i < end; i++) {
            var code = words[i];
            var active = remaining > 0 ? temporary : set;
            if ((active == 'C' && code < 100) || (active != 'C' && code < 96)) {
                if (active == 'C') {
                    if (upperShift) { finalSet = set; return false; }
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
            if (remaining > 0 || upperShift) { finalSet = set; return false; }
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
            finalSet = set;
            return false;
        }
        finalSet = set;
        if (remaining != 0 || upperShift) return false;
        text = result.ToString();
        return true;
    }
}
