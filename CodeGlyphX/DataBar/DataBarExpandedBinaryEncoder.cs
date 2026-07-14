using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Compacts a GS1 element string into GS1 DataBar Expanded 12-bit data characters.
/// </summary>
internal static class DataBarExpandedBinaryEncoder {
    private const char GroupSeparator = Gs1.GroupSeparator;
    private const string Padding = "00100";

    internal static DataBarExpandedBinary Encode(string value, bool requireEvenTotalCharacters) {
        var content = Gs1.ElementString(value);
        ValidateCharacters(content);

        var bits = new List<bool>(128);
        var method = SelectMethod(content);
        var finalMode = EncodeByMethod(content, method, bits);
        var dataCharacters = GetDataCharacterCount(bits.Count, requireEvenTotalCharacters);
        var totalCharacters = dataCharacters + 1;
        if (finalMode == GeneralFieldMode.Numeric && (dataCharacters * 12) - bits.Count >= 7) {
            AppendBits(bits, 0, 4);
            finalMode = GeneralFieldMode.Alpha;
            dataCharacters = GetDataCharacterCount(bits.Count, requireEvenTotalCharacters);
            totalCharacters = dataCharacters + 1;
        }
        if (totalCharacters > 22) {
            throw new ArgumentException("The GS1 element string exceeds the GS1 DataBar Expanded capacity.", nameof(value));
        }

        PatchVariableHeader(bits, method, totalCharacters);
        Pad(bits, dataCharacters * 12, finalMode);

        var values = new int[dataCharacters];
        var position = 0;
        for (var i = 0; i < values.Length; i++) {
            var characterValue = 0;
            for (var bit = 0; bit < 12; bit++) {
                characterValue <<= 1;
                if (bits[position++]) characterValue |= 1;
            }
            values[i] = characterValue;
        }

        return new DataBarExpandedBinary(content, totalCharacters, values);
    }

    private static GeneralFieldMode? EncodeByMethod(string content, int method, List<bool> bits) {
        switch (method) {
            case 1:
                return EncodeGtinMethod(content, bits);
            case 2:
                AppendBits(bits, 0, 5);
                return EncodeGeneralField(content, bits);
            case 3:
            case 4:
                EncodeWeightMethod(content, method, bits);
                return null;
            case 5:
            case 6:
                return EncodePriceMethod(content, method, bits);
            default:
                EncodeWeightAndDateMethod(content, method, bits);
                return null;
        }
    }

    private static GeneralFieldMode? EncodeGtinMethod(string content, List<bool> bits) {
        AppendBits(bits, 4, 4);

        var firstDigit = content[2] - '0';
        AppendBits(bits, firstDigit, 4);
        AppendGtinGroups(content, bits);

        if (content.Length > 16) {
            return EncodeGeneralField(content.Substring(16), bits);
        }
        return GeneralFieldMode.Numeric;
    }

    private static void EncodeWeightMethod(string content, int method, List<bool> bits) {
        AppendBits(bits, method == 3 ? 4 : 5, 5);
        AppendGtinGroups(content, bits);

        var weight = ParseDigits(content, 20, 6);
        if (method == 4 && content[19] == '3') weight += 10000;
        AppendBits(bits, weight, 15);
    }

    private static GeneralFieldMode EncodePriceMethod(string content, int method, List<bool> bits) {
        AppendBits(bits, method == 5 ? 48 : 52, 8);
        AppendGtinGroups(content, bits);
        AppendBits(bits, content[19] - '0', 2);

        var generalStart = 20;
        if (method == 6) {
            AppendBits(bits, ParseDigits(content, 20, 3), 10);
            generalStart = 23;
        }
        return EncodeGeneralField(content.Substring(generalStart), bits);
    }

    private static void EncodeWeightAndDateMethod(string content, int method, List<bool> bits) {
        AppendBits(bits, 56 + (method - 7), 8);
        AppendGtinGroups(content, bits);

        var weight = ((content[19] - '0') * 100000) + ParseDigits(content, 20, 6);
        AppendBits(bits, weight, 20);

        var year = ParseDigits(content, 28, 2);
        var month = ParseDigits(content, 30, 2);
        var day = ParseDigits(content, 32, 2);
        AppendBits(bits, (year * 384) + ((month - 1) * 32) + day, 16);
    }

    private static void AppendGtinGroups(string content, List<bool> bits) {
        for (var group = 0; group < 4; group++) {
            AppendBits(bits, ParseDigits(content, 3 + (group * 3), 3), 10);
        }
    }

