using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Encodes MicroPDF417 symbols into a <see cref="BitMatrix"/>.
/// </summary>
public static class MicroPdf417Encoder {
    /// <summary>
    /// Encodes a text payload as MicroPDF417.
    /// </summary>
    public static BitMatrix Encode(string text, MicroPdf417EncodeOptions? options = null) {
        options ??= new MicroPdf417EncodeOptions();
        var dataCodewords = Pdf417HighLevelEncoder.Encode(text, options.Compaction, options.TextEncoding);
        return EncodeCodewords(dataCodewords, options);
    }

    /// <summary>
    /// Encodes a byte payload as MicroPDF417.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, MicroPdf417EncodeOptions? options = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        options ??= new MicroPdf417EncodeOptions();
        var dataCodewords = EncodeByteCompaction(data);
        return EncodeCodewords(dataCodewords, options);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes a byte payload as MicroPDF417.
    /// </summary>
    public static BitMatrix EncodeBytes(ReadOnlySpan<byte> data, MicroPdf417EncodeOptions? options = null) {
        options ??= new MicroPdf417EncodeOptions();
        var dataCodewords = EncodeByteCompaction(data);
        return EncodeCodewords(dataCodewords, options);
    }
#endif

    private static BitMatrix EncodeCodewords(List<int> dataCodewords, MicroPdf417EncodeOptions options) {
        if (dataCodewords is null) throw new ArgumentNullException(nameof(dataCodewords));

        var variant = SelectVariant(dataCodewords.Count, options.Columns, options.Rows);
        var columns = MicroPdf417Tables.MicroVariants[variant];
        var rows = MicroPdf417Tables.MicroVariants[variant + 34];
        var eccCount = MicroPdf417Tables.MicroVariants[variant + 68];
        var coeffOffset = MicroPdf417Tables.MicroVariants[variant + 102];

        var dataCapacity = (columns * rows) - eccCount;
        if (dataCodewords.Count > dataCapacity) {
            throw new ArgumentException("Input too long for MicroPDF417.", nameof(dataCodewords));
        }

        var codewords = new List<int>(columns * rows);
        codewords.AddRange(dataCodewords);
        while (codewords.Count < dataCapacity) {
            codewords.Add(900);
        }

        var ecc = GenerateErrorCorrection(codewords, eccCount, coeffOffset);
        codewords.AddRange(ecc);

        return BuildMatrix(codewords, columns, rows, variant);
    }

    private static int SelectVariant(int codewordCount, int? columns, int? rows) {
        if (columns is not null && (columns < 1 || columns > 4)) {
            throw new ArgumentOutOfRangeException(nameof(columns), "MicroPDF417 columns must be between 1 and 4.");
        }
        if (rows is not null && (rows < 4 || rows > 44)) {
            throw new ArgumentOutOfRangeException(nameof(rows), "MicroPDF417 rows must be between 4 and 44.");
        }

        var sizeMatched = false;
        for (var i = 0; i < 34; i++) {
            var variant = MicroPdf417Tables.MicroAutosize[i + 34] - 1;
            var cols = MicroPdf417Tables.MicroVariants[variant];
            var rowCount = MicroPdf417Tables.MicroVariants[variant + 34];
            if ((columns is null || columns == cols) && (rows is null || rows == rowCount)) {
                sizeMatched = true;
                var maxCodewords = MicroPdf417Tables.MicroAutosize[i];
                if (codewordCount <= maxCodewords) return variant;
            }
        }

        if (sizeMatched) {
            throw new ArgumentException("Input too long for MicroPDF417.");
        }
        throw new ArgumentException("No MicroPDF417 variant found.");
    }

