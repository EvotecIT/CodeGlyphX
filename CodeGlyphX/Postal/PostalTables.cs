using System;

namespace CodeGlyphX.Postal;

internal static class PostalTables {
    internal static readonly int[] Weights = { 7, 4, 2, 1, 0 };
    internal static readonly bool[][] PostnetPatterns = BuildPostnetPatterns();

    internal static int CalcChecksum(ReadOnlySpan<char> digits) {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++) {
            var digit = digits[i] - '0';
            if ((uint)digit > 9) return -1;
            sum += digit;
        }
        return (10 - (sum % 10)) % 10;
    }

    internal static bool IsChecksumLength(int length) => length == 6 || length == 10 || length == 12;

    private static bool[][] BuildPostnetPatterns() {
        var patterns = new bool[10][];
        for (var digit = 0; digit <= 9; digit++) {
            var target = digit == 0 ? 11 : digit;
            var pattern = new bool[5];
            var found = false;
            for (var i = 0; i < Weights.Length && !found; i++) {
                for (var j = i + 1; j < Weights.Length; j++) {
                    if (Weights[i] + Weights[j] != target) continue;
                    pattern[i] = true;
                    pattern[j] = true;
                    found = true;
                    break;
                }
            }
            if (!found) throw new InvalidOperationException($"Unable to build POSTNET pattern for digit {digit}.");
            patterns[digit] = pattern;
        }
        return patterns;
    }
}
