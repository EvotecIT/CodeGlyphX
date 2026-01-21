using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code11;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.Code32;
using CodeGlyphX.Code93;
using CodeGlyphX.Codabar;
using CodeGlyphX.DataBar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Internal;
using CodeGlyphX.Msi;
using CodeGlyphX.PatchCode;
using CodeGlyphX.Plessey;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.Telepen;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodeCode39(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        var patternToChar = Code39PatternMap.Value;
        var maxSymbols = (modules.Length + 1) / 13;
        if (maxSymbols <= 0) return false;

        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;

        try {
            var index = 0;
            while (index + 12 <= modules.Length) {
                var key = PatternBits(modules, index, 12);
                if (!patternToChar.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                index += 12;
                if (index < modules.Length && !modules[index]) index++; // inter-character space
            }

            if (count < 2) return false;
            if (rented[0] != '*' || rented[count - 1] != '*') return false;

            var rawLen = count - 2;
            var raw = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            var policy = options?.Code39Checksum ?? Code39ChecksumPolicy.None;
            if (policy != Code39ChecksumPolicy.None && raw.Length >= 2) {
                // Minimum length is one data symbol plus optional checksum.
                var expected = GetCode39ChecksumChar(raw.AsSpan(0, raw.Length - 1));
                if (expected != '#' && raw[raw.Length - 1] == expected) {
                    raw = raw.Substring(0, raw.Length - 1);
                } else if (policy == Code39ChecksumPolicy.RequireValid) {
                    return false;
                }
            }
            text = DecodeCode39Extended(raw);
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeCode93(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 10) return false;
        if (!modules[modules.Length - 1]) return false;

        var dataLen = modules.Length - 1;
        if (dataLen % 9 != 0) return false;
        var charCount = dataLen / 9;
        if (charCount < 2) return false;

        var chars = new char[charCount];
        for (var i = 0; i < charCount; i++) {
            var key = PatternBits(modules, i * 9, 9);
            if (!Code93PatternMap.Value.TryGetValue(key, out var ch)) return false;
            chars[i] = ch;
        }

        if (chars[0] != '*' || chars[chars.Length - 1] != '*') return false;
        var raw = new string(chars, 1, chars.Length - 2);
        if (raw.Length >= 2) {
            var c = GetCode93Checksum(raw.Substring(0, raw.Length - 2), 20);
            var k = GetCode93Checksum(raw.Substring(0, raw.Length - 1), 15);
            if (raw[raw.Length - 2] == c && raw[raw.Length - 1] == k) {
                raw = raw.Substring(0, raw.Length - 2);
            }
        }

        text = DecodeCode93Extended(raw);
        return true;
    }

    private static bool TryDecodeCodabar(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 7) return false;

        var maxSymbols = (runs.Length + 1) / 8;
        if (maxSymbols <= 0) return false;
        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;
        var pos = 0;
        try {
            while (pos + 7 <= runs.Length) {
                var min = int.MaxValue;
                var max = 0;
                for (var i = 0; i < 7; i++) {
                    var len = runs[pos + i];
                    if (len < min) min = len;
                    if (len > max) max = len;
                }
                if (min <= 0) return false;
                var threshold = (min + max) / 2.0;

                var key = 0;
                for (var i = 0; i < 7; i++) {
                    key = (key << 1) | (runs[pos + i] > threshold ? 1 : 0);
                }
                if (!CodabarPatternMap.Value.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                pos += 7;

                if (pos < runs.Length) {
                    if ((pos & 1) == 0) return false;
                    pos++;
                }
            }

            if (pos != runs.Length) return false;
            if (count < 2) return false;
            if (!CodabarTables.StartStopChars.Contains(rented[0]) || !CodabarTables.StartStopChars.Contains(rented[count - 1])) return false;

            var rawLen = count - 2;
            text = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeMsi(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < MsiStartPattern.Length + MsiStopPattern.Length + 12) return false;
        if (!MatchPattern(modules, 0, MsiStartPattern)) return false;
        if (!MatchPattern(modules, modules.Length - MsiStopPattern.Length, MsiStopPattern)) return false;

        var dataLen = modules.Length - MsiStartPattern.Length - MsiStopPattern.Length;
        if (dataLen <= 0 || dataLen % 12 != 0) return false;
        var count = dataLen / 12;

        var digits = new char[count];
        var offset = MsiStartPattern.Length;
        for (var i = 0; i < count; i++) {
            var key = PatternBits(modules, offset, 12);
            if (!MsiPatternMap.Value.TryGetValue(key, out var digit)) return false;
            digits[i] = digit;
            offset += 12;
        }

        var raw = new string(digits);
        var policy = options?.MsiChecksum ?? MsiChecksumPolicy.None;
        if (policy != MsiChecksumPolicy.None) {
            if (TryStripMsiChecksum(raw, out var stripped)) {
                text = stripped;
                return true;
            }
            if (policy == MsiChecksumPolicy.RequireValid) return false;
        }

        text = raw;
        return true;
    }

    private static bool TryDecodeCode11(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 5) return false;

        var maxSymbols = (runs.Length + 1) / 6;
        if (maxSymbols <= 0) return false;
        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;
        var pos = 0;
        try {
            while (pos + 5 <= runs.Length) {
                var min = int.MaxValue;
                var max = 0;
                for (var i = 0; i < 5; i++) {
                    var len = runs[pos + i];
                    if (len < min) min = len;
                    if (len > max) max = len;
                }
                if (min <= 0) return false;
                var threshold = (min + max) / 2.0;

                var key = 0;
                for (var i = 0; i < 5; i++) {
                    key = (key << 1) | (runs[pos + i] > threshold ? 1 : 0);
                }
                if (!Code11PatternMap.Value.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                pos += 5;

                if (pos < runs.Length) {
                    if ((pos & 1) == 0) return false;
                    pos++;
                }
            }

            if (pos != runs.Length) return false;
            if (count < 2) return false;
            if (rented[0] != '*' || rented[count - 1] != '*') return false;

            var rawLen = count - 2;
            var raw = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            var policy = options?.Code11Checksum ?? Code11ChecksumPolicy.None;
            if (policy != Code11ChecksumPolicy.None) {
                if (TryStripCode11Checksum(raw, out var stripped)) {
                    text = stripped;
                    return true;
                }
                if (policy == Code11ChecksumPolicy.RequireValid) return false;
            }

            text = raw;
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodePlessey(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        if (TryDecodePlesseyInternal(modules, options, out text)) return true;
        var reversed = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) reversed[i] = modules[modules.Length - 1 - i];
        return TryDecodePlesseyInternal(reversed, options, out text);
    }

    private static bool TryDecodePlesseyInternal(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < 24) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 13) return false;
        if ((runs.Length & 1) == 0) return false;

        var startRuns = PlesseyTables.StartBits.Length * 2;
        if (runs.Length < startRuns + 9) return false;

        var pos = 0;
        for (var i = 0; i < PlesseyTables.StartBits.Length; i++) {
            if (pos + 1 >= runs.Length) return false;
            if (!TryDecodePlesseyPair(runs[pos], runs[pos + 1], out var bit)) return false;
            if (bit != (PlesseyTables.StartBits[i] == '1')) return false;
            pos += 2;
        }

        var stopStart = runs.Length - PlesseyTables.StopBits.Length * 2;
        if ((stopStart & 1) == 0) return false;
        var terminationIndex = stopStart - 1;
        if (terminationIndex < pos) return false;

        for (var i = 0; i < PlesseyTables.StopBits.Length; i++) {
            var spaceIndex = stopStart + i * 2;
            if (spaceIndex + 1 >= runs.Length) return false;
            if (!TryDecodePlesseyPair(runs[spaceIndex + 1], runs[spaceIndex], out var bit)) return false;
            if (bit != (PlesseyTables.StopBits[i] == '1')) return false;
        }

        var bitCount = terminationIndex - pos;
        if ((bitCount & 1) != 0) return false;
        var dataBits = bitCount / 2;
        if (dataBits <= 8) return false;
        if ((dataBits - 8) % 4 != 0) return false;

        var bits = new bool[dataBits];
        var bitPos = 0;
        for (var i = pos; i < terminationIndex; i += 2) {
            if (!TryDecodePlesseyPair(runs[i], runs[i + 1], out var bit)) return false;
            bits[bitPos++] = bit;
        }

        var payloadBits = dataBits - 8;
        var crcBits = new bool[8];
        Array.Copy(bits, payloadBits, crcBits, 0, 8);

        var payload = new bool[payloadBits];
        Array.Copy(bits, 0, payload, 0, payloadBits);

        var policy = options?.PlesseyChecksum ?? PlesseyChecksumPolicy.RequireValid;
        if (policy != PlesseyChecksumPolicy.None) {
            var expected = CalcPlesseyCrc(payload);
            var actual = BitsToByte(crcBits);
            if (expected != actual && policy == PlesseyChecksumPolicy.RequireValid) return false;
        }

        var chars = new char[payloadBits / 4];
        for (var i = 0; i < chars.Length; i++) {
            var value = 0;
            for (var b = 0; b < 4; b++) {
                if (payload[i * 4 + b]) value |= 1 << b;
            }
            chars[i] = value < 10 ? (char)('0' + value) : (char)('A' + (value - 10));
        }

        text = new string(chars);
        return true;
    }

    private static bool TryDecodePatchCode(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length != 7) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || (long)maxRun < (long)minRun * 2) return false;
        var threshold = ((long)minRun + maxRun) / 2.0;

        for (var i = 1; i < runs.Length; i += 2) {
            if (runs[i] > threshold) return false;
        }

        var key = 0;
        for (var i = 0; i < 4; i++) {
            if (runs[i * 2] > threshold) key |= 1 << (3 - i);
        }

        if (!PatchCodeTables.PatternMap.TryGetValue(key, out var symbol)) return false;

        text = symbol.ToString();
        return true;
    }

    private static bool TryDecodeTelepen(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 8) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if ((runs.Length & 1) != 0) {
            // Telepen ends with a space; TrimModules removes trailing spaces, so pad with a narrow space run.
            var min = int.MaxValue;
            var max = 0;
            for (var i = 0; i < runs.Length; i++) {
                if (runs[i] < min) min = runs[i];
                if (runs[i] > max) max = runs[i];
            }
            if (min <= 0) return false;

            var padded = new int[runs.Length + 1];
            Array.Copy(runs, padded, runs.Length);
            padded[padded.Length - 1] = min;
            runs = padded;
        }

        if ((runs.Length & 1) != 0) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || (long)maxRun < (long)minRun * 2) return false;
        var threshold = ((long)minRun + maxRun) / 2.0;

        var pairs = runs.Length / 2;
        var pairKinds = new TelepenPairKind[pairs];
        for (var i = 0; i < pairs; i++) {
            var barWide = runs[i * 2] > threshold;
            var spaceWide = runs[i * 2 + 1] > threshold;
            pairKinds[i] = barWide
                ? (spaceWide ? TelepenPairKind.WideWide : TelepenPairKind.WideNarrow)
                : (spaceWide ? TelepenPairKind.NarrowWide : TelepenPairKind.NarrowNarrow);
        }

        var bits = new List<bool>(pairs * 2);
        for (var i = 0; i < pairs; i++) {
            switch (pairKinds[i]) {
                case TelepenPairKind.NarrowNarrow:
                    bits.Add(true);
                    break;
                case TelepenPairKind.WideNarrow:
                    bits.Add(false);
                    bits.Add(false);
                    break;
                case TelepenPairKind.WideWide:
                    bits.Add(false);
                    bits.Add(true);
                    bits.Add(false);
                    break;
                case TelepenPairKind.NarrowWide: {
                    var j = i + 1;
                    var middle = 0;
                    while (j < pairs && pairKinds[j] == TelepenPairKind.NarrowNarrow) {
                        middle++;
                        j++;
                    }
                    if (j >= pairs || pairKinds[j] != TelepenPairKind.NarrowWide) return false;
                    bits.Add(false);
                    for (var k = 0; k < middle + 2; k++) bits.Add(true);
                    bits.Add(false);
                    i = j;
                    break;
                }
                default:
                    return false;
            }
        }

        if ((bits.Count & 7) != 0) return false;
        var byteCount = bits.Count / 8;
        if (byteCount < 3) return false;

        var values = new byte[byteCount];
        var bitIndex = 0;
        for (var i = 0; i < byteCount; i++) {
            var value = 0;
            for (var b = 0; b < 8; b++) {
                if (bits[bitIndex++]) value |= 1 << b;
            }
            var full = (byte)value;
            if (!TelepenTables.HasEvenParity(full)) return false;
            values[i] = (byte)(full & 0x7F);
        }

        if (values[0] != TelepenTables.StartValue) return false;
        if (values[values.Length - 1] != TelepenTables.StopValue) return false;

        var checkIndex = values.Length - 2;
        if (checkIndex <= 0) return false;

        var checksum = TelepenTables.CalcChecksum(values, 1, checkIndex - 1);
        if (values[checkIndex] != checksum) return false;

        var dataLen = checkIndex - 1;
        var chars = new char[dataLen];
        for (var i = 0; i < dataLen; i++) {
            chars[i] = (char)values[i + 1];
        }

        text = new string(chars);
        return true;
    }

    private static bool TryDecodePharmacode(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 3) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if ((runs.Length & 1) == 0) return false;

        var barCount = (runs.Length + 1) / 2;
        if (barCount < PharmacodeEncoder.MinBars || barCount > PharmacodeEncoder.MaxBars) return false;

        var minBar = int.MaxValue;
        var maxBar = 0;
        for (var i = 0; i < runs.Length; i += 2) {
            if (runs[i] < minBar) minBar = runs[i];
            if (runs[i] > maxBar) maxBar = runs[i];
        }

        if (minBar <= 0 || (long)maxBar < (long)minBar * 2) return false;
        var threshold = ((long)minBar + maxBar) / 2.0;

        for (var i = 1; i < runs.Length; i += 2) {
            if (runs[i] > threshold) return false;
        }

        var value = 0;
        for (var i = 0; i < barCount; i++) {
            var wide = runs[i * 2] > threshold;
            var pos = barCount - 1 - i;
            var weight = 1 << pos;
            value += (wide ? 2 : 1) * weight;
        }

        if (value < PharmacodeEncoder.MinValue || value > PharmacodeEncoder.MaxValue) return false;

        text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    private enum TelepenPairKind {
        NarrowNarrow,
        WideNarrow,
        WideWide,
        NarrowWide
    }

    private static bool TryDecodeCode128(bool[] modules, out string text, out bool isGs1) {
        text = string.Empty;
        isGs1 = false;
        if (modules.Length < 24) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 6) return false;
        if (runs[0] == 0) return false;

        var codes = new List<int>();
        var pos = 0;
        while (pos < runs.Length) {
            if (runs.Length - pos >= 7) {
                var stopKey = PatternKey(runs, pos, 7);
                if (stopKey == Code128StopKey.Value) {
                    pos += 7;
                    break;
                }
            }
            if (runs.Length - pos < 6) return false;
            var key = PatternKey(runs, pos, 6);
            if (!Code128PatternMap.Value.TryGetValue(key, out var code)) return false;
            codes.Add(code);
            pos += 6;
        }

        if (codes.Count < 3) return false;

        var checksum = codes[codes.Count - 1];
        var sum = codes[0];
        for (var i = 1; i < codes.Count - 1; i++) sum = (sum + codes[i] * i) % 103;
        if (sum != checksum) return false;

        var start = codes[0];
        var set = start == Code128Tables.StartC ? 'C' : start == Code128Tables.StartB ? 'B' : start == Code128Tables.StartA ? 'A' : '?';
        if (set == '?') return false;

        var sb = new System.Text.StringBuilder();
        var gs1StartConsumed = false;
        for (var i = 1; i < codes.Count - 1; i++) {
            var code = codes[i];
            if (code == Code128Tables.Fnc1) {
                isGs1 = true;
                if (!gs1StartConsumed) {
                    gs1StartConsumed = true;
                    continue;
                }
                sb.Append(Gs1.GroupSeparator);
                continue;
            }
            if (set == 'A') {
                if (code == Code128Tables.CodeC) {
                    set = 'C';
                    continue;
                }
                if (code == Code128Tables.CodeB) {
                    set = 'B';
                    continue;
                }
                if (code == Code128Tables.CodeA) continue;
                if (code >= 0 && code <= 95) {
                    sb.Append((char)code);
                    continue;
                }
                return false;
            }

            if (set == 'B') {
                if (code == Code128Tables.CodeC) {
                    set = 'C';
                    continue;
                }
                if (code == Code128Tables.CodeA) {
                    set = 'A';
                    continue;
                }
                if (code == Code128Tables.CodeB) continue;
                if (code >= 0 && code <= 95) {
                    sb.Append((char)(code + 32));
                    continue;
                }
                return false;
            }

            if (set == 'C') {
                if (code == Code128Tables.CodeB) {
                    set = 'B';
                    continue;
                }
                if (code == Code128Tables.CodeA) {
                    set = 'A';
                    continue;
                }
                if (code == Code128Tables.CodeC) continue;
                if (code >= 0 && code <= 99) {
                    sb.Append(code.ToString("00"));
                    continue;
                }
                return false;
            }
        }

        text = sb.ToString();
        return true;
    }

    private static bool TryDecodeItf14(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 7) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || (long)maxRun < (long)minRun * 2) return false;
        var threshold = ((long)minRun + maxRun) / 2.0;

        // Start pattern: narrow bar/space/bar/space.
        if (runs.Length < 4) return false;
        for (var i = 0; i < 4; i++) {
            if (runs[i] > threshold) return false;
        }

        var pos = 4;
        var remaining = runs.Length - pos;
        if (remaining < 3) return false;
        if ((remaining - 3) % 10 != 0) return false;

        var pairs = (remaining - 3) / 10;
        if (pairs * 2 != 14) return false;

        var digits = new char[pairs * 2];
        for (var pair = 0; pair < pairs; pair++) {
            var barKey = PatternKey(runs, pos, threshold);
            var spaceKey = PatternKey(runs, pos + 1, threshold);
            if (!Itf14PatternMap.Value.TryGetValue(barKey, out var leftDigit)) return false;
            if (!Itf14PatternMap.Value.TryGetValue(spaceKey, out var rightDigit)) return false;
            digits[pair * 2] = (char)('0' + leftDigit);
            digits[pair * 2 + 1] = (char)('0' + rightDigit);
            pos += 10;
        }

        // Stop pattern: wide bar, narrow space, narrow bar.
        if (pos + 2 >= runs.Length) return false;
        if (runs[pos] <= threshold) return false;
        if (runs[pos + 1] > threshold) return false;
        if (runs[pos + 2] > threshold) return false;

        var raw = new string(digits);
        var expected = CalcItf14Checksum(raw.AsSpan(0, 13));
        if (raw[13] != expected) return false;

        text = raw;
        return true;
    }

    private static bool TryDecodeCode32(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        BarcodeDecodeOptions? code39Options = options;
        if (code39Options?.Code39Checksum != Code39ChecksumPolicy.None) {
            code39Options = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.None };
        }
        if (!TryDecodeCode39(modules, code39Options, out var raw)) return false;
        if (raw.Length != 6) return false;
        if (!Code32Tables.TryFromBase32(raw.AsSpan(), out var value)) return false;

        var digits = value.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
        var check = Code32Tables.CalcChecksum(digits.AsSpan());
        if (check < 0) return false;
        if (digits[8] != (char)('0' + check)) return false;

        text = digits;
        return true;
    }

    private static bool TryDecodeItf(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 7) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || (long)maxRun < (long)minRun * 2) return false;
        var threshold = ((long)minRun + maxRun) / 2.0;

        // Start pattern: narrow bar/space/bar/space.
        if (runs.Length < 4) return false;
        for (var i = 0; i < 4; i++) {
            if (runs[i] > threshold) return false;
        }

        var pos = 4;
        var remaining = runs.Length - pos;
        if (remaining < 3) return false;
        if ((remaining - 3) % 10 != 0) return false;

        var pairs = (remaining - 3) / 10;
        if (pairs <= 0) return false;

        var digits = new char[pairs * 2];
        for (var pair = 0; pair < pairs; pair++) {
            var barKey = PatternKey(runs, pos, threshold);
            var spaceKey = PatternKey(runs, pos + 1, threshold);
            if (!Itf14PatternMap.Value.TryGetValue(barKey, out var leftDigit)) return false;
            if (!Itf14PatternMap.Value.TryGetValue(spaceKey, out var rightDigit)) return false;
            digits[pair * 2] = (char)('0' + leftDigit);
            digits[pair * 2 + 1] = (char)('0' + rightDigit);
            pos += 10;
        }

        // Stop pattern: wide bar, narrow space, narrow bar.
        if (pos + 2 >= runs.Length) return false;
        if (runs[pos] <= threshold) return false;
        if (runs[pos + 1] > threshold) return false;
        if (runs[pos + 2] > threshold) return false;

        text = new string(digits);
        return true;
    }

    private static bool TryDecodeMatrix2of5(bool[] modules, out string text) {
        return TryDecodeDiscrete2of5(modules, Matrix2of5StartBars, Matrix2of5StopBars, out text);
    }

    private static bool TryDecodeIndustrial2of5(bool[] modules, out string text) {
        return TryDecodeDiscrete2of5(modules, Industrial2of5StartBars, Industrial2of5StopBars, out text);
    }

    private static bool TryDecodeIata2of5(bool[] modules, out string text) {
        return TryDecodeDiscrete2of5(modules, Iata2of5StartBars, Iata2of5StopBars, out text);
    }

    private static bool TryDecodeDiscrete2of5(bool[] modules, int[] startBars, int[] stopBars, out string text) {
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < startBars.Length * 2 + stopBars.Length * 2) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || (long)maxRun < (long)minRun * 2) return false;
        var threshold = ((long)minRun + maxRun) / 2.0;

        for (var i = 1; i < runs.Length; i += 2) {
            if (runs[i] > threshold) return false;
        }

        var barCount = (runs.Length + 1) / 2;
        if (barCount < startBars.Length + stopBars.Length + 5) return false;

        var barWide = new bool[barCount];
        for (var i = 0; i < barCount; i++) {
            barWide[i] = runs[i * 2] > threshold;
        }

        for (var i = 0; i < startBars.Length; i++) {
            if (barWide[i] != (startBars[i] > 1)) return false;
        }

        for (var i = 0; i < stopBars.Length; i++) {
            if (barWide[barCount - stopBars.Length + i] != (stopBars[i] > 1)) return false;
        }

        var startIndex = startBars.Length;
        var endIndex = barCount - stopBars.Length;
        var barDigits = endIndex - startIndex;
        if (barDigits <= 0 || (barDigits % 5) != 0) return false;

        var digitCount = barDigits / 5;
        var digits = new char[digitCount];
        for (var d = 0; d < digitCount; d++) {
            var key = 0;
            for (var i = 0; i < 5; i++) {
                if (barWide[startIndex + d * 5 + i]) key |= 1 << (4 - i);
            }
            if (!Itf14PatternMap.Value.TryGetValue(key, out var digit)) return false;
            digits[d] = (char)('0' + digit);
        }

        text = new string(digits);
        return true;
    }

    private static int PatternKey(int[] runs, int start, double threshold) {
        var key = 0;
        var idx = start;
        for (var i = 0; i < 5; i++) {
            if (idx >= runs.Length) return -1;
            if (runs[idx] > threshold) key |= 1 << (4 - i);
            idx += 2;
        }
        return key;
    }

    private static char CalcItf14Checksum(ReadOnlySpan<char> content) {
        var sum = 0;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            var weight = ((content.Length - 1 - i) & 1) == 0 ? 3 : 1;
            sum += digit * weight;
        }
        var check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    private static bool TryDecodeEan8(bool[] modules, out string text) {
        text = string.Empty;
        var source = modules;
        if (TryExtractBaseModules(source, 67, GuardStart, out var baseModules)) {
            modules = baseModules;
        }
        if (!TryNormalizeModules(modules, 67, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 64, GuardStart)) return false;
        if (!MatchPattern(modules, 31, GuardCenter)) return false;

        var digits = new char[8];
        var offset = 3;
        for (var i = 0; i < 4; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftOdd, out var digit)) return false;
            digits[i] = digit;
        }
        offset = 3 + 4 * 7 + 5;
        for (var i = 0; i < 4; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.Right, out var digit)) return false;
            digits[i + 4] = digit;
        }

        var raw = new string(digits);
        if (!IsValidEanChecksum(raw)) return false;
        if (TryDecodeAddOn(source, 67, GuardStart, out var addOn)) {
            text = raw + "+" + addOn;
        } else {
            text = raw;
        }
        return true;
    }

    private static bool TryDecodeGs1DataBarTruncated(bool[] modules, out string text) {
        return DataBar14Decoder.TryDecodeTruncated(modules, out text);
    }

    private static bool TryDecodeGs1DataBarExpanded(bool[] modules, out string text) {
        return DataBarExpandedDecoder.TryDecodeExpanded(modules, out text);
    }

}
