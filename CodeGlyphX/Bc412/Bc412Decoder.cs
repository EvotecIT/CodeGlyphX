using System;

namespace CodeGlyphX.Bc412;

/// <summary>Decodes framed SEMI BC412 modules and validates their check character.</summary>
public static class Bc412Decoder {
    /// <summary>Decodes exact modules without a quiet zone, returning the identifier without its check character.</summary>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out string text) {
        text = string.Empty;
        if (modules.Length < 102 || modules.Length > 234 || (modules.Length - 6) % 12 != 0) return false;
        if (!modules[0] || modules[1] || modules[2] || !modules[modules.Length - 3]
            || modules[modules.Length - 2] || !modules[modules.Length - 1]) return false;
        var values = new int[(modules.Length - 6) / 12];
        var sum = 0;
        for (var i = 0; i < values.Length; i++) {
            var found = false;
            for (var digit = 0; digit < Bc412Tables.Patterns.Length; digit++) {
                if (!Match(modules, i * 12 + 3, Bc412Tables.Patterns[digit])) continue;
                values[i] = digit;
                sum += digit * ((i & 1) == 0 ? 1 : 2);
                found = true;
                break;
            }
            if (!found) return false;
        }
        if (sum % 35 != 0) return false;
        var result = new char[values.Length - 1];
        result[0] = Bc412Tables.Alphabet[values[0]];
        for (var i = 2; i < values.Length; i++) result[i - 1] = Bc412Tables.Alphabet[values[i]];
        text = new string(result);
        return true;
    }

    private static bool Match(ReadOnlySpan<bool> modules, int offset, string pattern) {
        var dark = true;
        foreach (var run in pattern) {
            for (var i = 0; i < run - '0'; i++) if (modules[offset++] != dark) return false;
            dark = !dark;
        }
        return true;
    }
}