    private static GeneralFieldMode EncodeGeneralField(string content, List<bool> bits) {
        var mode = GeneralFieldMode.Numeric;
        var index = 0;

        while (index < content.Length) {
            switch (mode) {
                case GeneralFieldMode.Numeric:
                    if (index + 1 < content.Length && IsNumeric(content[index]) && IsNumeric(content[index + 1])) {
                        var first = NumericValue(content[index]);
                        var second = NumericValue(content[index + 1]);
                        AppendBits(bits, 8 + (first * 11) + second, 7);
                        index += 2;
                    } else if (index + 1 == content.Length && IsNumeric(content[index])) {
                        AppendBits(bits, 8 + (NumericValue(content[index]) * 11) + 10, 7);
                        index++;
                    } else {
                        AppendBits(bits, 0, 4);
                        mode = GeneralFieldMode.Alpha;
                    }
                    break;

                case GeneralFieldMode.Alpha:
                    if (CountNumericRun(content, index) >= 6) {
                        AppendBits(bits, 0, 3);
                        mode = GeneralFieldMode.Numeric;
                    } else if (TryEncodeAlpha(content[index], bits)) {
                        index++;
                    } else {
                        AppendBits(bits, 4, 5);
                        mode = GeneralFieldMode.Iso646;
                    }
                    break;

                default:
                    if (CountNumericRun(content, index) >= 6) {
                        AppendBits(bits, 0, 3);
                        mode = GeneralFieldMode.Numeric;
                    } else if (CountAlphaRun(content, index) >= 6) {
                        AppendBits(bits, 4, 5);
                        mode = GeneralFieldMode.Alpha;
                    } else {
                        EncodeIso646(content[index], bits);
                        index++;
                    }
                    break;
            }
        }

        return mode;
    }

    private static bool TryEncodeAlpha(char value, List<bool> bits) {
        if (value >= '0' && value <= '9') {
            AppendBits(bits, 5 + (value - '0'), 5);
            return true;
        }
        if (value == GroupSeparator) {
            AppendBits(bits, 15, 5);
            return true;
        }
        if (value >= 'A' && value <= 'Z') {
            AppendBits(bits, 32 + (value - 'A'), 6);
            return true;
        }

        var encoded = value switch {
            '*' => 58,
            ',' => 59,
            '-' => 60,
            '.' => 61,
            '/' => 62,
            _ => -1
        };
        if (encoded < 0) return false;
        AppendBits(bits, encoded, 6);
        return true;
    }

    private static void EncodeIso646(char value, List<bool> bits) {
        if (value >= '0' && value <= '9') {
            AppendBits(bits, 5 + (value - '0'), 5);
            return;
        }
        if (value == GroupSeparator) {
            AppendBits(bits, 15, 5);
            return;
        }
        if (value >= 'A' && value <= 'Z') {
            AppendBits(bits, value - 1, 7);
            return;
        }
        if (value >= 'a' && value <= 'z') {
            AppendBits(bits, value - 7, 7);
            return;
        }

        var encoded = value switch {
            '!' => 232,
            '"' => 233,
            '%' => 234,
            '&' => 235,
            '\'' => 236,
            '(' => 237,
            ')' => 238,
            '*' => 239,
            '+' => 240,
            ',' => 241,
            '-' => 242,
            '.' => 243,
            '/' => 244,
            ':' => 245,
            ';' => 246,
            '<' => 247,
            '=' => 248,
            '>' => 249,
            '?' => 250,
            '_' => 251,
            ' ' => 252,
            _ => -1
        };
        if (encoded < 0) throw new ArgumentException($"Character U+{(int)value:X4} is not supported by GS1 DataBar Expanded.");
        AppendBits(bits, encoded, 8);
    }

    private static int SelectMethod(string content) {
        if (!CanUseGtinMethod(content)) return 2;
        if (content[2] != '9') return 1;

        var weightAndDateMethod = TrySelectWeightAndDateMethod(content);
        if (weightAndDateMethod != 0) return weightAndDateMethod;

        if (content.Length == 26 && Matches(content, 16, "3103")) {
            var weight = TryParseDigits(content, 20, 6);
            if (weight >= 0 && weight <= 32767) return 3;
        }
        if (content.Length == 26 && (Matches(content, 16, "3202") || Matches(content, 16, "3203"))) {
            var weight = TryParseDigits(content, 20, 6);
            if (weight >= 0 && ((content[19] == '2' && weight <= 9999) || (content[19] == '3' && weight <= 22767))) return 4;
        }
        if (content.Length >= 20 && Matches(content, 16, "392") && content[19] >= '0' && content[19] <= '3') return 5;
        if (content.Length >= 23 && Matches(content, 16, "393") && content[19] >= '0' && content[19] <= '3'
            && TryParseDigits(content, 20, 3) >= 0) return 6;
        return 1;
    }

    private static int TrySelectWeightAndDateMethod(string content) {
        if (content.Length != 34) return 0;
        var isMetric = Matches(content, 16, "310");
        var isImperial = Matches(content, 16, "320");
        if (!isMetric && !isImperial) return 0;
        if (content[19] < '0' || content[19] > '9') return 0;
        var weight = TryParseDigits(content, 20, 6);
        if (weight < 0 || weight > 99999) return 0;

        var dateOffset = Matches(content, 26, "11") ? 0
            : Matches(content, 26, "13") ? 2
            : Matches(content, 26, "15") ? 4
            : Matches(content, 26, "17") ? 6
            : -1;
        if (dateOffset < 0) return 0;

        var month = TryParseDigits(content, 30, 2);
        var day = TryParseDigits(content, 32, 2);
        if (TryParseDigits(content, 28, 2) < 0 || month < 1 || month > 12 || day < 1 || day > 31) return 0;
        return 7 + dateOffset + (isImperial ? 1 : 0);
    }

