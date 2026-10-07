using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    // Finder borders are outside Reed-Solomon protection. Allow a small number
    // of sampling errors on each edge, while requiring the solid L and both
    // alternating timing edges in every data region, including internal regions.
    private static bool HasValidRegionBorders(BitMatrix modules, DataMatrixSymbolInfo symbol, CancellationToken cancellationToken) {
        var rows = symbol.RegionTotalRows;
        var cols = symbol.RegionTotalCols;
        var horizontalAllowance = Math.Max(1, cols / 8);
        var verticalAllowance = Math.Max(1, rows / 8);

        for (var regionRow = 0; regionRow < symbol.RegionRows; regionRow++) {
            for (var regionCol = 0; regionCol < symbol.RegionCols; regionCol++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var left = regionCol * cols;
                var top = regionRow * rows;
                var topErrors = 0;
                var bottomErrors = 0;
                for (var x = 0; x < cols; x++) {
                    if (modules[left + x, top] != ((x & 1) == 0)) topErrors++;
                    if (!modules[left + x, top + rows - 1]) bottomErrors++;
                    if (topErrors > horizontalAllowance || bottomErrors > horizontalAllowance) return false;
                }

                var leftErrors = 0;
                var rightErrors = 0;
                for (var y = 0; y < rows; y++) {
                    if (!modules[left, top + y]) leftErrors++;
                    if (modules[left + cols - 1, top + y] != ((y & 1) != 0)) rightErrors++;
                    if (leftErrors > verticalAllowance || rightErrors > verticalAllowance) return false;
                }
            }
        }
        return true;
    }
}
