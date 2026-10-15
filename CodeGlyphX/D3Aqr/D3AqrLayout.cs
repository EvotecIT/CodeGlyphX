using System;

namespace CodeGlyphX.D3Aqr;

/// <summary>The draft category frame in half-D3-module cells measured from the bottom right.</summary>
internal static class D3AqrLayout {
    internal static void GetPitch(double ratio, out int half, out int host) {
        // These are discrete configuration choices, not approximate measured ratios.
        (half, host) = ratio switch {
            2 => (1, 1),
            2.5 => (5, 4),
            3 => (3, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(ratio), "The D3 to host pitch ratio must be 2, 2.5 or 3.")
        };
    }

    internal static int GetSize(int hostSize, int half, int hostPitch, out int offset) {
        offset = Math.Max(2 * half, 29 * half - hostSize * hostPitch);
        return offset + hostSize * hostPitch + 6 * half;
    }

    internal static BitMatrix BuildFrame(int hostSize, int category, int half, int hostPitch, out int offset) {
        var size = GetSize(hostSize, half, hostPitch, out offset);
        var matrix = new BitMatrix(size, size);
        PaintModule(matrix, half, 2, 2);
        PaintModule(matrix, half, 4, 2); PaintModule(matrix, half, 2, 4);
        PaintCell(matrix, half, 6, 2); PaintCell(matrix, half, 6, 3);
        PaintCell(matrix, half, 2, 6); PaintCell(matrix, half, 3, 6);
        PaintModule(matrix, half, 9, 2); PaintModule(matrix, half, 2, 9);
        PaintModule(matrix, half, 13, 2); PaintModule(matrix, half, 2, 13);
        var parity = 0;
        for (var bit = 0; bit < 7; bit++) {
            if ((bit & 1) == 0) PaintModule(matrix, half, 0, 31 - bit * 2);
            var value = (category >> bit) & 1;
            if (value != 0) PaintModule(matrix, half, 2, 31 - bit * 2);
            parity ^= value;
        }
        if (parity != 0) PaintModule(matrix, half, 2, 33);
        parity = 1;
        for (var bit = 7; bit < 14; bit++) {
            if ((bit & 1) != 0) PaintModule(matrix, half, 45 - bit * 2, 0);
            var value = (category >> bit) & 1;
            if (value != 0) PaintModule(matrix, half, 45 - bit * 2, 2);
            parity ^= value;
        }
        if (parity != 0) PaintModule(matrix, half, 33, 2);
        return matrix;
    }

    private static void PaintModule(BitMatrix matrix, int half, int x, int y) {
        PaintCell(matrix, half, x, y); PaintCell(matrix, half, x + 1, y);
        PaintCell(matrix, half, x, y + 1); PaintCell(matrix, half, x + 1, y + 1);
    }

    private static void PaintCell(BitMatrix matrix, int half, int x, int y) {
        var right = matrix.Width - 1 - x * half;
        var bottom = matrix.Height - 1 - y * half;
        for (var dy = 0; dy < half; dy++)
            for (var dx = 0; dx < half; dx++) matrix[right - dx, bottom - dy] = true;
    }
}
