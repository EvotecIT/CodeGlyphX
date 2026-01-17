using System;
using System.Collections.Generic;
using CodeGlyphX;

namespace CodeGlyphX.Code128;

internal static class Code128Encoder {
    public static Barcode1D Encode(string value) {
        var codes = EncodeCodeValues(value, gs1: false);
        return EncodeFromCodes(codes);
    }

    public static Barcode1D EncodeGs1(string elementString) {
        var codes = EncodeCodeValues(elementString, gs1: true);
        return EncodeFromCodes(codes);
    }

    private static Barcode1D EncodeFromCodes(int[] codes) {

        var segments = new List<BarSegment>(codes.Length * 6);
        for (var ci = 0; ci < codes.Length; ci++) {
            var code = codes[ci];
            var pattern = Code128Tables.GetPattern(code);
            var nibbles = code == Code128Tables.Stop ? 7 : 6;
            var isBar = true;
            for (var ni = nibbles - 1; ni >= 0; ni--) {
                var width = (int)((pattern >> (ni * 4)) & 0xFu);
                AppendSegment(segments, isBar, width);
                isBar = !isBar;
            }
        }

        return new Barcode1D(segments);
    }

    internal static int[] EncodeCodeValues(string value, bool gs1 = false) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) throw new ArgumentException("Value cannot be empty.", nameof(value));
        if (gs1 && value[0] == Gs1.GroupSeparator) throw new ArgumentException("GS1 element string cannot start with a group separator.");

        var codes = new List<int>(value.Length + 4);

        var digitRun0 = CountConsecutiveDigits(value, 0);
        var startSet = value[0] < 32 ? 'A' : (digitRun0 >= 4 && (digitRun0 % 2 == 0)) ? 'C' : 'B';
        var inCodeC = startSet == 'C';
        var set = startSet;
        codes.Add(startSet == 'A' ? Code128Tables.StartA : startSet == 'C' ? Code128Tables.StartC : Code128Tables.StartB);
        if (gs1) codes.Add(Code128Tables.Fnc1);

        var i = 0;
        while (i < value.Length) {
            if (gs1 && value[i] == Gs1.GroupSeparator) {
                codes.Add(Code128Tables.Fnc1);
                i++;
                continue;
            }

            if (set == 'C') {
                var run = CountConsecutiveDigits(value, i);
                if (run >= 2) {
                    if ((run % 2) == 1) {
                        var nextSet = value[i] < 32 ? 'A' : 'B';
                        codes.Add(nextSet == 'A' ? Code128Tables.CodeA : Code128Tables.CodeB);
                        set = nextSet;
                        continue;
                    }

                    var pair = ((value[i] - '0') * 10) + (value[i + 1] - '0');
                    codes.Add(pair);
                    i += 2;
                    continue;
                }

                var fallback = value[i] < 32 ? 'A' : 'B';
                codes.Add(fallback == 'A' ? Code128Tables.CodeA : Code128Tables.CodeB);
                set = fallback;
                continue;
            }

            if (set == 'A') {
                var digitRun = CountConsecutiveDigits(value, i);
                if (digitRun >= 4 && (digitRun % 2) == 0) {
                    codes.Add(Code128Tables.CodeC);
                    set = 'C';
                    continue;
                }

                var ch = value[i];
                if (ch > 95) {
                    codes.Add(Code128Tables.CodeB);
                    set = 'B';
                    continue;
                }

                EmitCodeAChar(ch, codes);
                i++;
                continue;
            }

            // Code B
            var digitRunB = CountConsecutiveDigits(value, i);
            if (digitRunB >= 4) {
                if ((digitRunB % 2) == 1) {
                    var ch = value[i];
                    if (ch < 32) {
                        codes.Add(Code128Tables.CodeA);
                        set = 'A';
                        continue;
                    }
                    EmitCodeBChar(ch, codes);
                    i++;
                    continue;
                }

                codes.Add(Code128Tables.CodeC);
                set = 'C';
                continue;
            }

            if (value[i] < 32) {
                codes.Add(Code128Tables.CodeA);
                set = 'A';
                continue;
            }
            EmitCodeBChar(value[i], codes);
            i++;
        }

        // Checksum
        var checksum = codes[0];
        for (var pos = 1; pos < codes.Count; pos++) checksum = (checksum + (codes[pos] * pos)) % 103;
        codes.Add(checksum);

        // Stop
        codes.Add(Code128Tables.Stop);
        return codes.ToArray();
    }

    private static void EmitCodeBChar(char ch, List<int> codes) {
        if (ch == Gs1.GroupSeparator) {
            codes.Add(Code128Tables.Fnc1);
            return;
        }
        if (ch is < (char)32 or > (char)126)
            throw new ArgumentException($"Code 128 (Set B/C) supports ASCII 32..126 for now. Bad char: U+{(int)ch:X4}");
        codes.Add(ch - 32);
    }

    private static void EmitCodeAChar(char ch, List<int> codes) {
        if (ch == Gs1.GroupSeparator) {
            codes.Add(Code128Tables.Fnc1);
            return;
        }
        if (ch > 95)
            throw new ArgumentException($"Code 128 (Set A) supports ASCII 0..95 for now. Bad char: U+{(int)ch:X4}");
        codes.Add(ch);
    }

    private static int CountConsecutiveDigits(string value, int startIndex) {
        var i = startIndex;
        while (i < value.Length) {
            var c = value[i];
            if (c is < '0' or > '9') break;
            i++;
        }
        return i - startIndex;
    }

    private static void AppendSegment(List<BarSegment> segments, bool isBar, int modules) {
        if (modules <= 0) return;

        if (segments.Count > 0) {
            var last = segments[segments.Count - 1];
            if (last.IsBar == isBar) {
                segments[segments.Count - 1] = new BarSegment(isBar, checked(last.Modules + modules));
                return;
            }
        }

        segments.Add(new BarSegment(isBar, modules));
    }
}
