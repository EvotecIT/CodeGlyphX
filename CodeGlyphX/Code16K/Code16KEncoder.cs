using System;
using System.Collections.Generic;
using CodeGlyphX.Code128;

namespace CodeGlyphX.Code16K;

/// <summary>Encodes Code 16K stacked symbols with two through sixteen rows.</summary>
public static class Code16KEncoder {
    /// <summary>Encodes Latin-1 characters, with optional GS1 semantics and an explicit data-row count.</summary>
    /// <remarks>Each data row is eight modules high, separated by a one-module bar. Unicode outside Latin-1 is rejected.</remarks>
    public static MatrixSymbol Encode(string value, bool isGs1 = false, int? rows = null) =>
        Encode(value, new Code16KEncodingOptions { IsGs1 = isGs1, Rows = rows });

    /// <summary>Encodes Latin-1 text with GS1, reader, or message-sequence controls.</summary>
    public static MatrixSymbol Encode(string value, Code16KEncodingOptions options) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (value.Length == 0) throw new ArgumentException("Code 16K requires a nonempty value.", nameof(value));
        var isGs1 = options.IsGs1;
        var rows = options.Rows;
        var append = options.StructuredAppend;
        if (append.HasValue && !append.Value.IsValid) throw new ArgumentException("Invalid Code 16K sequence position or total.", nameof(options));
        var controls = (isGs1 ? 1 : 0) + (options.ReaderInitialization ? 1 : 0)
            + (options.MessageAppend ? 1 : 0) + (append.HasValue ? 1 : 0);
        if (controls > 1) throw new ArgumentException("GS1, reader initialization, message append and ordered sequences are separate Code 16K modes.", nameof(options));
        var extended = append.HasValue && append.Value.IsExtendedDataLength;
        if (extended && rows.HasValue && rows != 16) throw new ArgumentException("Extended Code 16K blocks require sixteen rows.", nameof(options));
        if (isGs1) value = Gs1.ElementString(value);
        var data = EncodeData(value, isGs1, out var mode);
        // Controls use Set B so their numeric values cannot become digit pairs.
        if (options.ReaderInitialization || options.MessageAppend || append.HasValue) {
            if (mode != 1) data.Insert(0, mode == 0 ? 101 : 99);
            mode = 1;
            if (append.HasValue) {
                var sequence = append.Value;
                if (extended) {
                    data.InsertRange(0, new[] { sequence.Index - 1, sequence.Total - 1 });
                    mode = 7;
                } else data.InsertRange(0, new[] { sequence.Index * 10 + sequence.Total, 97 });
            } else data.Insert(0, options.ReaderInitialization ? 96 : 97);
        }
        var count = extended ? 16 : rows ?? Math.Max(2, (data.Count + 7) / 5);
        if (count < 2 || count > 16) throw new ArgumentOutOfRangeException(nameof(rows));
        if (data.Count > count * 5 - 3) throw new ArgumentException("The value exceeds the selected Code 16K capacity.", nameof(value));
        var words = new List<int>(count * 5) { (count - 2) * 7 + mode };
        words.AddRange(data);
        while (words.Count < count * 5 - 2) words.Add(103);
        var firstCheck = 0;
        var secondCheck = 0;
        for (var i = 0; i < words.Count; i++) {
            firstCheck += (i + 2) * words[i];
            secondCheck += (i + 1) * words[i];
        }
        firstCheck %= 107;
        secondCheck = (secondCheck + firstCheck * (words.Count + 1)) % 107;
        words.Add(firstCheck);
        words.Add(secondCheck);
        var matrix = new BitMatrix(70, count * 9 + 1);
        for (var y = 0; y < matrix.Height; y += 9)
            for (var x = 0; x < matrix.Width; x++) matrix[x, y] = true;
        for (var row = 0; row < count; row++) {
            var bits = new List<bool>(70);
            Code16KPatterns.Append(bits, Code16KPatterns.Starts[row % 8], true);
            bits.Add(true);
            for (var word = 0; word < 5; word++)
                Code16KPatterns.Append(bits, Code16KPatterns.Codeword(words[row * 5 + word]), false);
            var stops = extended && append!.Value.Index % 2 == 0 ? Code16KPatterns.EvenStops : Code16KPatterns.Stops;
            Code16KPatterns.Append(bits, stops[row], false);
            for (var repeat = 0; repeat < 8; repeat++)
                for (var x = 0; x < bits.Count; x++) matrix[x, row * 9 + 1 + repeat] = bits[x];
        }
        return new MatrixSymbol(SymbolFormat.Code16K, matrix);
    }

    private static List<int> EncodeData(string value, bool gs1, out int mode) {
        foreach (var character in value)
            if (character > 255) throw new ArgumentException("Code 16K text must contain Latin-1 characters.", nameof(value));
        var ascii = true;
        foreach (var character in value) if (character > 127) ascii = false;
        if (ascii) {
            var codes = Code128Encoder.EncodeCodeValues(value, gs1);
            mode = codes[0] - 103;
            if (gs1) {
                if (mode == 0) throw new ArgumentException("GS1 Code 16K cannot start with a control character.", nameof(value));
                mode = mode == 2 ? 4 : 3;
            }
            var first = gs1 ? 2 : 1;
            return new List<int>(codes.AsSpan(first, codes.Length - first - 2).ToArray());
        }
        mode = gs1 ? 3 : 1;
        var result = new List<int>();
        var set = 'B';
        foreach (var character in value) {
            if (gs1 && character == Gs1.GroupSeparator) { result.Add(102); continue; }
            var low = character & 127;
            var desired = low < 32 ? 'A' : low > 95 ? 'B' : set;
            if (desired != set) { result.Add(desired == 'A' ? 101 : 100); set = desired; }
            if (character > 127) result.Add(set == 'A' ? 101 : 100);
            result.Add(set == 'A' && low < 32 ? low + 64 : low - 32);
        }
        return result;
    }
}

/// <summary>Code 16K's row guards and eleven-module codewords, shared with Code 128 where their definitions coincide.</summary>
internal static class Code16KPatterns {
    internal static readonly string[] Starts = { "3211", "2221", "2122", "1411", "1132", "1231", "1114", "3112" };
    internal static readonly string[] Stops = {
        "3211", "2221", "2122", "1411", "1132", "1231", "1114", "3112",
        "1132", "1231", "1114", "3112", "3211", "2221", "2122", "1411"
    };
    internal static readonly string[] EvenStops = {
        "2122", "1411", "1132", "1231", "1114", "3112", "3211", "2221",
        "1114", "3112", "3211", "2221", "2122", "1411", "1132", "1231"
    };

    internal static string Codeword(int value) => value == 106 ? "211133" : Code128Tables.GetPattern(value).ToString("X6");

    internal static void Append(List<bool> bits, string widths, bool dark) {
        foreach (var width in widths) {
            for (var x = 0; x < width - '0'; x++) bits.Add(dark);
            dark = !dark;
        }
    }
}
