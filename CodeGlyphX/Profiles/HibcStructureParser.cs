using System;
using System.Globalization;

namespace CodeGlyphX;

/// <summary>Supplier structures from ANSI/HIBC 2.6 sections 2.1-2.3 and appendices E/F.</summary>
internal static class HibcStructureParser {
    internal static bool TryParse(string payload, string data, out HibcPayload value) {
        value = null!;
        if (data.Length == 0) return false;
        HibcPrimaryData? primary = null;
        HibcProductionData? production = null;
        if (IsLetter(data[0])) {
            var slash = data.IndexOf('/');
            var main = slash < 0 ? data : data.Substring(0, slash);
            if (main.Length < 6 || main.Length > 23 || !IsAlphaNumeric(main) || !IsDigit(main[main.Length - 1])) return false;
            primary = new HibcPrimaryData(main);
            if (slash >= 0 && !TryProduction(data.Substring(slash + 1), false, out production)) return false;
        } else if (!TryProduction(data, true, out production)) {
            return false;
        }
        if (production?.Quantity.HasValue == true && primary is not null && primary.UnitOfMeasure != 9) return false;
        value = new HibcPayload(payload, data, primary, production);
        return true;
    }

    private static bool TryProduction(string text, bool linked, out HibcProductionData value) {
        value = new HibcProductionData();
        if (text.Length == 0) return false;
        // A slash can itself be the separate symbol's link character. Two
        // consecutive slashes then mean link followed by supplemental delimiter.
        var separator = text.IndexOf('/');
        if (linked && separator >= 0 && (separator == text.Length - 1 || text[separator + 1] == '/')) separator++;
        var field = separator < 0 ? text : text.Substring(0, separator);
        if (linked) {
            if (field.Length == 0) return false;
            value.LinkCharacter = field[field.Length - 1];
            field = field.Substring(0, field.Length - 1);
        }
        var serial = false;
        var position = 0;
        var needsIdentifier = false;
        var bareJulian = false;
        if (field.Length == 0) return false;
        if (IsDigit(field[0])) {
            if (!ReadDate(field, 0, HibcDateFormat.YearJulianDay, 5, value, out position)) return false;
            bareJulian = true;
        } else if (field.StartsWith("$$", StringComparison.Ordinal)) {
            position = field.StartsWith("$$+", StringComparison.Ordinal) ? 3 : 2;
            serial = position == 3;
            if (position >= field.Length) return false;
            var flag = field[position];
            switch (flag) {
                case '0': case '1':
                    if (!ReadDate(field, position, HibcDateFormat.MonthYear, 4, value, out position)) return false;
                    break;
                case '2':
                    if (!ReadDate(field, position + 1, HibcDateFormat.MonthDayYear, 6, value, out position)) return false;
                    break;
                case '3': case '4':
                    if (!ReadDate(field, position + 1, flag == '3' ? HibcDateFormat.YearMonthDay : HibcDateFormat.YearMonthDayHour,
                        flag == '3' ? 6 : 8, value, out position)) return false;
                    break;
                case '5': case '6':
                    if (!ReadDate(field, position + 1, flag == '5' ? HibcDateFormat.YearJulianDay : HibcDateFormat.YearJulianDayHour,
                        flag == '5' ? 5 : 7, value, out position)) return false;
                    break;
                case '7': position++; break;
                default: return false;
            }
        } else if (field.StartsWith("$", StringComparison.Ordinal)) {
            serial = field.StartsWith("$+", StringComparison.Ordinal);
            position = serial ? 2 : 1;
            needsIdentifier = true;
        } else {
            return false;
        }
        var identifier = field.Substring(position);
        if (identifier.Length > (bareJulian ? 13 : 18) || (needsIdentifier && identifier.Length == 0)
            || !IsSecondaryIdentifier(identifier)) return false;
        if (identifier.Length > 0) {
            if (serial) value.SerialNumber = identifier;
            else value.LotNumber = identifier;
            value.HasLegacyLot = bareJulian;
        }
        if (separator < 0 || separator == text.Length) return true;
        var fields = text.Substring(separator + 1).Split('/');
        foreach (var extra in fields) {
            if (value.Quantity.HasValue || extra.Length == 0) return false;
            if (extra.StartsWith("16D", StringComparison.Ordinal)) {
                if (value.ManufactureDate.HasValue || !ReadFullDate(extra.Substring(3), out var date)) return false;
                value.ManufactureDate = date;
            } else if (extra.StartsWith("14D", StringComparison.Ordinal)) {
                if (value.ExpiryDate.HasValue || !ReadFullDate(extra.Substring(3), out var date)) return false;
                value.ExpiryDate = date;
            } else if (extra[0] == 'S') {
                var number = extra.Substring(1);
                if (value.SerialNumber is not null || number.Length < 1 || number.Length > 18 || !IsSecondaryIdentifier(number)) return false;
                value.SerialNumber = number;
            } else if (extra[0] == 'Q') {
                var digits = extra.Substring(1);
                if (digits.Length < 1 || digits.Length > 5 || !IsNumeric(digits)) return false;
                value.Quantity = int.Parse(digits, CultureInfo.InvariantCulture);
            } else {
                return false;
            }
        }
        return true;
    }

    private static bool ReadDate(string text, int start, HibcDateFormat format, int length,
        HibcProductionData value, out int end) {
        end = start + length;
        if (start < 0 || end > text.Length) return false;
        var digits = text.Substring(start, length);
        if (!IsNumeric(digits)) return false;
        var year = format == HibcDateFormat.MonthYear ? Number(digits, 2, 2)
            : format == HibcDateFormat.MonthDayYear ? Number(digits, 4, 2) : Number(digits, 0, 2);
        if (format == HibcDateFormat.YearJulianDay || format == HibcDateFormat.YearJulianDayHour) {
            var ordinal = Number(digits, 2, 3);
            // Preserve a two-digit year; 00 can denote a leap century.
            if (ordinal < 1 || ordinal > (year % 4 == 0 ? 366 : 365)) return false;
        } else {
            var month = Number(digits, format == HibcDateFormat.MonthYear || format == HibcDateFormat.MonthDayYear ? 0 : 2, 2);
            var day = format == HibcDateFormat.MonthYear ? 1
                : Number(digits, format == HibcDateFormat.MonthDayYear ? 2 : 4, 2);
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(2000 + year, month)) return false;
        }
        if ((format == HibcDateFormat.YearMonthDayHour || format == HibcDateFormat.YearJulianDayHour)
            && Number(digits, digits.Length - 2, 2) > 23) return false;
        value.Date = digits; value.DateFormat = format;
        return true;
    }

    private static bool ReadFullDate(string digits, out DateTime date) => DateTime.TryParseExact(digits, "yyyyMMdd",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    private static int Number(string text, int start, int length) => int.Parse(text.Substring(start, length), CultureInfo.InvariantCulture);
    private static bool IsLetter(char c) => c >= 'A' && c <= 'Z';
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsAlphaNumeric(string text) {
        foreach (var c in text) if (!IsLetter(c) && !IsDigit(c)) return false;
        return true;
    }
    private static bool IsSecondaryIdentifier(string text) {
        foreach (var c in text) if (!IsLetter(c) && !IsDigit(c) && c != '.' && c != '-') return false;
        return true;
    }
    private static bool IsNumeric(string text) {
        foreach (var c in text) if (!IsDigit(c)) return false;
        return text.Length > 0;
    }
}
