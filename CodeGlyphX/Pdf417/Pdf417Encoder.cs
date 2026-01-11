using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Encodes PDF417 barcodes.
/// </summary>
public static class Pdf417Encoder {
    private const int StartPattern = 0x1fea8; // 17 bits
    private const int StopPattern = 0x3fa29;  // 18 bits

    /// <summary>
    /// Encodes a text payload as PDF417.
    /// </summary>
    public static BitMatrix Encode(string text, Pdf417EncodeOptions? options = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        options ??= new Pdf417EncodeOptions();
        var dataCodewords = Pdf417HighLevelEncoder.Encode(text, options.Compaction, options.TextEncoding);
        return EncodeCodewords(dataCodewords, options);
    }

    /// <summary>
    /// Encodes a byte payload as PDF417 (byte compaction).
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, Pdf417EncodeOptions? options = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return EncodeBytesCore(data, options);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes a byte payload as PDF417 (byte compaction).
    /// </summary>
    public static BitMatrix EncodeBytes(ReadOnlySpan<byte> data, Pdf417EncodeOptions? options = null) {
        return EncodeBytesCore(data, options);
    }
#endif

#if NET8_0_OR_GREATER
    private static BitMatrix EncodeBytesCore(ReadOnlySpan<byte> data, Pdf417EncodeOptions? options) {
        options ??= new Pdf417EncodeOptions();
        var dataCodewords = EncodeByteCompaction(data);
        return EncodeCodewords(dataCodewords, options);
    }
#endif

    private static bool TryChooseDimensions(int dataCodewords, int eccCodewords, Pdf417EncodeOptions options, out int cols, out int rows) {
        var minCols = Clamp(options.MinColumns, 1, 30);
        var maxCols = Clamp(options.MaxColumns, minCols, 30);
        var minRows = Clamp(options.MinRows, 3, 90);
        var maxRows = Clamp(options.MaxRows, minRows, 90);

        var bestCols = 0;
        var bestRows = 0;
        var bestScore = float.MaxValue;

        for (var c = minCols; c <= maxCols; c++) {
            var r = (int)Math.Ceiling((dataCodewords + 1 + eccCodewords) / (double)c);
            if (r < minRows || r > maxRows) continue;

            var widthModules = c * Pdf417BarcodeMatrix.ColumnWidth + (options.Compact ? 35 : 69);
            var ratio = widthModules / (float)r;
            var score = Math.Abs(ratio - options.TargetAspectRatio);

            if (score < bestScore) {
                bestScore = score;
                bestCols = c;
                bestRows = r;
            }
        }

        cols = bestCols;
        rows = bestRows;
        return bestCols != 0;
    }

    private static (int cols, int rows) ChooseDimensions(int dataCodewords, int eccCodewords, Pdf417EncodeOptions options) {
        if (!TryChooseDimensions(dataCodewords, eccCodewords, options, out var cols, out var rows)) {
            throw new ArgumentException("Unable to fit PDF417 data within row/column constraints.");
        }
        return (cols, rows);
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

#if NET8_0_OR_GREATER
    private static List<int> EncodeByteCompaction(ReadOnlySpan<byte> data) {
        var codewords = new List<int>(data.Length + 3) { 901 };

        var idx = 0;
        while (idx + 6 <= data.Length) {
            long t = 0;
            for (var i = 0; i < 6; i++) {
                t = (t << 8) + data[idx + i];
            }
            idx += 6;

            var tmp = new int[5];
            for (var i = 4; i >= 0; i--) {
                tmp[i] = (int)(t % 900);
                t /= 900;
            }
            codewords.AddRange(tmp);
        }

        for (; idx < data.Length; idx++) {
            codewords.Add(data[idx]);
        }

        return codewords;
    }
#else
    private static BitMatrix EncodeBytesCore(byte[] data, Pdf417EncodeOptions? options) {
        options ??= new Pdf417EncodeOptions();
        var dataCodewords = EncodeByteCompaction(data);
        return EncodeCodewords(dataCodewords, options);
    }

    private static List<int> EncodeByteCompaction(byte[] data) {
        var codewords = new List<int>(data.Length + 3) { 901 };

        var idx = 0;
        while (idx + 6 <= data.Length) {
            long t = 0;
            for (var i = 0; i < 6; i++) {
                t = (t << 8) + data[idx + i];
            }
            idx += 6;

            var tmp = new int[5];
            for (var i = 4; i >= 0; i--) {
                tmp[i] = (int)(t % 900);
                t /= 900;
            }
            codewords.AddRange(tmp);
        }

        for (; idx < data.Length; idx++) {
            codewords.Add(data[idx]);
        }

        return codewords;
    }
#endif

    private static Pdf417BarcodeMatrix EncodeLowLevel(int[] fullCodewords, int columns, int rows, int errorCorrectionLevel, bool compact) {
        var matrix = new Pdf417BarcodeMatrix(rows, columns, compact);
        var idx = 0;

        for (var y = 0; y < rows; y++) {
            var cluster = y % 3;
            matrix.StartRow();
            EncodeChar(StartPattern, 17, matrix.GetCurrentRow());

            int left;
            int right;
            if (cluster == 0) {
                left = (30 * (y / 3)) + ((rows - 1) / 3);
                right = (30 * (y / 3)) + (columns - 1);
            } else if (cluster == 1) {
                left = (30 * (y / 3)) + (errorCorrectionLevel * 3) + ((rows - 1) % 3);
                right = (30 * (y / 3)) + ((rows - 1) / 3);
            } else {
                left = (30 * (y / 3)) + (columns - 1);
                right = (30 * (y / 3)) + (errorCorrectionLevel * 3) + ((rows - 1) % 3);
            }

            EncodeChar(Pdf417CodewordTable.Table[cluster][left], 17, matrix.GetCurrentRow());

            for (var x = 0; x < columns; x++) {
                EncodeChar(Pdf417CodewordTable.Table[cluster][fullCodewords[idx++]], 17, matrix.GetCurrentRow());
            }

            if (compact) {
                EncodeChar(StopPattern, 1, matrix.GetCurrentRow());
            } else {
                EncodeChar(Pdf417CodewordTable.Table[cluster][right], 17, matrix.GetCurrentRow());
                EncodeChar(StopPattern, 18, matrix.GetCurrentRow());
            }
        }

        return matrix;
    }

    private static void EncodeChar(int pattern, int len, Pdf417BarcodeRow row) {
        var map = 1 << (len - 1);
        var last = (pattern & map) != 0;
        var width = 0;
        for (var i = 0; i < len; i++) {
            var black = (pattern & map) != 0;
            if (last == black) {
                width++;
            } else {
                row.AddBar(last, width);
                last = black;
                width = 1;
            }
            map >>= 1;
        }
        row.AddBar(last, width);
    }

    private static BitMatrix ToBitMatrix(Pdf417BarcodeMatrix matrix) {
        var rows = matrix.GetMatrix();
        var height = rows.Length;
        var width = rows[0].Length;
        var modules = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            var row = rows[y];
            for (var x = 0; x < width; x++) {
                modules[x, y] = row[x] != 0;
            }
        }
        return modules;
    }

