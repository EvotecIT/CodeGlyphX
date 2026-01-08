using System;
using System.Globalization;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Payloads;

internal static class QrPayloadValidation {
    public static bool IsValidIban(string iban) {
        if (string.IsNullOrEmpty(iban)) return false;
        var text = iban.ToUpperInvariant().Replace(" ", "").Replace("-", "");
        var basic = RegexCache.IbanBasic().IsMatch(text);
        if (!basic) return false;

        var rearranged = text.Substring(4) + text.Substring(0, 4);
        var numeric = string.Empty;
        foreach (var c in rearranged) {
            numeric += char.IsLetter(c) ? (c - 55).ToString(CultureInfo.InvariantCulture) : c.ToString(CultureInfo.InvariantCulture);
        }

        var result = 0;
        for (var i = 0; i < (int)Math.Ceiling((numeric.Length - 2) / 7.0); i++) {
            var offset = i != 0 ? 2 : 0;
            var pos = i * 7 + offset;
            if (!int.TryParse(((i == 0) ? "" : result.ToString()) + numeric.Substring(pos, Math.Min(9 - offset, numeric.Length - pos)), NumberStyles.Any, CultureInfo.InvariantCulture, out result)) {
                return false;
            }
            result %= 97;
        }

        return result == 1;
    }

    public static bool IsValidQrIban(string iban) {
        if (string.IsNullOrEmpty(iban)) return false;
        var isQr = false;
        try {
            var segment = iban.ToUpperInvariant().Replace(" ", "").Replace("-", "").Substring(4, 5);
            var num = Convert.ToInt32(segment, CultureInfo.InvariantCulture);
            isQr = num >= 30000 && num <= 31999;
        } catch {
            isQr = false;
        }

        return isQr && IsValidIban(iban);
    }

    public static bool IsValidBic(string? bic, bool required = false) {
        if (string.IsNullOrEmpty(bic)) return !required;
        var value = bic ?? string.Empty;
        return RegexCache.Bic().IsMatch(value.Replace(" ", ""));
    }

    public static bool ChecksumMod10(string digits) {
        if (string.IsNullOrEmpty(digits) || digits.Length < 2) return false;
        int[] table = { 0, 9, 4, 6, 8, 2, 7, 1, 3, 5 };
        var carry = 0;
        for (var i = 0; i < digits.Length - 1; i++) {
            var v = digits[i] - '0';
            carry = table[(v + carry) % 10];
        }
        return (10 - carry) % 10 == digits[digits.Length - 1] - '0';
    }
}
