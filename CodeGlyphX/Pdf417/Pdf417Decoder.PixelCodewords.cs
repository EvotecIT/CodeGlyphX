using System;
using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static readonly int[][][] PixelCodewordRuns = BuildPixelCodewordRuns();
    private static readonly int[][][] PixelCodewordBuckets = BuildPixelCodewordBuckets();

    private static bool ReadPixelCodeword(List<int> runs, int start, int cluster, out int word, bool approximate = true, bool correctPrintGain = false) {
        word = 0;
        if (start + 8 > runs.Count) return false;
        var total = 0;
        for (var i = 0; i < 8; i++) total += runs[start + i];
        var pitch = total / 17.0;
        Span<double> normalized = stackalloc double[8];
        for (var i = 0; i < 8; i++) normalized[i] = runs[start + i] / pitch;
        var bits = 0;
        var modules = 0;
        for (var i = 0; i < 8; i++) {
            var count = (int)Math.Round(normalized[i]);
            if (count < 1 || count > 6) { modules = -100; break; }
            modules += count;
            for (var j = 0; j < count; j++) bits = (bits << 1) | ((i & 1) == 0 ? 1 : 0);
        }
        if (modules == 17 && PatternToCodeword[cluster].TryGetValue(bits, out word)) return true;
        if (!approximate) return false;

        // Every accepted run is within 0.85 modules after a bias of at most
        // 0.65. Index the first three widths using that conservative envelope;
        // the original eight-run fit still decides every candidate's acceptance.
        var margin = (correctPrintGain ? 1.5 : 0.85) + 1e-9;
        var firstMin = Math.Max(1, (int)Math.Ceiling(normalized[0] - margin));
        var firstMax = Math.Min(6, (int)Math.Floor(normalized[0] + margin));
        var secondMin = Math.Max(1, (int)Math.Ceiling(normalized[1] - margin));
        var secondMax = Math.Min(6, (int)Math.Floor(normalized[1] + margin));
        var thirdMin = Math.Max(1, (int)Math.Ceiling(normalized[2] - margin));
        var thirdMax = Math.Min(6, (int)Math.Floor(normalized[2] + margin));
        var bestError = 1.5;
        var found = false;
        for (var first = firstMin; first <= firstMax; first++) {
            for (var second = secondMin; second <= secondMax; second++) {
                for (var third = thirdMin; third <= thirdMax; third++) {
                    var bucket = PixelCodewordBuckets[cluster][(first - 1) * 36 + (second - 1) * 6 + third - 1];
                    foreach (var candidate in bucket) {
                        var widths = PixelCodewordRuns[cluster][candidate];
                        var bias = 0.0;
                        if (correctPrintGain) for (var i = 0; i < 8; i++)
                            bias += (normalized[i] - widths[i]) * ((i & 1) == 0 ? 1 : -1);
                        bias = Math.Max(-0.65, Math.Min(0.65, bias / 8));
                        var error = 0.0;
                        var fits = true;
                        for (var i = 0; i < 8; i++) {
                            var residual = normalized[i] - widths[i] - bias * ((i & 1) == 0 ? 1 : -1);
                            if (Math.Abs(residual) > 0.85) { fits = false; break; }
                            error += residual * residual;
                        }
                        // Buckets change iteration order. Exact score ties keep the
                        // lowest codeword, matching the preceding exhaustive search.
                        if (!fits || error > bestError || error.CompareTo(bestError) == 0 && (!found || candidate >= word)) continue;
                        word = candidate; bestError = error; found = true;
                    }
                }
            }
        }
        return found;
    }

    private static int[][][] BuildPixelCodewordBuckets() {
        var result = new int[3][][];
        for (var cluster = 0; cluster < 3; cluster++) {
            var groups = new List<int>[216];
            for (var word = 0; word < 929; word++) {
                var widths = PixelCodewordRuns[cluster][word];
                var key = (widths[0] - 1) * 36 + (widths[1] - 1) * 6 + widths[2] - 1;
                (groups[key] ??= new List<int>()).Add(word);
            }
            result[cluster] = new int[216][];
            for (var key = 0; key < groups.Length; key++) result[cluster][key] = groups[key]?.ToArray() ?? Array.Empty<int>();
        }
        return result;
    }

    private static int[][][] BuildPixelCodewordRuns() {
        var result = new int[3][][];
        for (var cluster = 0; cluster < 3; cluster++) {
            result[cluster] = new int[929][];
            for (var word = 0; word < 929; word++) {
                var runs = new int[8];
                var index = 0;
                var previous = true;
                for (var bit = 16; bit >= 0; bit--) {
                    var dark = (Pdf417CodewordTable.Table[cluster][word] & (1 << bit)) != 0;
                    if (dark != previous) { index++; previous = dark; }
                    runs[index]++;
                }
                result[cluster][word] = runs;
            }
        }
        return result;
    }
}
