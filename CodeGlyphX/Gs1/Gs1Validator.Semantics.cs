using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

public static partial class Gs1Validator {
    private static readonly int[] AlphaCheckPrimes = {
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
        73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151,
        157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239,
        241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337,
        347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433,
        439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509
    };

    private const string CharacterSet82 = "!\"%&'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
    private const string AlphaCheckCharacterSet32 = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    internal static bool TryApplySemanticRule(
        string rule,
        string value,
        Gs1ApplicationIdentifier definition,
        int position,
        List<Gs1ValidationIssue> issues) {
        bool valid;
        switch (rule) {
            case "csum": valid = HasValidMod10CheckDigit(value); break;
            case "csumalpha": valid = HasValidAlphaCheckPair(value); break;
            case "gcppos1": valid = HasNumericPrefix(value, 0, 4); break;
            case "gcppos2": valid = HasNumericPrefix(value, 1, 4); break;
            case "yymmd0": valid = IsValidDate(value, fourDigitYear: false, allowDayZero: true); break;
            case "yymmdd": valid = IsValidDate(value, fourDigitYear: false, allowDayZero: false); break;
            case "yyyymmd0": valid = IsValidDate(value, fourDigitYear: true, allowDayZero: true); break;
            case "yyyymmdd": valid = IsValidDate(value, fourDigitYear: true, allowDayZero: false); break;
            case "hh": valid = IsValidClockPart(value, 23); break;
            case "mi":
            case "ss": valid = IsValidClockPart(value, 59); break;
            case "hhmi": valid = value.Length == 4 && IsValidClockPart(value.Substring(0, 2), 23) && IsValidClockPart(value.Substring(2, 2), 59); break;
            case "zero": valid = value.Length > 0 && ContainsOnly(value, '0'); break;
            case "nonzero": valid = ContainsNonZeroDigit(value); break;
            case "nozeroprefix": valid = value.Length == 0 || (value[0] != '0' && ContainsOnlyDigits(value)); break;
            case "hasnondigit": valid = ContainsNonDigit(value); break;
            case "hyphen": valid = value.Length > 0 && ContainsOnly(value, '-'); break;
            case "yesno": valid = value == "0" || value == "1"; break;
            case "winding": valid = value == "0" || value == "1" || value == "9"; break;
            case "iso5218": valid = value == "0" || value == "1" || value == "2" || value == "9"; break;
            case "latitude": valid = IsCoordinate(value, 1800000000UL); break;
            case "longitude": valid = IsCoordinate(value, 3600000000UL); break;
            case "pieceoftotal": valid = IsValidPieceOfTotal(value); break;
            case "posinseqslash": valid = IsValidPositionInSequence(value); break;
            case "pcenc": valid = HasValidPercentEncoding(value); break;
            case "importeridx": valid = value.Length == 1 && IsImporterIndexCharacter(value[0]); break;
            case "iso3166": valid = IsIso3166Numeric(value); break;
            case "iso3166999": valid = value == "999" || IsIso3166Numeric(value); break;
            case "iso3166alpha2": valid = IsIso3166Alpha2(value); break;
            case "iso4217": valid = IsIso4217Numeric(value); break;
            case "mediatype": valid = IsMediaType(value); break;
            case "packagetype": valid = Array.BinarySearch(PackageTypes, value, StringComparer.Ordinal) >= 0; break;
            case "iban": valid = IsValidIban(value); break;
            case "couponposoffer": valid = IsValidPositiveOfferCoupon(value); break;
            case "couponcode": valid = IsValidNorthAmericanCoupon(value); break;
            default: return false;
        }

        if (!valid) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.InvalidData,
                definition.Ai,
                position,
                $"Data does not satisfy the GS1 '{rule}' syntax rule."));
        }
        return true;
    }

    private static bool HasValidMod10CheckDigit(string value) {
        if (value.Length == 0 || !ContainsOnlyDigits(value)) return false;
        var weight = value.Length % 2 == 0 ? 3 : 1;
        var sum = 0;
        for (var i = 0; i < value.Length - 1; i++) {
            sum += weight * (value[i] - '0');
            weight = 4 - weight;
        }
        return (10 - sum % 10) % 10 == value[value.Length - 1] - '0';
    }

    private static bool HasValidAlphaCheckPair(string value) {
        if (value.Length < 2 || value.Length > AlphaCheckPrimes.Length + 2) return false;
        if (value.Length == 2) return value == "22";
        var sum = 0L;
        for (var i = 0; i < value.Length - 2; i++) {
            var characterValue = CharacterSet82.IndexOf(value[i]);
            if (characterValue < 0) return false;
            sum += characterValue * AlphaCheckPrimes[value.Length - 3 - i];
        }
        sum %= 1021;
        return value[value.Length - 2] == AlphaCheckCharacterSet32[(int)(sum >> 5)]
            && value[value.Length - 1] == AlphaCheckCharacterSet32[(int)(sum & 31)];
    }

    private static bool HasNumericPrefix(string value, int offset, int length) {
        if (value.Length < offset + length) return false;
        for (var i = offset; i < offset + length; i++) {
            if (value[i] < '0' || value[i] > '9') return false;
        }
        return true;
    }

    private static bool IsValidDate(string value, bool fourDigitYear, bool allowDayZero) {
        var expectedLength = fourDigitYear ? 8 : 6;
        if (value.Length != expectedLength || !ContainsOnlyDigits(value)) return false;
        var yearDigits = fourDigitYear ? 4 : 2;
        var year = ParseDigits(value, 0, yearDigits);
        if (!fourDigitYear) year += 2000;
        var month = ParseDigits(value, yearDigits, 2);
        var day = ParseDigits(value, yearDigits + 2, 2);
        if (month < 1 || month > 12) return false;
        if (day == 0) return allowDayZero;
        return day <= DaysInMonth(year, month);
    }

    private static int DaysInMonth(int year, int month) {
        if (month == 2) {
            var leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            return leap ? 29 : 28;
        }
        return month == 4 || month == 6 || month == 9 || month == 11 ? 30 : 31;
    }

    private static bool IsValidClockPart(string value, int maximum) {
        return value.Length == 2 && ContainsOnlyDigits(value) && ParseDigits(value, 0, 2) <= maximum;
    }

    private static bool ContainsOnly(string value, char expected) {
        for (var i = 0; i < value.Length; i++) if (value[i] != expected) return false;
        return true;
    }

    private static bool ContainsOnlyDigits(string value) {
        for (var i = 0; i < value.Length; i++) if (value[i] < '0' || value[i] > '9') return false;
        return true;
    }

    private static bool ContainsNonZeroDigit(string value) {
        var found = false;
        for (var i = 0; i < value.Length; i++) {
            if (value[i] < '0' || value[i] > '9') return false;
            if (value[i] != '0') found = true;
        }
        return found;
    }

    private static bool ContainsNonDigit(string value) {
        for (var i = 0; i < value.Length; i++) if (value[i] < '0' || value[i] > '9') return true;
        return false;
    }

    private static bool IsCoordinate(string value, ulong maximum) {
        if (value.Length != 10 || !ContainsOnlyDigits(value)) return false;
        return ulong.TryParse(value, out var parsed) && parsed <= maximum;
    }

    private static bool IsValidPieceOfTotal(string value) {
        if (value.Length == 0 || value.Length % 2 != 0 || !ContainsOnlyDigits(value)) return false;
        var half = value.Length / 2;
        var piece = value.Substring(0, half);
        var total = value.Substring(half);
        if (ContainsOnly(piece, '0') || ContainsOnly(total, '0')) return false;
        return string.CompareOrdinal(piece, total) <= 0;
    }

    private static bool IsValidPositionInSequence(string value) {
        var slash = value.IndexOf('/');
        if (slash <= 0 || slash >= value.Length - 1 || value.IndexOf('/', slash + 1) >= 0) return false;
        var position = value.Substring(0, slash);
        var end = value.Substring(slash + 1);
        if (!ContainsOnlyDigits(position) || !ContainsOnlyDigits(end) || position[0] == '0' || end[0] == '0') return false;
        if (position.Length != end.Length) return position.Length < end.Length;
        return string.CompareOrdinal(position, end) <= 0;
    }

    private static bool HasValidPercentEncoding(string value) {
        for (var i = 0; i < value.Length; i++) {
            if (value[i] != '%') continue;
            if (i + 2 >= value.Length || !IsHex(value[i + 1]) || !IsHex(value[i + 2])) return false;
            i += 2;
        }
        return true;
    }

    private static bool IsHex(char value) {
        return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
    }

    private static bool IsImporterIndexCharacter(char value) {
        return value == '-' || value == '_' ||
            (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'Z') ||
            (value >= 'a' && value <= 'z');
    }

    private static bool IsValidIban(string value) {
        if (value.Length <= 10 || value.Length > 34 || !IsIso3166Alpha2(value.Substring(0, Math.Min(2, value.Length)))) return false;
        var remainder = 0;
        for (var i = 4; i < value.Length + 4; i++) {
            var character = value[i < value.Length ? i : i - value.Length];
            if (character >= '0' && character <= '9') {
                remainder = (remainder * 10 + character - '0') % 97;
            } else if (character >= 'A' && character <= 'Z') {
                remainder = (remainder * 100 + character - 'A' + 10) % 97;
            } else {
                return false;
            }
        }
        return remainder == 1;
    }

    private static bool IsValidPositiveOfferCoupon(string value) {
        if (!ContainsOnlyDigits(value) || value.Length == 0 || (value[0] != '0' && value[0] != '1')) return false;
        var offset = 1;
        if (!TryConsumeCouponVli(value, ref offset, '0', '6', 6, allowNineAsZero: false)) return false;
        if (!TryConsumeCouponFixed(value, ref offset, 6)) return false;
        if (!TryConsumeCouponVli(value, ref offset, '0', '9', 6, allowNineAsZero: false)) return false;
        return offset == value.Length;
    }

    private static bool IsValidNorthAmericanCoupon(string value) {
        if (!ContainsOnlyDigits(value)) return false;
        var offset = 0;
        if (!TryConsumeCouponVli(value, ref offset, '0', '6', 6, allowNineAsZero: false)) return false;
        if (!TryConsumeCouponFixed(value, ref offset, 6)) return false;
        if (!TryConsumeCouponVli(value, ref offset, '1', '5', 0, allowNineAsZero: false)) return false;
        if (!TryConsumeCouponVli(value, ref offset, '1', '5', 0, allowNineAsZero: false)) return false;
        if (!TryConsumeCouponPurchaseCodeAndFamily(value, ref offset)) return false;

        if (offset < value.Length && value[offset] == '1') {
            offset++;
            if (offset >= value.Length || value[offset] > '3') return false;
            offset++;
            if (!TryConsumeCouponVli(value, ref offset, '1', '5', 0, allowNineAsZero: false)) return false;
            if (!TryConsumeCouponPurchaseCodeAndFamily(value, ref offset)) return false;
            if (!TryConsumeCouponVli(value, ref offset, '0', '6', 6, allowNineAsZero: true)) return false;
        }

        if (offset < value.Length && value[offset] == '2') {
            offset++;
            if (!TryConsumeCouponVli(value, ref offset, '1', '5', 0, allowNineAsZero: false)) return false;
            if (!TryConsumeCouponPurchaseCodeAndFamily(value, ref offset)) return false;
            if (!TryConsumeCouponVli(value, ref offset, '0', '6', 6, allowNineAsZero: true)) return false;
        }

        string? expiration = null;
        if (offset < value.Length && value[offset] == '3') {
            offset++;
            if (!TryReadCouponDate(value, ref offset, out expiration)) return false;
        }

        if (offset < value.Length && value[offset] == '4') {
            offset++;
            if (!TryReadCouponDate(value, ref offset, out var start)) return false;
            if (expiration is not null && string.CompareOrdinal(start, expiration) > 0) return false;
        }

        if (offset < value.Length && value[offset] == '5') {
            offset++;
            if (!TryConsumeCouponVli(value, ref offset, '0', '9', 6, allowNineAsZero: false)) return false;
        }

        if (offset < value.Length && value[offset] == '6') {
            offset++;
            if (!TryConsumeCouponVli(value, ref offset, '1', '7', 6, allowNineAsZero: false)) return false;
        }

        if (offset < value.Length && value[offset] == '9') {
            offset++;
            if (offset + 4 > value.Length) return false;
            var saveValueCode = value[offset++];
            if (saveValueCode != '0' && saveValueCode != '1' && saveValueCode != '2' && saveValueCode != '5' && saveValueCode != '6') return false;
            if (value[offset++] > '2') return false;
            offset++; // Store Coupon Flag has no restricted value in the reference linter.
            var doNotMultiply = value[offset++];
            if (doNotMultiply != '0' && doNotMultiply != '1') return false;
        }

        return offset == value.Length;
    }

    private static bool TryConsumeCouponVli(
        string value,
        ref int offset,
        char minimumIndicator,
        char maximumIndicator,
        int additionalLength,
        bool allowNineAsZero) {
        if (offset >= value.Length) return false;
        var indicator = value[offset++];
        if (allowNineAsZero && indicator == '9') return true;
        if (indicator < minimumIndicator || indicator > maximumIndicator) return false;
        return TryConsumeCouponFixed(value, ref offset, indicator - '0' + additionalLength);
    }

    private static bool TryConsumeCouponFixed(string value, ref int offset, int length) {
        if (length < 0 || value.Length - offset < length) return false;
        offset += length;
        return true;
    }

    private static bool TryConsumeCouponPurchaseCodeAndFamily(string value, ref int offset) {
        if (offset >= value.Length) return false;
        var code = value[offset++];
        if (code > '4' && code != '9') return false;
        return TryConsumeCouponFixed(value, ref offset, 3);
    }

    private static bool TryReadCouponDate(string value, ref int offset, out string date) {
        date = string.Empty;
        if (value.Length - offset < 6) return false;
        date = value.Substring(offset, 6);
        offset += 6;
        return IsValidDate(date, fourDigitYear: false, allowDayZero: false);
    }

    private static int ParseDigits(string value, int offset, int length) {
        var result = 0;
        for (var i = 0; i < length; i++) result = result * 10 + value[offset + i] - '0';
        return result;
    }
}
