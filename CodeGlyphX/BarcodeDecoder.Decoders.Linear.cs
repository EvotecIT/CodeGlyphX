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
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodeCode39(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        if (TryDecodeNormalizedCode39(modules, options, out text)) return true;
        // Exact external module grids can use a different legal wide:narrow
        // ratio than our encoder. Reuse the physical scanline width rules.
        var workspace = new ModuleRunWorkspace(modules);
        try {
            var runs = workspace.Runs.ToArray().ToList();
            if (runs.Count < 19 || (runs.Count + 1) % 10 != 0 || !modules[0]) return false;
            var normalized = new List<bool>();
            for (var start = 0; start < runs.Count; start += 10) {
                if (!BarcodeScanline.NormalizeCode39(runs, start, false, out var pattern, out var pitch)) return false;
                if (start > 0) {
                    if (runs[start - 1] > pitch * 3 || runs[start - 1] < pitch * 0.4) return false;
                    normalized.Add(false);
                }
                for (var i = 0; i < pattern.Length; i++)
                    for (var x = 0; x < pattern[i]; x++) normalized.Add((i & 1) == 0);
            }
            return TryDecodeNormalizedCode39(normalized.ToArray(), options, out text);
        } finally { workspace.Dispose(); }
    }

    private static bool TryDecodeNormalizedCode39(bool[] modules, BarcodeDecodeOptions? options, out string text) {
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

            if (count < 2 || index != modules.Length) return false;
            if (rented[0] != '*' || rented[count - 1] != '*') return false;

            var rawLen = count - 2;
            var raw = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            var policy = options?.Code39Checksum ?? Code39ChecksumPolicy.None;
            if (policy != Code39ChecksumPolicy.None) {
                // Optional stripping retains short ordinary data. RequireValid
                // also validates the encoder's checksum for an empty payload.
                var hasChecksum = raw.Length >= 2 || (raw.Length == 1 && policy == Code39ChecksumPolicy.RequireValid);
                var expected = hasChecksum ? GetCode39ChecksumChar(raw.AsSpan(0, raw.Length - 1)) : '#';
                if (hasChecksum && expected != '#' && raw[raw.Length - 1] == expected) {
                    raw = raw.Substring(0, raw.Length - 1);
                } else if (policy == Code39ChecksumPolicy.RequireValid) {
                    return false;
                }
            }
            text = options?.Code39FullAscii == false ? raw : DecodeCode39Extended(raw);
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
        if (Code93Tables.HasChecksums(raw.AsSpan())) raw = raw.Substring(0, raw.Length - 2);

        text = DecodeCode93Extended(raw);
        return true;
    }

    private static bool TryDecodeCodabar(ref ModuleRunWorkspace runWorkspace, BarcodeDecodeOptions? options, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
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
                if (!CodabarTables.PatternMap.Value.TryGetValue(key, out var ch)) return false;
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
            var policy = options?.CodabarChecksum ?? OptionalChecksumPolicy.None;
            if (policy == OptionalChecksumPolicy.None) return true;
            var sum = 0;
            const string alphabet = "0123456789-$:/.+ABCD";
            for (var i = 0; i < count; i++) sum += alphabet.IndexOf(rented[i]);
            if (rawLen >= 2 && sum % 16 == 0) { text = text.Substring(0, text.Length - 1); return true; }
            return policy == OptionalChecksumPolicy.StripIfValid;
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
            var valid = options?.MsiChecksumAlgorithm is { } algorithm
                ? MsiChecks.TryStrip(raw, algorithm, options.MsiExpectedDataLength, out var stripped)
                : TryStripMsiChecksum(raw, out stripped);
            if (valid) {
                text = stripped;
                return true;
            }
            if (policy == MsiChecksumPolicy.RequireValid) return false;
        }

        text = raw;
        return true;
    }

    private static bool TryDecodeCode11(ref ModuleRunWorkspace runWorkspace, BarcodeDecodeOptions? options, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
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

    private static bool TryDecodePlessey(ref ModuleRunWorkspace runWorkspace, BarcodeDecodeOptions? options, out string text) {
        var modules = runWorkspace.Modules;
        if (TryDecodePlesseyInternal(ref runWorkspace, options, out text)) return true;
        var reversed = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) reversed[i] = modules[modules.Length - 1 - i];
        var reversedWorkspace = new ModuleRunWorkspace(reversed);
        try {
            return TryDecodePlesseyInternal(ref reversedWorkspace, options, out text);
        } finally {
            reversedWorkspace.Dispose();
        }
    }

    private static bool TryDecodePlesseyInternal(ref ModuleRunWorkspace runWorkspace, BarcodeDecodeOptions? options, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 24) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
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

    private static bool TryDecodePlesseyPair(int barRun, int spaceRun, out bool bit) {
        bit = false;
        if (barRun == spaceRun) return false;
        bit = barRun > spaceRun;
        return true;
    }

    private static bool TryDecodePatchCode(ref ModuleRunWorkspace runWorkspace, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
        if (runs.Length != 7) return false;

        if (!TryGetRunThreshold(runs, startIndex: 0, step: 1, out var threshold)) return false;
        if (!AreRunsBelowThreshold(runs, startIndex: 1, step: 2, threshold)) return false;

        var key = BuildPatchKey(runs, threshold);

        if (!PatchCodeTables.PatternMap.TryGetValue(key, out var symbol)) return false;

        text = symbol.ToString();
        return true;
    }

    private static bool TryDecodeItf14(ref ModuleRunWorkspace runWorkspace, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
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

    private static bool TryDecodeItf(ref ModuleRunWorkspace runWorkspace, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
        if (runs.Length < 7) return false;
        if (!TryGetRunThreshold(runs, startIndex: 0, step: 1, out var threshold)) return false;
        if (!TryValidateItfStart(runs, threshold)) return false;
        if (!TryDecodeItfPairs(runs, threshold, startIndex: 4, out var digits, out var stopPos)) return false;
        if (!TryValidateItfStop(runs, threshold, stopPos)) return false;

        text = new string(digits);
        return true;
    }

    private static bool TryDecodeMatrix2of5(ref ModuleRunWorkspace runWorkspace, out string text) {
        return Code25.Discrete2Of5Encoder.TryDecode(runWorkspace.Runs, SymbolFormat.Matrix2Of5, out text);
    }

    private static bool TryDecodeIndustrial2of5(ref ModuleRunWorkspace runWorkspace, out string text) {
        return Code25.Discrete2Of5Encoder.TryDecode(runWorkspace.Runs, SymbolFormat.Industrial2Of5, out text);
    }

    private static bool TryDecodeIata2of5(ref ModuleRunWorkspace runWorkspace, out string text) {
        return Code25.Discrete2Of5Encoder.TryDecode(runWorkspace.Runs, SymbolFormat.Iata2Of5, out text);
    }

    private static bool TryValidateItfStart(ReadOnlySpan<int> runs, double threshold) {
        if (runs.Length < 4) return false;
        for (var i = 0; i < 4; i++) {
            if (runs[i] > threshold) return false;
        }
        return true;
    }

    private static bool TryValidateItfStop(ReadOnlySpan<int> runs, double threshold, int pos) {
        if (pos + 2 >= runs.Length) return false;
        if (runs[pos] <= threshold) return false;
        if (runs[pos + 1] > threshold) return false;
        if (runs[pos + 2] > threshold) return false;
        return true;
    }

    private static bool TryDecodeItfPairs(ReadOnlySpan<int> runs, double threshold, int startIndex, out char[] digits, out int stopPos) {
        digits = Array.Empty<char>();
        stopPos = 0;

        var remaining = runs.Length - startIndex;
        if (remaining < 3) return false;
        if ((remaining - 3) % 10 != 0) return false;

        var pairs = (remaining - 3) / 10;
        if (pairs <= 0) return false;

        var decoded = new char[pairs * 2];
        var pos = startIndex;
        for (var pair = 0; pair < pairs; pair++) {
            var barKey = PatternKey(runs, pos, threshold);
            var spaceKey = PatternKey(runs, pos + 1, threshold);
            if (!Itf14PatternMap.Value.TryGetValue(barKey, out var leftDigit)) return false;
            if (!Itf14PatternMap.Value.TryGetValue(spaceKey, out var rightDigit)) return false;
            decoded[pair * 2] = (char)('0' + leftDigit);
            decoded[pair * 2 + 1] = (char)('0' + rightDigit);
            pos += 10;
        }

        digits = decoded;
        stopPos = pos;
        return true;
    }

    private static int PatternKey(ReadOnlySpan<int> runs, int start, double threshold) {
        var key = 0;
        var idx = start;
        for (var i = 0; i < 5; i++) {
            if (idx >= runs.Length) return -1;
            if (runs[idx] > threshold) key |= 1 << (4 - i);
            idx += 2;
        }
        return key;
    }

    private static bool TryGetRunThreshold(ReadOnlySpan<int> runs, int startIndex, int step, out double threshold) {
        threshold = 0;
        if (runs.Length == 0 || step <= 0 || startIndex < 0 || startIndex >= runs.Length) return false;
        var min = int.MaxValue;
        var max = 0;
        for (var i = startIndex; i < runs.Length; i += step) {
            var value = runs[i];
            if (value < min) min = value;
            if (value > max) max = value;
        }
        if (min <= 0 || (long)max < (long)min * 2) return false;
        threshold = ((long)min + max) / 2.0;
        return true;
    }

    private static bool AreRunsBelowThreshold(ReadOnlySpan<int> runs, int startIndex, int step, double threshold) {
        for (var i = startIndex; i < runs.Length; i += step) {
            if (runs[i] > threshold) return false;
        }
        return true;
    }

    private static int BuildPatchKey(ReadOnlySpan<int> runs, double threshold) {
        var key = 0;
        for (var i = 0; i < 4; i++) {
            if (runs[i * 2] > threshold) key |= 1 << (3 - i);
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

    private static bool TryDecodeGs1DataBarTruncated(bool[] modules, out string text) {
        return DataBar14Decoder.TryDecodeTruncated(modules, out text);
    }

    private static bool TryDecodeGs1DataBarOmnidirectional(bool[] modules, out string text) {
        return DataBar14Decoder.TryDecodeOmnidirectional(modules, out text);
    }

    private static bool TryDecodeGs1DataBarLimited(bool[] modules, out string text) {
        return DataBarLimitedDecoder.TryDecode(modules, out text);
    }

    private static bool TryDecodeGs1DataBarExpanded(bool[] modules, out string text) {
        return DataBarExpandedDecoder.TryDecodeExpanded(modules, out text);
    }

}
