using System;
using System.Numerics;

namespace CodeGlyphX.Mailmark;

/// <summary>Mailmark postcode alphabets, 2D field validation and four-state destination ranking.</summary>
internal static class MailmarkDestination {
    private static readonly string[] Patterns = { "FNFNLLNLS", "FFNNLLNLS", "FFNNNLLNL", "FFNFNLLNL", "FNNLLNLSS", "FNNNLLNLS" };
    private static readonly string[] OutwardPatterns = { "FN", "FFN", "FNN", "FFNN", "FNF", "FFNF" };
    private const string Full = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Limited = "ABDEFGHJLNPQRSTUWXYZ";
    private const string Digits = "0123456789";
    private const string International = "XY11     ";

    /// <summary>Checks Appendix A's full, outward-only, blank and designated special 2D destinations.</summary>
    internal static bool IsValid2DDestination(ReadOnlySpan<char> value) {
        if (value.Length != 9) return false;
        value = TrimPadding(value);
        if (value.IsEmpty || value.SequenceEqual("XY11".AsSpan()) || value.SequenceEqual("XY991AA".AsSpan())) return true;
        // The field definition also permits area-only information, for example NE.
        if (value.Length <= 2 && Matches(value, value.Length == 1 ? "F" : "FF")) return true;
        foreach (var outward in OutwardPatterns) {
            if (value.Length == outward.Length && Matches(value, outward)) return true;
            if (value.Length == outward.Length + 5 && Matches(value.Slice(0, outward.Length), outward)
                && Matches(value.Slice(outward.Length), "NLLNL")) return true;
        }
        return false;
    }

    /// <summary>Checks a blank return field or a complete postcode without a delivery point suffix.</summary>
    internal static bool IsValid2DReturn(ReadOnlySpan<char> value) {
        if (value.Length != 7) return false;
        value = TrimPadding(value);
        if (value.IsEmpty) return true;
        foreach (var outward in OutwardPatterns)
            if (value.Length == outward.Length + 3 && Matches(value.Slice(0, outward.Length), outward)
                && Matches(value.Slice(outward.Length), "NLL")) return true;
        return false;
    }

    private static ReadOnlySpan<char> TrimPadding(ReadOnlySpan<char> value) {
        var length = value.Length;
        while (length > 0 && value[length - 1] == ' ') length--;
        return value.Slice(0, length);
    }

    private static bool Matches(ReadOnlySpan<char> value, string pattern) {
        if (value.Length != pattern.Length) return false;
        for (var i = 0; i < value.Length; i++) if (Alphabet(pattern[i]).IndexOf(value[i]) < 0) return false;
        return true;
    }

    internal static bool TryRank(string value, out BigInteger rank) {
        rank = BigInteger.Zero;
        if (value == International) return true;
        if (value.Length != 9) return false;
        var offset = BigInteger.One;
        foreach (var pattern in Patterns) {
            var size = BigInteger.One;
            var current = BigInteger.Zero;
            var valid = true;
            for (var i = 0; i < pattern.Length; i++) {
                var alphabet = Alphabet(pattern[i]);
                var index = alphabet.IndexOf(value[i]);
                if (index < 0) valid = false;
                current = current * alphabet.Length + Math.Max(index, 0);
                size *= alphabet.Length;
            }
            if (valid) { rank = offset + current; return true; }
            offset += size;
        }
        return false;
    }

    internal static bool TryUnrank(BigInteger rank, out string value) {
        value = string.Empty;
        if (rank.Sign < 0) return false;
        if (rank.IsZero) { value = International; return true; }
        rank--;
        foreach (var pattern in Patterns) {
            var size = BigInteger.One;
            foreach (var type in pattern) size *= Alphabet(type).Length;
            if (rank >= size) { rank -= size; continue; }
            var chars = new char[9];
            for (var i = chars.Length - 1; i >= 0; i--) {
                var alphabet = Alphabet(pattern[i]);
                chars[i] = alphabet[(int)(rank % alphabet.Length)];
                rank /= alphabet.Length;
            }
            value = new string(chars);
            return true;
        }
        return false;
    }

    private static string Alphabet(char type) => type == 'F' ? Full : type == 'L' ? Limited : type == 'N' ? Digits : " ";
}
