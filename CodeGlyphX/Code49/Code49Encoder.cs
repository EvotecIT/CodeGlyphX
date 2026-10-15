using System;

namespace CodeGlyphX.Code49;

/// <summary>Encodes ASCII text and numeric data into Code 49 stacked symbols.</summary>
public static class Code49Encoder {
    /// <summary>Encodes text with optional GS1 semantics and an explicit row count from two through eight.</summary>
    public static MatrixSymbol Encode(string value, bool isGs1 = false, int? rows = null) =>
        Encode(value, new Code49EncodingOptions { IsGs1 = isGs1, Rows = rows });

    /// <summary>Encodes ASCII text with optional GS1, continuation, or ordered-group controls.</summary>
    public static MatrixSymbol Encode(string value, Code49EncodingOptions options) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (value.Length == 0) throw new ArgumentException("Code 49 requires a nonempty value.", nameof(value));
        var isGs1 = options.IsGs1;
        var rows = options.Rows;
        var append = options.StructuredAppend;
        if (append.HasValue && !append.Value.IsValid) throw new ArgumentException("Invalid Code 49 sequence position or total.", nameof(options));
        if ((isGs1 ? 1 : 0) + (options.IsContinuation ? 1 : 0) + (append.HasValue ? 1 : 0) > 1)
            throw new ArgumentException("GS1, continuation and ordered groups are separate Code 49 modes.", nameof(options));
        if (isGs1) value = Gs1.ElementString(value);
        var data = Code49DataCodec.Encode(value, isGs1, out var mode);
        if (options.IsContinuation || append.HasValue) {
            if (mode != 0) data.Insert(0, mode == 2 ? 48 : mode == 4 ? 43 : 44);
            mode = options.IsContinuation ? 1 : 3;
            if (append.HasValue) data.Insert(0, append.Value.Codeword);
        }
        if (data.Count > 49) throw new ArgumentException("The value exceeds Code 49's capacity.", nameof(value));
        var minimum = (data.Count + 6) / 7;
        var padding = minimum * 7 - data.Count;
        if (minimum == 1 || minimum > 6 || padding < 5) minimum++;
        var count = rows ?? minimum;
        if (count < 2 || count > 8) throw new ArgumentOutOfRangeException(nameof(rows));
        if (count < minimum) throw new ArgumentException("The value exceeds the selected row count.", nameof(value));
        var words = new int[count, 8];
        for (var row = 0; row < count; row++) {
            for (var at = 0; at < 7; at++) {
                var index = row * 7 + at;
                words[row, at] = index < data.Count ? data[index] : 48;
            }
        }
        words[count - 1, 6] = (count - 2) * 7 + mode;
        Code49Checks.Apply(words, count);
        var modules = new BitMatrix(70, count * 9 + 1);
        for (var y = 0; y < modules.Height; y += 9)
            for (var x = 0; x < 70; x++) modules[x, y] = true;
        for (var row = 0; row < count; row++) {
            for (var repeat = 0; repeat < 8; repeat++) {
                var y = row * 9 + 1 + repeat;
                modules[0, y] = true;
                for (var pair = 0; pair < 4; pair++) {
                    var valuePair = words[row, pair * 2] * 49 + words[row, pair * 2 + 1];
                    var even = row == count - 1 || Code49Tables.RowParity[row][pair] == 'E';
                    var pattern = (even ? Code49Tables.Even : Code49Tables.Odd)[valuePair];
                    for (var bit = 0; bit < 16; bit++) modules[2 + pair * 16 + bit, y] = (pattern & (1 << (15 - bit))) != 0;
                }
                for (var x = 66; x < 70; x++) modules[x, y] = true;
            }
        }
        return new MatrixSymbol(SymbolFormat.Code49, modules);
    }
}
