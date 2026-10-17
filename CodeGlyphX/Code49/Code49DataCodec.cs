using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Code49;

/// <summary>Owns Code 49's Full ASCII shifts and base-48 numeric encodation.</summary>
internal static class Code49DataCodec {
    internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    internal static List<int> Encode(string text, bool gs1, out int mode) {
        var words = new List<int>();
        if (gs1) words.Add(45);
        for (var i = 0; i < text.Length;) {
            if (text[i] > 127) throw new ArgumentException("Code 49 supports ASCII characters only.", nameof(text));
            if (gs1 && text[i] == Gs1.GroupSeparator) { words.Add(45); i++; continue; }
            var digits = 0;
            while (i + digits < text.Length && text[i + digits] >= '0' && text[i + digits] <= '9') digits++;
            if (digits >= 5) {
                words.Add(48);
                var end = i + digits;
                while (end - i >= 5) {
                    if (end - i == 7) {
                        Pack(words, 100000 + Number(text, i, 4), 3);
                        Pack(words, Number(text, i + 4, 3), 2);
                        i += 7;
                    } else { Pack(words, Number(text, i, 5), 3); i += 5; }
                }
                if (end - i == 1) words.Add(text[i++] - '0');
                else if (end - i == 3) { Pack(words, Number(text, i, 3), 2); i += 3; }
                else if (end - i == 4) { Pack(words, 100000 + Number(text, i, 4), 3); i += 4; }
                if (i < text.Length) words.Add(48);
                continue;
            }
            foreach (var character in Code49Tables.Ascii[text[i++]])
                words.Add(character == '!' ? 43 : character == '&' ? 44 : Alphabet.IndexOf(character));
        }
        mode = words[0] == 48 ? 2 : words[0] == 43 ? 4 : words[0] == 44 ? 5 : 0;
        if (mode != 0) words.RemoveAt(0);
        return words;
    }

    internal static bool TryDecode(List<int> words, int mode, out string text, out bool gs1) {
        text = string.Empty;
        gs1 = false;
        if (mode < 0 || mode > 5) return false;
        var numeric = mode == 2;
        var shift = mode == 4 ? 43 : mode == 5 ? 44 : 0;
        var result = new StringBuilder();
        for (var i = mode == 3 ? 1 : 0; i < words.Count;) {
            if (numeric) {
                var end = i;
                while (end < words.Count && words[end] != 48) end++;
                if (!ReadNumeric(words, i, end, result)) return false;
                i = end;
                numeric = false;
                if (i < words.Count) i++;
                continue;
            }
            var word = words[i++];
            if (shift != 0) {
                if (word >= Alphabet.Length) return false;
                var pair = (shift == 43 ? "!" : "&") + Alphabet[word];
                var found = Array.IndexOf(Code49Tables.Ascii, pair);
                if (found < 0) return false;
                result.Append((char)found);
                shift = 0;
            } else if (word < Alphabet.Length) result.Append(Alphabet[word]);
            else if (word == 43 || word == 44) shift = word;
            else if (word == 45) {
                if (i == (mode == 3 ? 2 : 1)) gs1 = true;
                else result.Append(Gs1.GroupSeparator);
            } else if (word == 46) result.Append('\u001c');
            else if (word == 47) return false; // FNC3 is reserved by USS-49.
            else if (word == 48) numeric = true;
            else return false;
        }
        if (shift != 0) return false;
        text = result.ToString();
        return true;
    }

    private static bool ReadNumeric(List<int> words, int start, int end, StringBuilder result) {
        if (start == end) return false;
        var count = end - start;
        var triples = count / 3;
        for (var group = 0; group < triples; group++) {
            var at = start + group * 3;
            var value = words[at] * 2304 + words[at + 1] * 48 + words[at + 2];
            if (value >= 100000) {
                if (value > 109999 || group != triples - 1 || count % 3 == 1) return false;
                result.Append((value - 100000).ToString("D4", CultureInfo.InvariantCulture));
            } else result.Append(value.ToString("D5", CultureInfo.InvariantCulture));
        }
        var remainder = start + triples * 3;
        if (end - remainder == 1) {
            if (words[remainder] > 9) return false;
            result.Append((char)('0' + words[remainder]));
        } else if (end - remainder == 2) {
            var value = words[remainder] * 48 + words[remainder + 1];
            if (value > 999) return false;
            result.Append(value.ToString("D3", CultureInfo.InvariantCulture));
        }
        return true;
    }

    private static int Number(string text, int offset, int count) {
        var value = 0;
        for (var i = 0; i < count; i++) value = value * 10 + text[offset + i] - '0';
        return value;
    }

    private static void Pack(List<int> words, int value, int count) {
        if (count == 3) { words.Add(value / 2304); value %= 2304; }
        words.Add(value / 48);
        words.Add(value % 48);
    }
}
