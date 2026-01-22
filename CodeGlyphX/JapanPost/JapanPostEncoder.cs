using System;
using System.Text;

namespace CodeGlyphX.JapanPost;

/// <summary>
/// Encodes Japan Post barcodes.
/// </summary>
public static class JapanPostEncoder {
    /// <summary>
    /// Encodes a Japan Post barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim().ToUpperInvariant();
        ValidateContent(content);

        var inter = BuildInter(content);
        PadInter(inter);

        var dest = new StringBuilder(67);
        dest.Append("FD");
        AppendPatternsAndChecksum(dest, inter, out var sum);
        AppendCheckDigitAndStop(dest, sum);

        return BuildMatrix(dest.ToString());
    }

    private static void ValidateContent(string content) {
        if (content.Length == 0) throw new InvalidOperationException("Japan Post content cannot be empty.");
        if (content.Length > 20) throw new InvalidOperationException("Japan Post content must be 20 characters or fewer.");
        for (var i = 0; i < content.Length; i++) {
            var ch = content[i];
            var ok = (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '-';
            if (!ok) throw new InvalidOperationException("Japan Post expects digits, A-Z, and '-' only.");
        }
    }

    private static StringBuilder BuildInter(string content) {
        var inter = new StringBuilder(40);
        for (var i = 0; i < content.Length; i++) {
            var ch = content[i];
            if ((ch >= '0' && ch <= '9') || ch == '-') {
                inter.Append(ch);
                continue;
            }

            if (ch <= 'J') {
                inter.Append('a');
                inter.Append(JapanPostTables.CheckSet[ch - 'A']);
            } else if (ch <= 'T') {
                inter.Append('b');
                inter.Append(JapanPostTables.CheckSet[ch - 'K']);
            } else {
                inter.Append('c');
                inter.Append(JapanPostTables.CheckSet[ch - 'U']);
            }
        }
        return inter;
    }

    private static void PadInter(StringBuilder inter) {
        if (inter.Length > 20) throw new InvalidOperationException("Japan Post content expands beyond 20 codewords.");
        while (inter.Length < 20) inter.Append('d');
    }

    private static void AppendPatternsAndChecksum(StringBuilder dest, StringBuilder inter, out int sum) {
        sum = 0;
        for (var i = 0; i < inter.Length; i++) {
            var ch = inter[i];
            if (!JapanPostTables.KasutIndex.TryGetValue(ch, out var idx)) {
                throw new InvalidOperationException("Japan Post input contains unsupported characters.");
            }
            dest.Append(JapanPostTables.Patterns[idx]);

            if (!JapanPostTables.CheckIndex.TryGetValue(ch, out var checkIdx)) {
                throw new InvalidOperationException("Japan Post input contains unsupported characters.");
            }
            sum += checkIdx;
        }
    }

    private static void AppendCheckDigitAndStop(StringBuilder dest, int sum) {
        var check = 19 - (sum % 19);
        if (check == 19) check = 0;
        var checkChar = JapanPostTables.CheckSet[check];
        dest.Append(JapanPostTables.Patterns[JapanPostTables.KasutIndex[checkChar]]);
        dest.Append("DF");
    }

    private static BitMatrix BuildMatrix(string pattern) {
        var width = pattern.Length * 2 - 1;
        var matrix = new BitMatrix(width, JapanPostTables.BarcodeHeight);

        var x = 0;
        for (var i = 0; i < pattern.Length; i++) {
            SetBar(matrix, x, pattern[i]);
            x += 2;
        }

        return matrix;
    }

    private static void SetBar(BitMatrix matrix, int x, char bar) {
        switch (bar) {
            case 'A':
                SetAscender(matrix, x);
                break;
            case 'D':
                SetDescender(matrix, x);
                break;
            case 'F':
                SetFull(matrix, x);
                break;
            case 'T':
                SetTracker(matrix, x);
                break;
            default:
                throw new InvalidOperationException("Japan Post encoding generated invalid bar pattern.");
        }
    }

    private static void SetAscender(BitMatrix matrix, int x) {
        matrix[x, 0] = true;
        matrix[x, 1] = true;
        matrix[x, 2] = true;
        matrix[x, 3] = true;
        matrix[x, 4] = true;
    }

    private static void SetDescender(BitMatrix matrix, int x) {
        matrix[x, 3] = true;
        matrix[x, 4] = true;
        matrix[x, 5] = true;
        matrix[x, 6] = true;
        matrix[x, 7] = true;
    }

    private static void SetFull(BitMatrix matrix, int x) {
        for (var y = 0; y < JapanPostTables.BarcodeHeight; y++) {
            matrix[x, y] = true;
        }
    }

    private static void SetTracker(BitMatrix matrix, int x) {
        matrix[x, 3] = true;
        matrix[x, 4] = true;
    }
}
