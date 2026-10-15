using System;

namespace CodeGlyphX.Bc412;

/// <summary>Decodes SEMI and IBM BC412 modules using explicit checksum and framing conventions.</summary>
public static class Bc412Decoder {
    /// <summary>Decodes exact modules without a quiet zone, returning data without the declared check character.</summary>
    /// <remarks>Unframed IBM input must retain its final spaces. Check presence cannot be inferred from the data.</remarks>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out string text, Bc412Variant variant = Bc412Variant.Semi,
        bool hasChecksum = true, bool hasStartStop = true) {
        text = string.Empty;
        if (variant == Bc412Variant.Ibm) return TryDecodeIbm(modules, hasChecksum, hasStartStop, out text);
        if (variant != Bc412Variant.Semi || !hasChecksum || !hasStartStop) return false;
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

    private static bool TryDecodeIbm(ReadOnlySpan<bool> modules, bool checksum, bool guards, out string text) {
        text = string.Empty;
        var offset = guards ? 3 : 0;
        var dataModules = modules.Length - (guards ? 6 : 0);
        if (dataModules <= 0 || dataModules % 12 != 0) return false;
        var count = dataModules / 12 - (checksum ? 1 : 0);
        if (count < 1 || count > 500) return false;
        if (guards && (!modules[0] || modules[1] || modules[2] || !modules[modules.Length - 3]
            || modules[modules.Length - 2] || !modules[modules.Length - 1])) return false;
        var characters = new char[count];
        var sum = 0;
        for (var i = 0; i < count + (checksum ? 1 : 0); i++) {
            var digit = -1;
            for (var at = 0; at < Bc412Tables.Patterns.Length; at++)
                if (Match(modules, offset + i * 12, Bc412Tables.Patterns[at])) { digit = at; break; }
            if (digit < 0) return false;
            if (i == count) {
                if (digit != sum % 35) return false;
            } else { sum += digit; characters[i] = Bc412Tables.Alphabet[digit]; }
        }
        text = new string(characters);
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
