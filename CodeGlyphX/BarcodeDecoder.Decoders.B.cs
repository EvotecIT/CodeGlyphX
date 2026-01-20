using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code11;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Codabar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Internal;
using CodeGlyphX.Msi;
using CodeGlyphX.Plessey;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodeEan13(bool[] modules, out string text) {
        text = string.Empty;
        var source = modules;
        if (TryExtractBaseModules(source, 95, GuardStart, out var baseModules)) {
            modules = baseModules;
        }
        if (!TryNormalizeModules(modules, 95, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 92, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardCenter)) return false;

        var leftDigits = new char[6];
        var parity = new bool[6];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftOdd, out var digit)) {
                leftDigits[i] = digit;
                parity[i] = false;
            } else if (TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftEven, out digit)) {
                leftDigits[i] = digit;
                parity[i] = true;
            } else {
                return false;
            }
        }

        var rightDigits = new char[6];
        offset = 3 + 6 * 7 + 5;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.Right, out var digit)) return false;
            rightDigits[i] = digit;
        }

        var firstDigit = EanParityMap.Value[ParityKey(parity)];
        var raw = firstDigit + new string(leftDigits) + new string(rightDigits);
        if (!IsValidEanChecksum(raw)) return false;
        if (TryDecodeAddOn(source, 95, GuardStart, out var addOn)) {
            text = raw + "+" + addOn;
        } else {
            text = raw;
        }
        return true;
    }

    private static bool TryDecodeUpcA(bool[] modules, out string text) {
        text = string.Empty;
        var source = modules;
        if (TryExtractBaseModules(source, 95, GuardStart, out var baseModules)) {
            modules = baseModules;
        }
        if (!TryNormalizeModules(modules, 95, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 92, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardCenter)) return false;

        var digits = new char[12];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchUpcDigit(modules, offset + i * 7, true, out var digit)) return false;
            digits[i] = digit;
        }
        offset = 3 + 6 * 7 + 5;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchUpcDigit(modules, offset + i * 7, false, out var digit)) return false;
            digits[i + 6] = digit;
        }
        var raw = new string(digits);
        if (!IsValidUpcAChecksum(raw)) return false;
        if (TryDecodeAddOn(source, 95, GuardStart, out var addOn)) {
            text = raw + "+" + addOn;
        } else {
            text = raw;
        }
        return true;
    }

    private static bool TryDecodeUpcE(bool[] modules, out string text) {
        text = string.Empty;
        var source = modules;
        if (TryExtractBaseModules(source, 51, GuardUpcEEnd, out var baseModules)) {
            modules = baseModules;
        }
        if (!TryNormalizeModules(modules, 51, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardUpcEEnd)) return false;

        var digits = new char[6];
        var parity = new UpcETables.Parity[6];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (TryMatchUpcEDigit(modules, offset + i * 7, UpcETables.Parity.Odd, out var digit)) {
                digits[i] = digit;
                parity[i] = UpcETables.Parity.Odd;
            } else if (TryMatchUpcEDigit(modules, offset + i * 7, UpcETables.Parity.Even, out digit)) {
                digits[i] = digit;
                parity[i] = UpcETables.Parity.Even;
            } else {
                return false;
            }
        }

        var parityKey = ParityKey(parity);
        foreach (var kvp in UpcETables.ParityPatternTable) {
            var pattern = kvp.Value;
            if (ParityKey(pattern.NumberSystemZero) == parityKey) {
                var candidate = "0" + new string(digits) + kvp.Key;
                if (IsValidUpcE(candidate)) {
                    if (TryDecodeAddOn(source, 51, GuardUpcEEnd, out var addOn)) {
                        text = candidate + "+" + addOn;
                    } else {
                        text = candidate;
                    }
                    return true;
                }
            }
            if (ParityKey(pattern.NumberSystemOne) == parityKey) {
                var candidate = "1" + new string(digits) + kvp.Key;
                if (IsValidUpcE(candidate)) {
                    if (TryDecodeAddOn(source, 51, GuardUpcEEnd, out var addOn)) {
                        text = candidate + "+" + addOn;
                    } else {
                        text = candidate;
                    }
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchPattern(bool[] modules, int offset, bool[] pattern) {
        if (offset < 0 || offset + pattern.Length > modules.Length) return false;
        for (var i = 0; i < pattern.Length; i++) {
            if (modules[offset + i] != pattern[i]) return false;
        }
        return true;
    }

    private enum EanDigitKind {
        LeftOdd,
        LeftEven,
        Right
    }

    private static bool TryMatchEanDigit(bool[] modules, int offset, EanDigitKind kind, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = EanTables.EncodingTable[(char)('0' + d)];
            var pattern = kind switch {
                EanDigitKind.LeftOdd => enc.LeftOdd,
                EanDigitKind.LeftEven => enc.LeftEven,
                _ => enc.Right
            };
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatchUpcDigit(bool[] modules, int offset, bool left, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = UpcATables.EncodingTable[(char)('0' + d)];
            var pattern = left ? enc.Left : enc.Right;
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatchUpcEDigit(bool[] modules, int offset, UpcETables.Parity parity, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = UpcETables.EncodingTable[(char)('0' + d)];
            var pattern = parity == UpcETables.Parity.Odd ? enc.Odd : enc.Even;
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool TryExtractBaseModules(bool[] modules, int expectedLength, bool[] endGuardPattern, out bool[] baseModules) {
        baseModules = modules;
        if (modules.Length <= expectedLength + 2) return false;
        if (!TryFindEndGuard(modules, expectedLength, endGuardPattern, out var endIndex)) return false;
        if (endIndex >= modules.Length) return false;

        var hasBar = false;
        for (var i = endIndex; i < modules.Length; i++) {
            if (modules[i]) { hasBar = true; break; }
        }
        if (!hasBar) return false;

        var trimmed = new bool[endIndex];
        Array.Copy(modules, 0, trimmed, 0, endIndex);
        baseModules = trimmed;
        return true;
    }

    private static bool TryDecodeAddOn(bool[] modules, int expectedLength, bool[] endGuardPattern, out string addOn) {
        addOn = string.Empty;
        if (modules.Length < expectedLength + 8) return false;
        if (!TryFindEndGuard(modules, expectedLength, endGuardPattern, out var endIndex)) return false;
        if (!TryFindAddOnStart(modules, endIndex, out var startIndex)) return false;

        var pos = startIndex + AddOnStartPattern.Length;
        if (TryDecodeAddOnDigits(modules, pos, 5, out var five, out var parity5, out _)
            && EanAddOn.ValidateParity(five.AsSpan(), parity5)) {
            addOn = five;
            return true;
        }
        if (TryDecodeAddOnDigits(modules, pos, 2, out var two, out var parity2, out _)
            && EanAddOn.ValidateParity(two.AsSpan(), parity2)) {
            addOn = two;
            return true;
        }

        return false;
    }

    private static bool TryDecodeAddOnDigits(bool[] modules, int offset, int digitCount, out string value, out bool[] parity, out int endOffset) {
        value = string.Empty;
        parity = Array.Empty<bool>();
        endOffset = offset;
        if (digitCount != 2 && digitCount != 5) return false;
        var required = digitCount * 7 + (digitCount - 1) * 2;
        if (offset < 0 || offset + required > modules.Length) return false;

        var chars = new char[digitCount];
        parity = new bool[digitCount];
        var pos = offset;
        for (var i = 0; i < digitCount; i++) {
            if (TryMatchEanDigit(modules, pos, EanDigitKind.LeftOdd, out var digit)) {
                chars[i] = digit;
                parity[i] = false;
            } else if (TryMatchEanDigit(modules, pos, EanDigitKind.LeftEven, out digit)) {
                chars[i] = digit;
                parity[i] = true;
            } else {
                return false;
            }
            pos += 7;
            if (i + 1 < digitCount) {
                if (!MatchPattern(modules, pos, AddOnSeparatorPattern)) return false;
                pos += AddOnSeparatorPattern.Length;
            }
        }
        endOffset = pos;
        value = new string(chars);
        return true;
    }

    private static bool TryFindEndGuard(bool[] modules, int expectedLength, bool[] endGuardPattern, out int endIndex) {
        endIndex = -1;
        if (modules.Length < endGuardPattern.Length) return false;
        var expectedStart = Math.Max(0, expectedLength - endGuardPattern.Length);
        var min = Math.Max(0, expectedStart - 10);
        var max = Math.Min(modules.Length - endGuardPattern.Length, expectedStart + 10);
        var bestDistance = int.MaxValue;

        for (var i = min; i <= max; i++) {
            if (!MatchPattern(modules, i, endGuardPattern)) continue;
            var distance = Math.Abs(i - expectedStart);
            if (distance < bestDistance) {
                bestDistance = distance;
                endIndex = i + endGuardPattern.Length;
            }
        }
        return endIndex >= 0;
    }

    private static bool TryFindAddOnStart(bool[] modules, int startAt, out int startIndex) {
        startIndex = -1;
        if (startAt < 0) startAt = 0;
        for (var i = startAt; i + AddOnStartPattern.Length <= modules.Length; i++) {
            if (!modules[i]) continue;
            if (MatchPattern(modules, i, AddOnStartPattern)) {
                startIndex = i;
                return true;
            }
        }
        return false;
    }

    private static bool IsValidEanChecksum(string value) {
        if (value.Length != 8 && value.Length != 13) return false;
        var expected = value[value.Length - 1];
        var actual = CalcEanChecksum(value.Substring(0, value.Length - 1));
        return expected == actual;
    }

    private static char CalcEanChecksum(string content) {
        var triple = content.Length == 7;
        var sum = 0;
        for (var i = 0; i < content.Length; i++) {
            var val = content[i] - '0';
            if (triple) val *= 3;
            triple = !triple;
            sum += val;
        }
        return (char)((10 - sum % 10) % 10 + '0');
    }

    private static bool IsValidUpcAChecksum(string value) {
        if (value.Length != 12) return false;
        var expected = value[value.Length - 1];
        var actual = CalcUpcAChecksum(value.Substring(0, 11));
        return expected == actual;
    }

    private static bool IsValidUpcE(string value) {
        if (value.Length != 8) return false;
        var upcA = ExpandUpcEToUpcA(value);
        return IsValidUpcAChecksum(upcA);
    }

    private static string ExpandUpcEToUpcA(string value) {
        var numberSystem = value[0];
        var digits = value.Substring(1, 6);
        var check = value[7];

        string upcA;
        switch (digits[5]) {
            case '0':
            case '1':
            case '2':
                upcA = $"{numberSystem}{digits.Substring(0, 2)}{digits[5]}0000{digits.Substring(2, 3)}";
                break;
            case '3':
                upcA = $"{numberSystem}{digits.Substring(0, 3)}00000{digits.Substring(3, 2)}";
                break;
            case '4':
                upcA = $"{numberSystem}{digits.Substring(0, 4)}00000{digits[4]}";
                break;
            default:
                upcA = $"{numberSystem}{digits.Substring(0, 5)}0000{digits[5]}";
                break;
        }

        return upcA + check;
    }

    private static char CalcUpcAChecksum(string content) {
        var digits = content.Select(c => c - '0').ToArray();
        var sum = 3 * (digits[0] + digits[2] + digits[4] + digits[6] + digits[8] + digits[10]);
        sum += digits[1] + digits[3] + digits[5] + digits[7] + digits[9];
        sum %= 10;
        sum = sum != 0 ? 10 - sum : 0;
        return (char)(sum + '0');
    }

    private static bool TryNormalizeModules(bool[] modules, int expectedLength, out bool[] normalized) {
        normalized = modules;
        if (modules.Length == expectedLength) return true;
        if (modules.Length == 0) return false;

        var len = modules.Length;
        var ratio = len / (double)expectedLength;
        if (ratio < 0.5 || ratio > 3.0) return false;

        var output = new bool[expectedLength];
        for (var i = 0; i < expectedLength; i++) {
            var start = (int)Math.Floor(i * len / (double)expectedLength);
            var end = (int)Math.Floor((i + 1) * len / (double)expectedLength);
            if (end <= start) {
                output[i] = modules[Math.Min(start, len - 1)];
                continue;
            }
            var count = 0;
            for (var j = start; j < end && j < len; j++) {
                if (modules[j]) count++;
            }
            var total = end - start;
            output[i] = count * 2 >= total;
        }
        normalized = output;
        return true;
    }

    private static string DecodeCode39Extended(string raw) {
        if (raw.IndexOfAny(new[] { '$', '%', '/', '+' }) < 0) return raw;
        var reverse = Code39ExtendedMap.Value;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < raw.Length; i++) {
            if (i + 1 < raw.Length) {
                var key = raw.Substring(i, 2);
                if (reverse.TryGetValue(key, out var mapped)) {
                    sb.Append(mapped);
                    i++;
                    continue;
                }
            }
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }

    private static string DecodeCode93Extended(string raw) {
        if (!raw.Any(c => c == Code93Tables.Fnc1 || c == Code93Tables.Fnc2 || c == Code93Tables.Fnc3 || c == Code93Tables.Fnc4)) {
            return raw;
        }
        var reverse = Code93ExtendedMap.Value;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < raw.Length; i++) {
            var ch = raw[i];
            if ((ch == Code93Tables.Fnc1 || ch == Code93Tables.Fnc2 || ch == Code93Tables.Fnc3 || ch == Code93Tables.Fnc4) && i + 1 < raw.Length) {
                var key = raw.Substring(i, 2);
                if (reverse.TryGetValue(key, out var mapped)) {
                    sb.Append(mapped);
                    i++;
                    continue;
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static uint PatternBits(bool[] modules, int offset, int length) {
        uint key = 0;
        for (var i = 0; i < length; i++) {
            key = (key << 1) | (modules[offset + i] ? 1u : 0u);
        }
        return key;
    }

    private static int PatternBitsFromString(string pattern) {
        var key = 0;
        for (var i = 0; i < pattern.Length; i++) {
            key = (key << 1) | (pattern[i] == '1' ? 1 : 0);
        }
        return key;
    }

    private static int PatternKey(int[] runs, int offset, int count) {
        var key = 0;
        for (var i = 0; i < count; i++) {
            key = key * 10 + runs[offset + i];
        }
        return key;
    }

    private static string ParityKey(bool[] parity) {
        var key = 0;
        for (var i = 0; i < parity.Length; i++) {
            key = (key << 1) | (parity[i] ? 1 : 0);
        }
        return Convert.ToString(key, 2).PadLeft(parity.Length, '0');
    }

    private static string ParityKey(UpcETables.Parity[] parity) {
        var key = 0;
        for (var i = 0; i < parity.Length; i++) {
            key = (key << 1) | (parity[i] == UpcETables.Parity.Even ? 1 : 0);
        }
        return Convert.ToString(key, 2).PadLeft(parity.Length, '0');
    }

    private static char GetCode39ChecksumChar(ReadOnlySpan<char> content) {
        var sum = 0;
        foreach (var ch in content) {
            if (!Code39Tables.EncodingTable.TryGetValue(ch, out var entry) || entry.value < 0) return '#';
            sum += entry.value;
        }
        sum %= 43;
        foreach (var kvp in Code39Tables.EncodingTable) {
            if (kvp.Value.value == sum) return kvp.Key;
        }
        return '#';
    }

    private static char GetCode93Checksum(string content, int maxWeight) {
        var weight = 1;
        var sum = 0;
        for (var i = content.Length - 1; i >= 0; i--) {
            var ch = content[i];
            if (!Code93Tables.EncodingTable.TryGetValue(ch, out var entry)) return ' ';
            sum += entry.value * weight;
            if (++weight > maxWeight) weight = 1;
        }
        sum %= 47;
        foreach (var kvp in Code93Tables.EncodingTable) {
            if (kvp.Value.value == sum) return kvp.Key;
        }
        return ' ';
    }

    private static bool TryStripMsiChecksum(string raw, out string stripped) {
        stripped = raw;
        if (raw.Length >= 2) {
            var data = raw.Substring(0, raw.Length - 2);
            var check1 = raw[raw.Length - 2];
            var check2 = raw[raw.Length - 1];
            if (CalcMsiMod10(data) == check1 && CalcMsiMod10(data + check1) == check2) {
                stripped = data;
                return true;
            }
        }
        if (raw.Length >= 1) {
            var data = raw.Substring(0, raw.Length - 1);
            var check = raw[raw.Length - 1];
            if (CalcMsiMod10(data) == check) {
                stripped = data;
                return true;
            }
        }
        return false;
    }

    private static char CalcMsiMod10(string content) {
        var sum = 0;
        var doubleIt = true;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            if (doubleIt) {
                digit *= 2;
                if (digit > 9) digit = (digit / 10) + (digit % 10);
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        var mod = sum % 10;
        var check = mod == 0 ? 0 : 10 - mod;
        return (char)('0' + check);
    }

    private static bool TryStripCode11Checksum(string raw, out string stripped) {
        stripped = raw;
        if (raw.Length >= 2) {
            var data = raw.Substring(0, raw.Length - 2);
            var c = raw[raw.Length - 2];
            var k = raw[raw.Length - 1];
            if (CalcCode11Checksum(data, 10) == c && CalcCode11Checksum(data + c, 9) == k) {
                stripped = data;
                return true;
            }
        }
        if (raw.Length >= 1) {
            var data = raw.Substring(0, raw.Length - 1);
            var c = raw[raw.Length - 1];
            if (CalcCode11Checksum(data, 10) == c) {
                stripped = data;
                return true;
            }
        }
        return false;
    }

    private static char CalcCode11Checksum(string content, int maxWeight) {
        var sum = 0;
        var weight = 1;
        for (var i = content.Length - 1; i >= 0; i--) {
            var ch = content[i];
            if (!Code11Tables.ValueTable.TryGetValue(ch, out var value)) return '-';
            sum += value * weight;
            weight++;
            if (weight > maxWeight) weight = 1;
        }
        var check = sum % 11;
        return check == 10 ? '-' : (char)('0' + check);
    }

    private static bool TryDecodePlesseyPair(int barRun, int spaceRun, out bool bit) {
        bit = false;
        if (barRun == spaceRun) return false;
        bit = barRun > spaceRun;
        return true;
    }

    private static byte CalcPlesseyCrc(bool[] bits) {
        const int poly = 0x1E9;
        var crc = 0;
        for (var i = 0; i < bits.Length; i++) {
            crc = (crc << 1) | (bits[i] ? 1 : 0);
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        for (var i = 0; i < 8; i++) {
            crc <<= 1;
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        return (byte)(crc & 0xFF);
    }

    private static byte BitsToByte(bool[] bits) {
        var value = 0;
        for (var i = 0; i < bits.Length; i++) {
            if (bits[i]) value |= 1 << i;
        }
        return (byte)value;
    }

    private static readonly bool[] GuardStart = { true, false, true };
    private static readonly bool[] GuardCenter = { false, true, false, true, false };
    private static readonly bool[] GuardUpcEEnd = { false, true, false, true, false, true };
    private static readonly bool[] AddOnStartPattern = { true, false, true, true };
    private static readonly bool[] AddOnSeparatorPattern = { false, true };
    private static readonly bool[] MsiStartPattern = { true, true, false };
    private static readonly bool[] MsiStopPattern = { true, false, false, true };
    private static readonly int[] Matrix2of5StartBars = { 3, 1, 1, 1, 1 };
    private static readonly int[] Matrix2of5StopBars = { 3, 1, 1, 1, 1 };
    private static readonly int[] Iata2of5StartBars = { 1, 1, 1 };
    private static readonly int[] Iata2of5StopBars = { 3, 1, 1 };

    private static readonly Lazy<Dictionary<uint, char>> Code39PatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in Code39Tables.EncodingTable) {
            uint key = 0;
            var data = kvp.Value.data;
            for (var i = 0; i < data.Length; i++) {
                key = (key << 1) | (data[i] ? 1u : 0u);
            }
            dict[key] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, char>> CodabarPatternMap = new(() => {
        var dict = new Dictionary<int, char>();
        foreach (var kvp in CodabarTables.EncodingTable) {
            dict[PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, char>> Code11PatternMap = new(() => {
        var dict = new Dictionary<int, char> {
            { PatternBitsFromString(Code11Tables.StartStopPattern), '*' }
        };
        foreach (var kvp in Code11Tables.EncodingTable) {
            dict[PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<uint, char>> MsiPatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in MsiTables.DigitPatterns) {
            dict[(uint)PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> Code39ExtendedMap = new(() => {
        var dict = new Dictionary<string, char>();
        foreach (var kvp in Code39Tables.ExtendedTable) {
            dict[kvp.Value] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<uint, char>> Code93PatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in Code93Tables.EncodingTable) {
            dict[kvp.Value.data] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> Code93ExtendedMap = new(() => {
        var dict = new Dictionary<string, char>();
        for (var i = 0; i < Code93Tables.ExtendedTable.Length; i++) {
            var key = Code93Tables.ExtendedTable[i];
            if (!dict.ContainsKey(key)) dict[key] = (char)i;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> EanParityMap = new(() => {
        var dict = new Dictionary<string, char>();
        foreach (var kvp in EanTables.EncodingTable) {
            var key = ParityKey(kvp.Value.Checksum);
            dict[key] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, int>> Code128PatternMap = new(() => {
        var dict = new Dictionary<int, int>();
        for (var code = 0; code <= 105; code++) {
            var pattern = Code128Tables.GetPattern(code);
            var key = PatternKeyFromCode(pattern, 6);
            dict[key] = code;
        }
        return dict;
    });

    private static readonly Lazy<int> Code128StopKey = new(() => {
        var pattern = Code128Tables.GetPattern(Code128Tables.Stop);
        return PatternKeyFromCode(pattern, 7);
    });

    private static readonly Lazy<Dictionary<int, int>> Itf14PatternMap = new(() => {
        var dict = new Dictionary<int, int>();
        for (var digit = 0; digit <= 9; digit++) {
            var pattern = Itf14Tables.DigitPatterns[digit];
            var key = 0;
            for (var i = 0; i < 5; i++) {
                if (pattern[i] == 3) key |= 1 << (4 - i);
            }
            dict[key] = digit;
        }
        return dict;
    });

    private static int PatternKeyFromCode(uint pattern, int nibbles) {
        var key = 0;
        for (var i = nibbles - 1; i >= 0; i--) {
            var width = (int)((pattern >> (i * 4)) & 0xFu);
            key = key * 10 + width;
        }
        return key;
    }

}
