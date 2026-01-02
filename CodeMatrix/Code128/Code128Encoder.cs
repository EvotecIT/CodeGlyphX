using System;
using System.Collections.Generic;

namespace CodeMatrix.Code128;

internal static class Code128Encoder {
    public static Barcode1D Encode(string value) {
        var codes = EncodeCodeValues(value);

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

    internal static int[] EncodeCodeValues(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) throw new ArgumentException("Value cannot be empty.", nameof(value));

        var codes = new List<int>(value.Length + 4);

        var digitRun0 = CountConsecutiveDigits(value, 0);
        var inCodeC = digitRun0 >= 4 && (digitRun0 % 2 == 0);
        codes.Add(inCodeC ? Code128Tables.StartC : Code128Tables.StartB);

        var i = 0;
        while (i < value.Length) {
            if (inCodeC) {
                var run = CountConsecutiveDigits(value, i);
                if (run >= 2) {
                    if ((run % 2) == 1) {
                        // Switch to Code B to emit a single digit, then we can re-evaluate switching back to C.
                        codes.Add(Code128Tables.CodeB);
                        inCodeC = false;
                        continue;
                    }

                    var pair = ((value[i] - '0') * 10) + (value[i + 1] - '0');
                    codes.Add(pair);
                    i += 2;
                    continue;
                }

                codes.Add(Code128Tables.CodeB);
                inCodeC = false;
                continue;
            }

            // Code B
            var digits = CountConsecutiveDigits(value, i);
            if (digits >= 4) {
                if ((digits % 2) == 1) {
                    EmitCodeBChar(value[i], codes);
                    i++;
                    continue;
                }

                codes.Add(Code128Tables.CodeC);
                inCodeC = true;
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
        if (ch is < (char)32 or > (char)126)
            throw new ArgumentException($"Code 128 (Set B/C) supports ASCII 32..126 for now. Bad char: U+{(int)ch:X4}");
        codes.Add(ch - 32);
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