    private static bool CanUseGtinMethod(string content) {
        if (content.Length < 16 || content[0] != '0' || content[1] != '1') return false;
        for (var i = 2; i < 16; i++) {
            if (content[i] < '0' || content[i] > '9') return false;
        }

        var expectedCheck = ComputeGtinCheckDigit(content, 2);
        if (content[15] - '0' != expectedCheck) {
            throw new FormatException("AI (01) contains an invalid GTIN check digit.");
        }
        return true;
    }

    private static int ParseDigits(string content, int start, int count) {
        var value = TryParseDigits(content, start, count);
        if (value < 0) throw new FormatException("GS1 numeric data contains a non-numeric character.");
        return value;
    }

    private static int TryParseDigits(string content, int start, int count) {
        if (start < 0 || count < 0 || start + count > content.Length) return -1;
        var value = 0;
        for (var i = 0; i < count; i++) {
            var character = content[start + i];
            if (character < '0' || character > '9') return -1;
            value = (value * 10) + (character - '0');
        }
        return value;
    }

    private static bool Matches(string content, int start, string expected) {
        if (start < 0 || start + expected.Length > content.Length) return false;
        for (var i = 0; i < expected.Length; i++) {
            if (content[start + i] != expected[i]) return false;
        }
        return true;
    }

    private static int ComputeGtinCheckDigit(string content, int start) {
        var sum = 0;
        for (var i = 0; i < 13; i++) {
            var digit = content[start + i] - '0';
            sum += (i & 1) == 0 ? digit * 3 : digit;
        }
        return (10 - (sum % 10)) % 10;
    }

    private static int CountNumericRun(string content, int start) {
        var count = 0;
        while (start + count < content.Length && IsNumeric(content[start + count])) count++;
        return count;
    }

    private static int CountAlphaRun(string content, int start) {
        var count = 0;
        while (start + count < content.Length && IsAlpha(content[start + count])) count++;
        return count;
    }

    private static bool IsAlpha(char value) {
        return (value >= '0' && value <= '9')
            || value == GroupSeparator
            || (value >= 'A' && value <= 'Z')
            || value is '*' or ',' or '-' or '.' or '/';
    }

    private static bool IsNumeric(char value) {
        return (value >= '0' && value <= '9') || value == GroupSeparator;
    }

    private static int NumericValue(char value) {
        return value == GroupSeparator ? 10 : value - '0';
    }

    private static void ValidateCharacters(string content) {
        for (var i = 0; i < content.Length; i++) {
            var value = content[i];
            if (IsAlpha(value) || (value >= 'a' && value <= 'z') || IsIsoPunctuation(value)) continue;
            throw new ArgumentException($"Character U+{(int)value:X4} is not supported by GS1 DataBar Expanded.", nameof(content));
        }
    }

    private static bool IsIsoPunctuation(char value) {
        return value is '!' or '"' or '%' or '&' or '\'' or '(' or ')' or '+' or ':' or ';' or '<' or '=' or '>' or '?' or '_' or ' ';
    }

    private static void PatchVariableHeader(List<bool> bits, int method, int totalCharacters) {
        var odd = (totalCharacters & 1) != 0;
        var large = totalCharacters > 14;
        if (method == 1) {
            bits[2] = odd;
            bits[3] = large;
        } else if (method == 2) {
            bits[3] = odd;
            bits[4] = large;
        } else if (method is 5 or 6) {
            bits[6] = odd;
            bits[7] = large;
        }
    }

    private static int GetDataCharacterCount(int bitCount, bool requireEvenTotalCharacters) {
        var dataCharacters = Math.Max(3, (bitCount + 11) / 12);
        if (requireEvenTotalCharacters && ((dataCharacters + 1) & 1) != 0) dataCharacters++;
        return dataCharacters;
    }

    private static void Pad(List<bool> bits, int targetLength, GeneralFieldMode? finalMode) {
        if (finalMode == GeneralFieldMode.Numeric) {
            while (bits.Count < targetLength) bits.Add(false);
            return;
        }

        var paddingIndex = 0;
        while (bits.Count < targetLength) {
            bits.Add(Padding[paddingIndex] == '1');
            paddingIndex = (paddingIndex + 1) % Padding.Length;
        }
    }

    private static void AppendBits(List<bool> bits, int value, int count) {
        for (var bit = count - 1; bit >= 0; bit--) {
            bits.Add(((value >> bit) & 1) != 0);
        }
    }

    private enum GeneralFieldMode {
        Numeric,
        Alpha,
        Iso646
    }
}

internal sealed class DataBarExpandedBinary {
    internal DataBarExpandedBinary(string content, int totalCharacters, int[] dataValues) {
        Content = content;
        TotalCharacters = totalCharacters;
        DataValues = dataValues;
    }

    internal string Content { get; }
    internal int TotalCharacters { get; }
    internal int[] DataValues { get; }
}
