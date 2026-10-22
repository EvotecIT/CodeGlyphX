using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.Gs1Composite;

internal static partial class CompositeBitStreamCodec {
    // ISO/IEC 24723 section 5.3.1: a leading lot AI, or a packed date
    // followed by an optional lot, can omit their application identifiers.
    private static string EncodeCompactedPrefix(string text, List<bool> bits, ref int mode, out bool alphaPad) {
        alphaPad = false;
        if (text.StartsWith("90", StringComparison.Ordinal)
            && TryEncodeAi90(text, bits, ref mode, out var rest, out alphaPad)) return rest;
        if (text.StartsWith("10", StringComparison.Ordinal)) {
            Append(bits, 2, 2);
            Append(bits, 3, 2);
            return text.Substring(2);
        }
        if (text.Length >= 8 && (text.StartsWith("11", StringComparison.Ordinal)
            || text.StartsWith("17", StringComparison.Ordinal)) && TryPackDate(text.Substring(2, 6), out var date)) {
            Append(bits, 2, 2);
            Append(bits, date, 16);
            Append(bits, text[1] == '7' ? 1 : 0, 1);
            if (text.Length >= 10 && text[8] == '1' && text[9] == '0') return text.Substring(10);
            if (text.Length > 8) return Gs1.GroupSeparator + text.Substring(8);
            // A date with no following lot still carries FNC1. The explicit
            // alpha latch permits a single FNC1 and returns to numeric mode.
            Append(bits, 15, 9);
            return string.Empty;
        }
        bits.Add(false);
        return text;
    }

    private static bool TryDecodeCompressed(bool[] bits, out string text) {
        text = string.Empty;
        if (bits.Length < 4) return false;
        if (bits[1]) return TryDecodeAi90(bits, out text);
        var position = 2;
        if (Peek(bits, position, 2) == 3) {
            position += 2;
            if (!TryDecodeGeneralField(bits, position, Numeric, out var lot)) return false;
            text = "10" + lot;
            return true;
        }
        if (!TryRead(bits, ref position, 16, out var packed) || packed >= 38400
            || !TryRead(bits, ref position, 1, out var expiry)) return false;
        var year = packed / 384;
        var month = packed / 32 % 12 + 1;
        var day = packed % 32;
        if (day > DateTime.DaysInMonth(2000 + year, month)) return false;
        var prefix = (expiry == 0 ? "11" : "17") + year.ToString("D2", CultureInfo.InvariantCulture)
            + month.ToString("D2", CultureInfo.InvariantCulture) + day.ToString("D2", CultureInfo.InvariantCulture);
        if (!TryDecodeGeneralField(bits, position, Numeric, out var general) || general.Length == 0) return false;
        text = prefix + (general[0] == Gs1.GroupSeparator ? general.Substring(1) : "10" + general);
        return true;
    }

    private static bool TryPackDate(string digits, out int value) {
        value = 0;
        foreach (var c in digits) if (c < '0' || c > '9') return false;
        var year = int.Parse(digits.Substring(0, 2), CultureInfo.InvariantCulture);
        var month = int.Parse(digits.Substring(2, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(digits.Substring(4, 2), CultureInfo.InvariantCulture);
        if (month < 1 || month > 12 || day > DateTime.DaysInMonth(2000 + year, month)) return false;
        value = (year * 12 + month - 1) * 32 + day;
        return true;
    }
}
