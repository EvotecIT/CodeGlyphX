using System;

namespace CodeMatrix.DataMatrix;

internal static class DataMatrixPlacement {
    public static BitMatrix PlaceCodewords(ReadOnlySpan<byte> codewords, int rows, int cols) {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));

        var matrix = new BitMatrix(cols, rows);
        var assigned = new bool[rows * cols];
        var codewordIndex = 0;

        var row = 4;
        var col = 0;

        do {
            if (row == rows && col == 0) {
                Corner1(codewords, ref codewordIndex, rows, cols, matrix, assigned);
            }
            if (row == rows - 2 && col == 0 && (cols & 3) != 0) {
                Corner2(codewords, ref codewordIndex, rows, cols, matrix, assigned);
            }
            if (row == rows - 2 && col == 0 && (cols & 7) == 4) {
                Corner3(codewords, ref codewordIndex, rows, cols, matrix, assigned);
            }
            if (row == rows + 4 && col == 2 && (cols & 7) == 0) {
                Corner4(codewords, ref codewordIndex, rows, cols, matrix, assigned);
            }

            do {
                if (row < rows && col >= 0 && !IsAssigned(assigned, rows, cols, row, col)) {
                    Utah(codewords, ref codewordIndex, rows, cols, matrix, assigned, row, col);
                }
                row -= 2;
                col += 2;
            } while (row >= 0 && col < cols);

            row += 1;
            col += 3;

            do {
                if (row >= 0 && col < cols && !IsAssigned(assigned, rows, cols, row, col)) {
                    Utah(codewords, ref codewordIndex, rows, cols, matrix, assigned, row, col);
                }
                row += 2;
                col -= 2;
            } while (row < rows && col >= 0);

            row += 3;
            col += 1;
        } while (row < rows || col < cols);

        // Last corner fixup
        if (!IsAssigned(assigned, rows, cols, rows - 1, cols - 1)) {
            SetModule(rows, cols, matrix, assigned, rows - 1, cols - 1, true);
            SetModule(rows, cols, matrix, assigned, rows - 2, cols - 2, true);
        }

        return matrix;
    }

    public static byte[] ReadCodewords(BitMatrix modules, int codewordCount) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (codewordCount <= 0) throw new ArgumentOutOfRangeException(nameof(codewordCount));

        var rows = modules.Height;
        var cols = modules.Width;
        var codewords = new byte[codewordCount];
        var assigned = new bool[rows * cols];
        var codewordIndex = 0;

        var row = 4;
        var col = 0;

        do {
            if (row == rows && col == 0) {
                Corner1(modules, ref codewordIndex, rows, cols, codewords, assigned);
            }
            if (row == rows - 2 && col == 0 && (cols & 3) != 0) {
                Corner2(modules, ref codewordIndex, rows, cols, codewords, assigned);
            }
            if (row == rows - 2 && col == 0 && (cols & 7) == 4) {
                Corner3(modules, ref codewordIndex, rows, cols, codewords, assigned);
            }
            if (row == rows + 4 && col == 2 && (cols & 7) == 0) {
                Corner4(modules, ref codewordIndex, rows, cols, codewords, assigned);
            }

            do {
                if (row < rows && col >= 0 && !IsAssigned(assigned, rows, cols, row, col)) {
                    Utah(modules, ref codewordIndex, rows, cols, codewords, assigned, row, col);
                }
                row -= 2;
                col += 2;
            } while (row >= 0 && col < cols);

            row += 1;
            col += 3;

            do {
                if (row >= 0 && col < cols && !IsAssigned(assigned, rows, cols, row, col)) {
                    Utah(modules, ref codewordIndex, rows, cols, codewords, assigned, row, col);
                }
                row += 2;
                col -= 2;
            } while (row < rows && col >= 0);

            row += 3;
            col += 1;
        } while (row < rows || col < cols);

        return codewords;
    }

    private static bool IsAssigned(bool[] assigned, int rows, int cols, int row, int col) {
        var idx = row * cols + col;
        return assigned[idx];
    }

    private static void SetModule(int rows, int cols, BitMatrix matrix, bool[] assigned, int row, int col, bool value) {
        Wrap(rows, cols, ref row, ref col);
        var idx = row * cols + col;
        if (assigned[idx]) return;
        assigned[idx] = true;
        matrix[col, row] = value;
    }

    private static bool GetModule(int rows, int cols, BitMatrix matrix, bool[] assigned, int row, int col) {
        Wrap(rows, cols, ref row, ref col);
        var idx = row * cols + col;
        if (assigned[idx]) return false;
        assigned[idx] = true;
        return matrix[col, row];
    }

    private static void Wrap(int rows, int cols, ref int row, ref int col) {
        if (row < 0) {
            row += rows;
            col += 4 - ((rows + 4) % 8);
        }
        if (col < 0) {
            col += cols;
            row += 4 - ((cols + 4) % 8);
        }
    }

    private static void Utah(ReadOnlySpan<byte> codewords, ref int codewordIndex, int rows, int cols, BitMatrix matrix, bool[] assigned, int row, int col) {
        if (codewordIndex >= codewords.Length) return;
        var cw = codewords[codewordIndex++];
        SetModule(rows, cols, matrix, assigned, row - 2, col - 2, GetBit(cw, 0));
        SetModule(rows, cols, matrix, assigned, row - 2, col - 1, GetBit(cw, 1));
        SetModule(rows, cols, matrix, assigned, row - 1, col - 2, GetBit(cw, 2));
        SetModule(rows, cols, matrix, assigned, row - 1, col - 1, GetBit(cw, 3));
        SetModule(rows, cols, matrix, assigned, row - 1, col, GetBit(cw, 4));
        SetModule(rows, cols, matrix, assigned, row, col - 2, GetBit(cw, 5));
        SetModule(rows, cols, matrix, assigned, row, col - 1, GetBit(cw, 6));
        SetModule(rows, cols, matrix, assigned, row, col, GetBit(cw, 7));
    }

    private static void Utah(BitMatrix modules, ref int codewordIndex, int rows, int cols, byte[] codewords, bool[] assigned, int row, int col) {
        if (codewordIndex >= codewords.Length) return;
        var cw = (byte)0;
        if (GetModule(rows, cols, modules, assigned, row - 2, col - 2)) cw |= 0x80;
        if (GetModule(rows, cols, modules, assigned, row - 2, col - 1)) cw |= 0x40;
        if (GetModule(rows, cols, modules, assigned, row - 1, col - 2)) cw |= 0x20;
        if (GetModule(rows, cols, modules, assigned, row - 1, col - 1)) cw |= 0x10;
        if (GetModule(rows, cols, modules, assigned, row - 1, col)) cw |= 0x08;
        if (GetModule(rows, cols, modules, assigned, row, col - 2)) cw |= 0x04;
        if (GetModule(rows, cols, modules, assigned, row, col - 1)) cw |= 0x02;
        if (GetModule(rows, cols, modules, assigned, row, col)) cw |= 0x01;
        codewords[codewordIndex++] = cw;
    }

    private static void Corner1(ReadOnlySpan<byte> codewords, ref int codewordIndex, int rows, int cols, BitMatrix matrix, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = codewords[codewordIndex++];
        SetModule(rows, cols, matrix, assigned, rows - 1, 0, GetBit(cw, 0));
        SetModule(rows, cols, matrix, assigned, rows - 1, 1, GetBit(cw, 1));
        SetModule(rows, cols, matrix, assigned, rows - 1, 2, GetBit(cw, 2));
        SetModule(rows, cols, matrix, assigned, 0, cols - 2, GetBit(cw, 3));
        SetModule(rows, cols, matrix, assigned, 0, cols - 1, GetBit(cw, 4));
        SetModule(rows, cols, matrix, assigned, 1, cols - 1, GetBit(cw, 5));
        SetModule(rows, cols, matrix, assigned, 2, cols - 1, GetBit(cw, 6));
        SetModule(rows, cols, matrix, assigned, 3, cols - 1, GetBit(cw, 7));
    }

    private static void Corner1(BitMatrix modules, ref int codewordIndex, int rows, int cols, byte[] codewords, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = (byte)0;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 0)) cw |= 0x80;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 1)) cw |= 0x40;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 2)) cw |= 0x20;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 2)) cw |= 0x10;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 1)) cw |= 0x08;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 1)) cw |= 0x04;
        if (GetModule(rows, cols, modules, assigned, 2, cols - 1)) cw |= 0x02;
        if (GetModule(rows, cols, modules, assigned, 3, cols - 1)) cw |= 0x01;
        codewords[codewordIndex++] = cw;
    }

    private static void Corner2(ReadOnlySpan<byte> codewords, ref int codewordIndex, int rows, int cols, BitMatrix matrix, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = codewords[codewordIndex++];
        SetModule(rows, cols, matrix, assigned, rows - 3, 0, GetBit(cw, 0));
        SetModule(rows, cols, matrix, assigned, rows - 2, 0, GetBit(cw, 1));
        SetModule(rows, cols, matrix, assigned, rows - 1, 0, GetBit(cw, 2));
        SetModule(rows, cols, matrix, assigned, 0, cols - 4, GetBit(cw, 3));
        SetModule(rows, cols, matrix, assigned, 0, cols - 3, GetBit(cw, 4));
        SetModule(rows, cols, matrix, assigned, 0, cols - 2, GetBit(cw, 5));
        SetModule(rows, cols, matrix, assigned, 0, cols - 1, GetBit(cw, 6));
        SetModule(rows, cols, matrix, assigned, 1, cols - 1, GetBit(cw, 7));
    }

    private static void Corner2(BitMatrix modules, ref int codewordIndex, int rows, int cols, byte[] codewords, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = (byte)0;
        if (GetModule(rows, cols, modules, assigned, rows - 3, 0)) cw |= 0x80;
        if (GetModule(rows, cols, modules, assigned, rows - 2, 0)) cw |= 0x40;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 0)) cw |= 0x20;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 4)) cw |= 0x10;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 3)) cw |= 0x08;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 2)) cw |= 0x04;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 1)) cw |= 0x02;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 1)) cw |= 0x01;
        codewords[codewordIndex++] = cw;
    }

    private static void Corner3(ReadOnlySpan<byte> codewords, ref int codewordIndex, int rows, int cols, BitMatrix matrix, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = codewords[codewordIndex++];
        SetModule(rows, cols, matrix, assigned, rows - 3, 0, GetBit(cw, 0));
        SetModule(rows, cols, matrix, assigned, rows - 2, 0, GetBit(cw, 1));
        SetModule(rows, cols, matrix, assigned, rows - 1, 0, GetBit(cw, 2));
        SetModule(rows, cols, matrix, assigned, 0, cols - 2, GetBit(cw, 3));
        SetModule(rows, cols, matrix, assigned, 0, cols - 1, GetBit(cw, 4));
        SetModule(rows, cols, matrix, assigned, 1, cols - 1, GetBit(cw, 5));
        SetModule(rows, cols, matrix, assigned, 2, cols - 1, GetBit(cw, 6));
        SetModule(rows, cols, matrix, assigned, 3, cols - 1, GetBit(cw, 7));
    }

    private static void Corner3(BitMatrix modules, ref int codewordIndex, int rows, int cols, byte[] codewords, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = (byte)0;
        if (GetModule(rows, cols, modules, assigned, rows - 3, 0)) cw |= 0x80;
        if (GetModule(rows, cols, modules, assigned, rows - 2, 0)) cw |= 0x40;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 0)) cw |= 0x20;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 2)) cw |= 0x10;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 1)) cw |= 0x08;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 1)) cw |= 0x04;
        if (GetModule(rows, cols, modules, assigned, 2, cols - 1)) cw |= 0x02;
        if (GetModule(rows, cols, modules, assigned, 3, cols - 1)) cw |= 0x01;
        codewords[codewordIndex++] = cw;
    }

    private static void Corner4(ReadOnlySpan<byte> codewords, ref int codewordIndex, int rows, int cols, BitMatrix matrix, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = codewords[codewordIndex++];
        SetModule(rows, cols, matrix, assigned, rows - 1, 0, GetBit(cw, 0));
        SetModule(rows, cols, matrix, assigned, rows - 1, cols - 1, GetBit(cw, 1));
        SetModule(rows, cols, matrix, assigned, 0, cols - 3, GetBit(cw, 2));
        SetModule(rows, cols, matrix, assigned, 0, cols - 2, GetBit(cw, 3));
        SetModule(rows, cols, matrix, assigned, 0, cols - 1, GetBit(cw, 4));
        SetModule(rows, cols, matrix, assigned, 1, cols - 3, GetBit(cw, 5));
        SetModule(rows, cols, matrix, assigned, 1, cols - 2, GetBit(cw, 6));
        SetModule(rows, cols, matrix, assigned, 1, cols - 1, GetBit(cw, 7));
    }

    private static void Corner4(BitMatrix modules, ref int codewordIndex, int rows, int cols, byte[] codewords, bool[] assigned) {
        if (codewordIndex >= codewords.Length) return;
        var cw = (byte)0;
        if (GetModule(rows, cols, modules, assigned, rows - 1, 0)) cw |= 0x80;
        if (GetModule(rows, cols, modules, assigned, rows - 1, cols - 1)) cw |= 0x40;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 3)) cw |= 0x20;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 2)) cw |= 0x10;
        if (GetModule(rows, cols, modules, assigned, 0, cols - 1)) cw |= 0x08;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 3)) cw |= 0x04;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 2)) cw |= 0x02;
        if (GetModule(rows, cols, modules, assigned, 1, cols - 1)) cw |= 0x01;
        codewords[codewordIndex++] = cw;
    }

    private static bool GetBit(byte value, int bitIndex) {
        return (value & (1 << (7 - bitIndex))) != 0;
    }
}
