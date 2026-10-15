using System;
using System.Numerics;

namespace CodeGlyphX.Mailmark;

/// <summary>The ranked destination postcode and delivery point suffix used by Mailmark four-state symbols.</summary>
internal static class MailmarkDestination {
    private static readonly string[] Patterns = { "FNFNLLNLS", "FFNNLLNLS", "FFNNNLLNL", "FFNFNLLNL", "FNNLLNLSS", "FNNNLLNLS" };
    private const string Full = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Limited = "ABDEFGHJLNPQRSTUWXYZ";
    private const string Digits = "0123456789";
    private const string International = "XY11     ";

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
