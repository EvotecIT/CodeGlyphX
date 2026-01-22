using System;
using System.Collections.Generic;

namespace CodeGlyphX.RoyalMail;

/// <summary>
/// Encodes Royal Mail 4-State Customer Code (RM4SCC) barcodes.
/// </summary>
public static class RoyalMailFourStateEncoder {
    /// <summary>
    /// Encodes a Royal Mail 4-State barcode into a matrix.
    /// </summary>
    /// <param name="content">Payload (digits and uppercase A-Z).</param>
    /// <param name="includeHeaders">When true, includes start/stop bars and checksum (RM4SCC). When false, encodes KIX (headerless).</param>
    public static BitMatrix Encode(string content, bool includeHeaders) {
        if (content is null) throw new ArgumentNullException(nameof(content));

        var symbolsCount = content.Length * 4;
        if (includeHeaders) symbolsCount += 6;

        var width = symbolsCount * 2 - 1;
        var matrix = new BitMatrix(width, RoyalMailTables.BarcodeHeight);

        var x = 0;
        if (includeHeaders) {
            SetBar(RoyalMailBarTypes.Ascender, matrix, x);
            x += 2;
        }

        var indices = new List<int>(content.Length);
        for (var i = 0; i < content.Length; i++) {
            var idx = GetCodepointIndex(content[i]);
            indices.Add(idx);
            SetCodepoint(idx, matrix, x);
            x += 8;
        }

        if (includeHeaders) {
            var sumAsc = 0;
            var sumDesc = 0;
            foreach (var idx in indices) {
                var symbol = RoyalMailTables.Symbols[idx];
                sumAsc += symbol[0].HasFlag(RoyalMailBarTypes.Ascender) ? 4 : 0;
                sumAsc += symbol[1].HasFlag(RoyalMailBarTypes.Ascender) ? 2 : 0;
                sumAsc += symbol[2].HasFlag(RoyalMailBarTypes.Ascender) ? 1 : 0;
                sumDesc += symbol[0].HasFlag(RoyalMailBarTypes.Descender) ? 4 : 0;
                sumDesc += symbol[1].HasFlag(RoyalMailBarTypes.Descender) ? 2 : 0;
                sumDesc += symbol[2].HasFlag(RoyalMailBarTypes.Descender) ? 1 : 0;
            }

            var chkAsc = sumAsc % 6 == 0 ? 5 : (sumAsc % 6 - 1);
            var chkDesc = sumDesc % 6 == 0 ? 5 : (sumDesc % 6 - 1);
            SetCodepoint(chkAsc * 6 + chkDesc, matrix, x);
            x += 8;
            SetBar(RoyalMailBarTypes.FullHeight, matrix, x);
        }

        return matrix;
    }

    private static int GetCodepointIndex(char ch) {
        if (ch >= '0' && ch <= '9') return ch - '0';
        if (ch >= 'A' && ch <= 'Z') return ch - 'A' + 10;
        throw new InvalidOperationException($"Character 0x{(int)ch:X2} is not supported by RM4SCC encoding");
    }

    private static void SetCodepoint(int codepointIndex, BitMatrix matrix, int xStart) {
        var x = xStart;
        var symbol = RoyalMailTables.Symbols[codepointIndex];
        for (var i = 0; i < symbol.Length; i++) {
            SetBar(symbol[i], matrix, x);
            x += 2;
        }
    }

    private static void SetBar(RoyalMailBarTypes bar, BitMatrix matrix, int x) {
        matrix[x, 3] = true;
        matrix[x, 4] = true;
        if (bar.HasFlag(RoyalMailBarTypes.Descender)) {
            matrix[x, 5] = true;
            matrix[x, 6] = true;
            matrix[x, 7] = true;
        }
        if (bar.HasFlag(RoyalMailBarTypes.Ascender)) {
            matrix[x, 0] = true;
            matrix[x, 1] = true;
            matrix[x, 2] = true;
        }
    }
}
