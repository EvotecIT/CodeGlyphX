using System.Collections.Generic;
using CodeGlyphX.Code128;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodeCode128(bool[] modules, out string text, out bool isGs1) {
        return TryDecodeCode128(modules, out text, out isGs1, out _);
    }

    internal static bool TryDecodeGs1CompositeCarrier(bool[] modules, out string text, out int compositeLinkage) {
        return TryDecodeCode128(modules, out text, out var isGs1, out compositeLinkage)
            && isGs1
            && compositeLinkage != 0;
    }

    private static bool TryDecodeCode128(bool[] modules, out string text, out bool isGs1, out int compositeLinkage) {
        text = string.Empty;
        isGs1 = false;
        compositeLinkage = 0;
        if (modules.Length < 24) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 6) return false;
        if (runs[0] == 0) return false;

        var codes = new List<int>();
        var pos = 0;
        var stopped = false;
        while (pos < runs.Length) {
            if (runs.Length - pos >= 7) {
                var stopKey = PatternKey(runs, pos, 7);
                if (stopKey == Code128StopKey.Value) {
                    pos += 7;
                    stopped = true;
                    break;
                }
            }
            if (runs.Length - pos < 6) return false;
            var key = PatternKey(runs, pos, 6);
            if (!Code128PatternMap.Value.TryGetValue(key, out var code)) return false;
            codes.Add(code);
            pos += 6;
        }

        if (!stopped || pos != runs.Length || codes.Count < 3) return false;

        var checksum = codes[codes.Count - 1];
        var sum = codes[0];
        for (var i = 1; i < codes.Count - 1; i++) sum = (sum + codes[i] * i) % 103;
        if (sum != checksum) return false;

        var start = codes[0];
        var set = start == Code128Tables.StartC ? 'C' : start == Code128Tables.StartB ? 'B' : start == Code128Tables.StartA ? 'A' : '?';
        if (set == '?') return false;

        var sb = new System.Text.StringBuilder();
        var upperMode = false;
        var upperShift = false;
        var shifted = false;
        for (var i = 1; i < codes.Count - 1; i++) {
            var code = codes[i];
            var activeSet = shifted ? (set == 'A' ? 'B' : 'A') : set;
            // SHIFT applies to exactly one data character, never another control.
            if (shifted && code > 95) return false;
            shifted = false;
            if (isGs1 && i == codes.Count - 2) {
                compositeLinkage = ResolveCompositeLinkage(set, code);
                if (compositeLinkage != 0) {
                    if (upperShift) return false;
                    continue;
                }
            }
            if (code == Code128Tables.Fnc1) {
                if (i == 1) isGs1 = true;
                else sb.Append(Gs1.GroupSeparator);
                continue;
            }
            if (activeSet == 'C') {
                if (code == Code128Tables.CodeA) set = 'A';
                else if (code == Code128Tables.CodeB) set = 'B';
                else if (code <= 99) sb.Append(code.ToString("00", System.Globalization.CultureInfo.InvariantCulture));
                else return false;
                continue;
            }
            if (code <= 95) {
                var value = activeSet == 'A' ? (code < 64 ? code + 32 : code - 64) : code + 32;
                if (upperMode != upperShift) value += 128;
                sb.Append((char)value);
                upperShift = false;
            } else if (code == 98) {
                shifted = true;
            } else if (code == Code128Tables.CodeC) {
                set = 'C';
            } else if (code == (activeSet == 'A' ? Code128Tables.CodeB : Code128Tables.CodeA)) {
                set = activeSet == 'A' ? 'B' : 'A';
            } else if (code == (activeSet == 'A' ? Code128Tables.CodeA : Code128Tables.CodeB)) {
                if (upperShift) { upperMode = !upperMode; upperShift = false; }
                else upperShift = true;
            } else {
                // FNC2/FNC3 require application-level semantics not represented by this result.
                return false;
            }
        }
        if (shifted || upperShift) return false;

        text = sb.ToString();
        return true;
    }

    private static int ResolveCompositeLinkage(char set, int code) {
        if (set == 'C') {
            if (code == Code128Tables.CodeA) return 1;
            if (code == Code128Tables.CodeB) return 2;
            return 0;
        }
        if (code == Code128Tables.CodeC) return 1;
        if (code == Code128Tables.CodeA) return 2;
        return 0;
    }

}
