using System;
using System.Collections.Generic;

namespace CodeGlyphX.Code25;

/// <summary>Owns the bar/space encodation and framing of the discrete 2-of-5 family.</summary>
internal static class Discrete2Of5Encoder {
    private static readonly string[] BarOnlyDigits = {
        "1111313111", "3111111131", "1131111131", "3131111111", "1111311131",
        "3111311111", "1131311111", "1111113131", "3111113111", "1131113111"
    };
    private static readonly string[] MatrixDigits = {
        "113311", "311131", "131131", "331111", "113131",
        "313111", "133111", "111331", "311311", "131311"
    };
    private static readonly string[] CoopDigits = {
        "331111", "111331", "113131", "113311", "131131",
        "131311", "133111", "311131", "311311", "313111"
    };

    internal static Barcode1D Encode(string content, bool includeChecksum, SymbolFormat format) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Length == 0) throw new ArgumentException("Content cannot be empty.", nameof(content));
        for (var i = 0; i < content.Length; i++) {
            if ((uint)(content[i] - '0') > 9) throw new ArgumentException("Only decimal digits are supported.", nameof(content));
        }
        if (includeChecksum) content += CheckDigit(content);
        GetPatterns(format, out var digits, out var start, out var stop);
        var segments = new List<BarSegment>(checked(content.Length * digits[0].Length + start.Length + stop.Length));
        Append(segments, start);
        foreach (var digit in content) Append(segments, digits[digit - '0']);
        Append(segments, stop);
        return new Barcode1D(segments, format);
    }

    internal static bool TryDecode(ReadOnlySpan<int> runs, SymbolFormat format, out string text) {
        text = string.Empty;
        GetPatterns(format, out var digits, out var start, out var stop);
        var body = runs.Length - start.Length - stop.Length;
        if (body <= 0 || body % digits[0].Length != 0) return false;
        var min = int.MaxValue;
        var max = 0;
        foreach (var run in runs) {
            if (run <= 0) return false;
            min = Math.Min(min, run);
            max = Math.Max(max, run);
        }
        if ((long)max < (long)min * 2 || (long)max > (long)min * 4) return false;
        var threshold = ((long)min + max) / 2.0;
        if (!Match(runs, 0, start, threshold) || !Match(runs, runs.Length - stop.Length, stop, threshold)) return false;
        var result = new char[body / digits[0].Length];
        for (var i = 0; i < result.Length; i++) {
            var found = false;
            for (var digit = 0; digit < 10; digit++) {
                if (!Match(runs, start.Length + i * digits[0].Length, digits[digit], threshold)) continue;
                result[i] = (char)('0' + digit);
                found = true;
                break;
            }
            if (!found) return false;
        }
        text = new string(result);
        return true;
    }

    private static bool Match(ReadOnlySpan<int> runs, int offset, string pattern, double threshold) {
        for (var i = 0; i < pattern.Length; i++) {
            if ((runs[offset + i] > threshold) != (pattern[i] == '3')) return false;
        }
        return true;
    }

    private static void Append(List<BarSegment> segments, string pattern) {
        foreach (var width in pattern) segments.Add(new BarSegment((segments.Count & 1) == 0, width - '0'));
    }

    private static char CheckDigit(string value) {
        var sum = 0;
        for (var i = value.Length - 1; i >= 0; i--)
            sum += (value[i] - '0') * (((value.Length - 1 - i) & 1) == 0 ? 3 : 1);
        return (char)('0' + (10 - sum % 10) % 10);
    }

    private static void GetPatterns(SymbolFormat format, out string[] digits, out string start, out string stop) {
        switch (format) {
            case SymbolFormat.Industrial2Of5: digits = BarOnlyDigits; start = "313111"; stop = "31113"; break;
            case SymbolFormat.Iata2Of5: digits = BarOnlyDigits; start = "1111"; stop = "311"; break;
            case SymbolFormat.Matrix2Of5: digits = MatrixDigits; start = "311111"; stop = "31111"; break;
            case SymbolFormat.Coop2Of5: digits = CoopDigits; start = "3131"; stop = "133"; break;
            case SymbolFormat.Datalogic2Of5: digits = MatrixDigits; start = "1111"; stop = "311"; break;
            default: throw new ArgumentOutOfRangeException(nameof(format));
        }
    }
}
