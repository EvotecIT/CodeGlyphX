// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;

namespace CodeGlyphX.DotCode;

internal static class DotCodeMatrix {
    private const int UnlitEdgeScore = -99999;

    internal static BitMatrix Fold(bool[] stream, int width, int height) {
        var matrix = new BitMatrix(width, height);
        var position = 0;
        if ((height & 1) != 0) {
            for (var row = 0; row < height; row++) {
                for (var column = 0; column < width; column++) {
                    if (((column + row) & 1) == 0 && !IsCorner(column, row, width, height)) {
                        matrix[column, height - row - 1] = stream[position++];
                    }
                }
            }
            matrix[width - 2, 0] = stream[position++];
            matrix[width - 2, height - 1] = stream[position++];
            matrix[width - 1, 1] = stream[position++];
            matrix[width - 1, height - 2] = stream[position++];
            matrix[0, 0] = stream[position++];
            matrix[0, height - 1] = stream[position];
        } else {
            for (var column = 0; column < width; column++) {
                for (var row = 0; row < height; row++) {
                    if (((column + row) & 1) == 0 && !IsCorner(column, row, width, height)) {
                        matrix[column, row] = stream[position++];
                    }
                }
            }
            matrix[width - 1, height - 2] = stream[position++];
            matrix[0, height - 2] = stream[position++];
            matrix[width - 2, height - 1] = stream[position++];
            matrix[1, height - 1] = stream[position++];
            matrix[width - 1, 0] = stream[position++];
            matrix[0, 0] = stream[position];
        }
        return matrix;
    }

    internal static bool[] Unfold(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        var stream = new bool[width * height / 2];
        var position = 0;
        if ((height & 1) != 0) {
            for (var row = 0; row < height; row++) {
                for (var column = 0; column < width; column++) {
                    if (((column + row) & 1) == 0 && !IsCorner(column, row, width, height)) {
                        stream[position++] = matrix[column, height - row - 1];
                    }
                }
            }
            stream[position++] = matrix[width - 2, 0];
            stream[position++] = matrix[width - 2, height - 1];
            stream[position++] = matrix[width - 1, 1];
            stream[position++] = matrix[width - 1, height - 2];
            stream[position++] = matrix[0, 0];
            stream[position] = matrix[0, height - 1];
        } else {
            for (var column = 0; column < width; column++) {
                for (var row = 0; row < height; row++) {
                    if (((column + row) & 1) == 0 && !IsCorner(column, row, width, height)) {
                        stream[position++] = matrix[column, row];
                    }
                }
            }
            stream[position++] = matrix[width - 1, height - 2];
            stream[position++] = matrix[0, height - 2];
            stream[position++] = matrix[width - 2, height - 1];
            stream[position++] = matrix[1, height - 1];
            stream[position++] = matrix[width - 1, 0];
            stream[position] = matrix[0, 0];
        }
        return stream;
    }

    internal static void ForceCorners(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        if ((width & 1) != 0) {
            matrix[0, 0] = true; matrix[width - 1, 0] = true; matrix[0, height - 2] = true;
            matrix[width - 1, height - 2] = true; matrix[1, height - 1] = true; matrix[width - 2, height - 1] = true;
        } else {
            matrix[0, 0] = true; matrix[width - 2, 0] = true; matrix[width - 1, 1] = true;
            matrix[width - 1, height - 2] = true; matrix[0, height - 1] = true; matrix[width - 2, height - 1] = true;
        }
    }

    internal static bool CornersAreLit(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        return (width & 1) != 0
            ? matrix[0, 0] && matrix[width - 1, 0] && matrix[0, height - 2] &&
              matrix[width - 1, height - 2] && matrix[1, height - 1] && matrix[width - 2, height - 1]
            : matrix[0, 0] && matrix[width - 2, 0] && matrix[width - 1, 1] &&
              matrix[width - 1, height - 2] && matrix[0, height - 1] && matrix[width - 2, height - 1];
    }

    internal static int Score(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        var penalty = RowPenalty(matrix) + ColumnPenalty(matrix);
        var worst = EdgeScore(matrix, horizontal: true, atEnd: false);
        if (worst < 0) return UnlitEdgeScore;
        var score = EdgeScore(matrix, horizontal: true, atEnd: true);
        if (score < 0) return UnlitEdgeScore;
        worst = Math.Min(worst * height, score * height);
        score = EdgeScore(matrix, horizontal: false, atEnd: false);
        if (score < 0) return UnlitEdgeScore;
        worst = Math.Min(worst, score * width);
        score = EdgeScore(matrix, horizontal: false, atEnd: true);
        if (score < 0) return UnlitEdgeScore;
        worst = Math.Min(worst, score * width);

        var isolated = 0;
        for (var y = 0; y < height; y++) {
            for (var x = y & 1; x < width; x += 2) {
                if (!Get(matrix, x - 1, y - 1) && !Get(matrix, x + 1, y - 1) &&
                    !Get(matrix, x - 1, y + 1) && !Get(matrix, x + 1, y + 1) &&
                    (!Get(matrix, x, y) || (!Get(matrix, x - 2, y) && !Get(matrix, x, y - 2) &&
                     !Get(matrix, x + 2, y) && !Get(matrix, x, y + 2)))) isolated++;
            }
        }
        return worst - isolated * isolated - penalty;
    }

    private static int EdgeScore(BitMatrix matrix, bool horizontal, bool atEnd) {
        var limit = horizontal ? matrix.Width : matrix.Height;
        var fixedPosition = atEnd ? (horizontal ? matrix.Height : matrix.Width) - 1 : 0;
        var start = atEnd ? (horizontal ? matrix.Width : matrix.Height) & 1 : 0;
        var count = 0;
        var first = -1;
        var last = -1;
        for (var i = start; i < limit; i += 2) {
            var value = horizontal ? matrix[i, fixedPosition] : matrix[fixedPosition, i];
            if (!value) continue;
            if (first < 0) first = i;
            last = i;
            count++;
        }
        return count == 0 ? -1 : count + last - first;
    }

    private static int ColumnPenalty(BitMatrix matrix) {
        var penalty = 0;
        var local = 0;
        for (var x = 1; x < matrix.Width - 1; x++) {
            var clear = true;
            for (var y = x & 1; y < matrix.Height; y += 2) if (matrix[x, y]) { clear = false; break; }
            if (clear) local = local == 0 ? matrix.Height : local * matrix.Height;
            else { penalty += local; local = 0; }
        }
        return penalty + local;
    }

    private static int RowPenalty(BitMatrix matrix) {
        var penalty = 0;
        var local = 0;
        for (var y = 1; y < matrix.Height - 1; y++) {
            var clear = true;
            for (var x = y & 1; x < matrix.Width; x += 2) if (matrix[x, y]) { clear = false; break; }
            if (clear) local = local == 0 ? matrix.Width : local * matrix.Width;
            else { penalty += local; local = 0; }
        }
        return penalty + local;
    }

    private static bool Get(BitMatrix matrix, int x, int y) => x >= 0 && y >= 0 && x < matrix.Width && y < matrix.Height && matrix[x, y];

    private static bool IsCorner(int column, int row, int width, int height) {
        if (column == 0 && row == 0) return true;
        if ((height & 1) != 0) {
            if ((column == width - 2 && row == 0) || (column == width - 1 && row == 1) || (column == 0 && row == height - 1)) return true;
        } else if (column == width - 1 && row == 0 || column == 0 && row == height - 2 || column == 1 && row == height - 1) return true;
        return column == width - 2 && row == height - 1 || column == width - 1 && row == height - 2;
    }
}
