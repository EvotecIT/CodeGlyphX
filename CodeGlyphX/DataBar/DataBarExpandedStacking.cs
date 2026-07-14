using System;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Lays GS1 DataBar Expanded character pairs out in alternating stacked rows.
/// </summary>
internal static class DataBarExpandedStacking {
    internal static BitMatrix Build(int[] elements, int totalCharacters, int requestedColumns) {
        var blockCount = (totalCharacters + 1) / 2;
        var columns = Math.Min(requestedColumns, blockCount);
        var rowCount = (blockCount + columns - 1) / columns;
        var width = rowCount == 1 ? Sum(elements) : 4 + (columns * 49);
        var height = 1 + ((rowCount - 1) * 4);
        var matrix = new BitMatrix(width, height);
        var currentBlock = 0;
        var rows = new int[rowCount][];
        var rowDirections = new bool[rowCount];

        for (var row = 0; row < rowCount; row++) {
            var blocksInRow = Math.Min(columns, blockCount - currentBlock);
            var leftToRight = ((row + 1) & 1) == 1 || (columns & 1) == 1;
            var hasPartialBlock = (totalCharacters & 1) != 0 && currentBlock + blocksInRow == blockCount;
            var specialRow = !leftToRight && (blocksInRow == 1 || hasPartialBlock);
            if (specialRow) leftToRight = true;

            var rowWidths = BuildRow(elements, totalCharacters, currentBlock, blocksInRow, leftToRight, specialRow);
            rows[row] = rowWidths;
            rowDirections[row] = leftToRight;
            var firstSourceBlock = leftToRight ? currentBlock : currentBlock + blocksInRow - 1;
            WriteRuns(matrix, row * 4, rowWidths, (firstSourceBlock & 1) != 0);
            currentBlock += blocksInRow;
        }

        FillSeparators(matrix, rows, rowDirections);
        return matrix;
    }

    private static int[] BuildRow(int[] elements, int totalCharacters, int firstBlock, int blockCount, bool leftToRight, bool specialRow) {
        var totalBlocks = (totalCharacters + 1) / 2;
        var partialBlock = (totalCharacters & 1) != 0 ? totalBlocks - 1 : -1;
        var contentElements = 0;
        for (var block = firstBlock; block < firstBlock + blockCount; block++) {
            contentElements += block == partialBlock ? 13 : 21;
        }

        var widths = new int[4 + contentElements];
        widths[0] = specialRow ? 2 : 1;
        widths[1] = 1;
        widths[widths.Length - 2] = 1;
        widths[widths.Length - 1] = 1;

        for (var rowBlock = 0; rowBlock < blockCount; rowBlock++) {
            var sourceBlock = leftToRight ? firstBlock + rowBlock : firstBlock + (blockCount - 1 - rowBlock);
            var sourceStart = 2 + (sourceBlock * 21);
            var sourceLength = sourceBlock == partialBlock ? 13 : 21;
            var destinationStart = 2;
            for (var prior = 0; prior < rowBlock; prior++) {
                var priorBlock = leftToRight ? firstBlock + prior : firstBlock + (blockCount - 1 - prior);
                destinationStart += priorBlock == partialBlock ? 13 : 21;
            }
            for (var element = 0; element < sourceLength; element++) {
                widths[destinationStart + element] = leftToRight
                    ? elements[sourceStart + element]
                    : elements[sourceStart + (sourceLength - 1 - element)];
            }
        }

        return widths;
    }

    private static void WriteRuns(BitMatrix matrix, int row, int[] widths, bool startWithBar) {
        var x = 0;
        var isBar = startWithBar;
        for (var i = 0; i < widths.Length; i++) {
            var end = x + widths[i];
            if (isBar) {
                for (; x < end && x < matrix.Width; x++) matrix[x, row] = true;
            } else {
                x = end;
            }
            isBar = !isBar;
        }
    }

    private static void FillSeparators(BitMatrix matrix, int[][] rows, bool[] rowDirections) {
        for (var row = 0; row < rows.Length - 1; row++) {
            var upper = row * 4;
            var lower = upper + 4;
            FillAdjacentSeparator(matrix, upper, upper + 1, rows[row], rowDirections[row]);
            for (var x = 5; x < matrix.Width - 4; x += 2) matrix[x, upper + 2] = true;
            FillAdjacentSeparator(matrix, lower, upper + 3, rows[row + 1], rowDirections[row + 1]);
        }
    }

    private static void FillAdjacentSeparator(BitMatrix matrix, int sourceRow, int targetRow, int[] widths, bool leftToRight) {
        var rowWidth = 0;
        for (var i = 0; i < widths.Length; i++) rowWidth += widths[i];
        var end = Math.Min(rowWidth - 4, matrix.Width - 4);
        for (var x = 4; x < end; x++) {
            if (!matrix[x, sourceRow]) matrix[x, targetRow] = true;
        }

        var contentElements = widths.Length - 4;
        var blockCount = (contentElements / 21) + (contentElements % 21 == 13 ? 1 : 0);
        var blockRunStart = 2;
        for (var block = 0; block < blockCount; block++) {
            var finderRun = blockRunStart + 8;
            var finderStart = 0;
            for (var run = 0; run < finderRun; run++) finderStart += widths[run];
            var finderEnd = finderStart;
            for (var run = 0; run < 5; run++) finderEnd += widths[finderRun + run];
            ApplyFinderSeparator(matrix, sourceRow, targetRow, finderStart, Math.Min(finderEnd, end), leftToRight);
            blockRunStart += block == blockCount - 1 && contentElements % 21 == 13 ? 13 : 21;
        }
    }

    private static void ApplyFinderSeparator(BitMatrix matrix, int sourceRow, int targetRow, int start, int end, bool leftToRight) {
        var latch = true;
        var x = leftToRight ? start : Math.Min(end, matrix.Width) - 1;
        var limit = leftToRight ? Math.Min(end, matrix.Width) : start - 1;
        var step = leftToRight ? 1 : -1;
        for (; x != limit; x += step) {
            if (!matrix[x, sourceRow]) {
                matrix[x, targetRow] = latch;
                latch = !latch;
            } else {
                matrix[x, targetRow] = false;
                latch = true;
            }
        }
    }

    private static int Sum(int[] values) {
        var sum = 0;
        for (var i = 0; i < values.Length; i++) sum += values[i];
        return sum;
    }
}