    private static BitMatrix BuildMatrix(List<int> codewords, int columns, int rows, int variant) {
        var leftRap = MicroPdf417Tables.RapTable[variant];
        var centreRap = MicroPdf417Tables.RapTable[variant + 34];
        var rightRap = MicroPdf417Tables.RapTable[variant + 68];
        var cluster = MicroPdf417Tables.RapTable[variant + 102] / 3;

        BitMatrix? matrix = null;
        var codewordIndex = 0;

        for (var row = 0; row < rows; row++) {
            var bits = BuildRowBits(codewords, codewordIndex, columns, cluster, leftRap, centreRap, rightRap);
            matrix ??= new BitMatrix(bits.Count, rows);
            if (bits.Count != matrix.Width) {
                throw new InvalidOperationException("MicroPDF417 row width mismatch.");
            }

            for (var x = 0; x < bits.Count; x++) {
                if (bits[x]) matrix[x, row] = true;
            }

            codewordIndex += columns;
            leftRap = NextRap(leftRap);
            centreRap = NextRap(centreRap);
            rightRap = NextRap(rightRap);
            cluster = (cluster + 1) % 3;
        }

        return matrix!;
    }

    private static int NextRap(int value) => value == 52 ? 1 : value + 1;

    private static List<bool> BuildRowBits(List<int> codewords, int start, int columns, int cluster, int leftRap, int centreRap, int rightRap) {
        var codebarre = new StringBuilder();
        var offset = cluster * 929;

        codebarre.Append(MicroPdf417Tables.RapLR[leftRap]);
        codebarre.Append('1');
        codebarre.Append(MicroPdf417Tables.CodewordPatterns[offset + codewords[start]]);
        codebarre.Append('1');

        if (columns == 3) {
            codebarre.Append(MicroPdf417Tables.RapC[centreRap]);
        }

        if (columns >= 2) {
            codebarre.Append('1');
            codebarre.Append(MicroPdf417Tables.CodewordPatterns[offset + codewords[start + 1]]);
            codebarre.Append('1');
        }

        if (columns == 4) {
            codebarre.Append(MicroPdf417Tables.RapC[centreRap]);
        }

        if (columns >= 3) {
            codebarre.Append('1');
            codebarre.Append(MicroPdf417Tables.CodewordPatterns[offset + codewords[start + 2]]);
            codebarre.Append('1');
        }

        if (columns == 4) {
            codebarre.Append('1');
            codebarre.Append(MicroPdf417Tables.CodewordPatterns[offset + codewords[start + 3]]);
            codebarre.Append('1');
        }

        codebarre.Append(MicroPdf417Tables.RapLR[rightRap]);
        codebarre.Append('1');

        var bits = new List<bool>(MicroPdf417Tables.GetRowWidth(columns));
        var flip = true;
        for (var i = 0; i < codebarre.Length; i++) {
            var ch = codebarre[i];
            if (ch >= '0' && ch <= '9') {
                var count = ch - '0';
                for (var j = 0; j < count; j++) {
                    bits.Add(flip);
                }
                flip = !flip;
                continue;
            }

            var mapped = ch < MicroPdf417Tables.PdfTtfMap.Length ? MicroPdf417Tables.PdfTtfMap[ch] : null;
            if (string.IsNullOrEmpty(mapped)) {
                throw new InvalidOperationException("Invalid MicroPDF417 pattern character.");
            }
            for (var j = 0; j < mapped.Length; j++) {
                bits.Add(mapped[j] == '1');
            }
        }

        return bits;
    }

    private static int[] GenerateErrorCorrection(IReadOnlyList<int> dataCodewords, int eccCount, int coeffOffset) {
        var correction = new int[eccCount];
        for (var i = 0; i < dataCodewords.Count; i++) {
            var total = (dataCodewords[i] + correction[eccCount - 1]) % 929;
            for (var j = eccCount - 1; j >= 0; j--) {
                var coefficient = MicroPdf417Tables.MicroCoefficients[coeffOffset + j];
                if (j == 0) {
                    correction[j] = (929 - (total * coefficient) % 929) % 929;
                } else {
                    correction[j] = (correction[j - 1] + 929 - (total * coefficient) % 929) % 929;
                }
            }
        }

        for (var j = 0; j < eccCount; j++) {
            if (correction[j] != 0) correction[j] = 929 - correction[j];
        }

        var ecc = new int[eccCount];
        for (var i = 0; i < eccCount; i++) {
            ecc[i] = correction[eccCount - 1 - i];
        }

        return ecc;
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
#endif
}
