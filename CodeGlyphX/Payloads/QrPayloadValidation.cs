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

    public static bool IsValidEmail(string? address) {
        if (address is null) return false;
        if (IsWhiteSpace(address)) return false;
        var at = address.IndexOf('@');
        if (at <= 0 || at == address.Length - 1) return false;
        var dot = address.IndexOf('.', at + 1);
        return dot > at + 1 && dot < address.Length - 1;
    }

    public static bool IsValidPhone(string? number) {
        if (number is null) return false;
        if (IsWhiteSpace(number)) return false;
        var digits = 0;
        for (var i = 0; i < number.Length; i++) {
            var ch = number[i];
            if (char.IsDigit(ch)) {
                digits++;
                continue;
            }
            if (ch is '+' or '-' or ' ' or '(' or ')' or '.') continue;
            return false;
        }
        return digits >= 3;
    }

    public static bool IsValidUrl(string? url) {
        if (url is null) return false;
        if (IsWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public static bool IsValidCurrency(string? currency) {
        if (currency is null) return false;
        if (IsWhiteSpace(currency)) return false;
        if (currency.Length != 3) return false;
        for (var i = 0; i < currency.Length; i++) {
            if (!char.IsLetter(currency[i])) return false;
        }
        return true;
    }

    public static bool IsValidUpiVpa(string? vpa) {
        if (vpa is null) return false;
        if (IsWhiteSpace(vpa)) return false;
        var at = vpa.IndexOf('@');
        if (at <= 0 || at == vpa.Length - 1) return false;
        if (vpa.IndexOf(' ') >= 0) return false;
        return true;
    }

    public static bool IsValidWifiAuth(string? auth) {
        if (auth is null) return true;
        if (IsWhiteSpace(auth)) return true;
        return auth.Equals("WEP", StringComparison.OrdinalIgnoreCase)
               || auth.Equals("WPA", StringComparison.OrdinalIgnoreCase)
               || auth.Equals("WPA2", StringComparison.OrdinalIgnoreCase)
               || auth.Equals("WPA3", StringComparison.OrdinalIgnoreCase)
               || auth.Equals("nopass", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWhiteSpace(string value) {
        for (var i = 0; i < value.Length; i++) {
            if (!char.IsWhiteSpace(value[i])) return false;
        }
        return true;
    }
}