    private static BitMatrix EncodeCodewords(List<int> dataCodewords, Pdf417EncodeOptions options) {
        var dataCount = dataCodewords.Count;

        var requested = options.ErrorCorrectionLevel;
        var auto = requested < 0 || requested > 8;
        var eccLevel = auto
            ? Pdf417ErrorCorrection.GetErrorCorrectionLevel(requested, dataCount + 1)
            : requested;
        var eccCount = Pdf417ErrorCorrection.GetErrorCorrectionCodewordCount(eccLevel);

        int cols;
        int rows;
        if (auto) {
            if (!TryChooseDimensions(dataCount, eccCount, options, out cols, out rows)) {
                var found = false;
                for (var level = Math.Min(8, eccLevel - 1); level >= 0; level--) {
                    var count = Pdf417ErrorCorrection.GetErrorCorrectionCodewordCount(level);
                    if (!TryChooseDimensions(dataCount, count, options, out cols, out rows)) continue;
                    eccLevel = level;
                    eccCount = count;
                    found = true;
                    break;
                }
                if (!found) throw new ArgumentException("Unable to fit PDF417 data within row/column constraints.");
            }

            var capacity = rows * cols - (dataCount + 1);
            for (var level = 8; level > eccLevel; level--) {
                var count = Pdf417ErrorCorrection.GetErrorCorrectionCodewordCount(level);
                if (count <= capacity) {
                    eccLevel = level;
                    eccCount = count;
                    break;
                }
            }
        } else {
            (cols, rows) = ChooseDimensions(dataCount, eccCount, options);
        }

        var pad = rows * cols - (dataCount + 1 + eccCount);
        if (pad < 0) throw new ArgumentException("Data too long for PDF417 constraints.");

        var lengthDescriptor = dataCount + pad + 1;
        if (lengthDescriptor > 929) throw new ArgumentException("Data too long for PDF417.");

        var dataWithPad = new List<int>(lengthDescriptor);
        dataWithPad.Add(lengthDescriptor);
        dataWithPad.AddRange(dataCodewords);
        for (var i = 0; i < pad; i++) dataWithPad.Add(900);

        var ecc = Pdf417ErrorCorrection.GenerateErrorCorrection(dataWithPad, eccLevel);
        if (dataWithPad.Count + ecc.Length != rows * cols) {
            throw new InvalidOperationException("PDF417 codeword sizing mismatch.");
        }

        var fullCodewords = new int[rows * cols];
        dataWithPad.CopyTo(fullCodewords, 0);
        Array.Copy(ecc, 0, fullCodewords, dataWithPad.Count, ecc.Length);

        var matrix = EncodeLowLevel(fullCodewords, cols, rows, eccLevel, options.Compact);
        return ToBitMatrix(matrix);
    }
}
