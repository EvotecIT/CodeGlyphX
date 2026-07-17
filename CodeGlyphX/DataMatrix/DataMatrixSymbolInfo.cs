using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataMatrix;

internal enum DataMatrixSymbolFamily {
    Square,
    OriginalRectangular,
    Dmre
}

internal readonly struct DataMatrixSymbolInfo {
    public int SymbolRows { get; }
    public int SymbolCols { get; }
    public int DataCodewords { get; }
    public int EccCodewords { get; }
    public int RegionRows { get; }
    public int RegionCols { get; }
    public int[] DataBlockSizes { get; }
    public int EccBlockSize { get; }
    public DataMatrixSymbolFamily Family { get; }

    public int CodewordCount => DataCodewords + EccCodewords;
    public int BlockCount => DataBlockSizes.Length;
    public int RegionTotalRows => SymbolRows / RegionRows;
    public int RegionTotalCols => SymbolCols / RegionCols;
    public int RegionDataRows => RegionTotalRows - 2;
    public int RegionDataCols => RegionTotalCols - 2;
    public int DataRegionRows => RegionDataRows * RegionRows;
    public int DataRegionCols => RegionDataCols * RegionCols;
    public bool IsRectangular => Family != DataMatrixSymbolFamily.Square;
    public bool IsDmre => Family == DataMatrixSymbolFamily.Dmre;

    private DataMatrixSymbolInfo(
        int symbolRows,
        int symbolCols,
        int dataCodewords,
        int eccCodewords,
        int regionRows,
        int regionCols,
        int[] dataBlockSizes,
        int eccBlockSize,
        DataMatrixSymbolFamily family) {
        SymbolRows = symbolRows;
        SymbolCols = symbolCols;
        DataCodewords = dataCodewords;
        EccCodewords = eccCodewords;
        RegionRows = regionRows;
        RegionCols = regionCols;
        DataBlockSizes = dataBlockSizes;
        EccBlockSize = eccBlockSize;
        Family = family;
    }

    public static IReadOnlyList<DataMatrixSymbolInfo> All => Symbols;

    public static bool TryGetForData(
        int dataCodewords,
        DataMatrixShape shape,
        int? requestedRows,
        int? requestedColumns,
        out DataMatrixSymbolInfo info) {
        if (requestedRows is not null && requestedColumns is not null) {
            if (TryGetForSize(requestedRows.Value, requestedColumns.Value, out var exact)
                && dataCodewords <= exact.DataCodewords) {
                info = exact;
                return true;
            }
            info = default;
            return false;
        }

        var found = false;
        var best = default(DataMatrixSymbolInfo);
        for (var i = 0; i < Symbols.Length; i++) {
            var symbol = Symbols[i];
            if (!MatchesShape(symbol, shape) || dataCodewords > symbol.DataCodewords) continue;
            if (!found || IsBetterFit(symbol, best)) {
                best = symbol;
                found = true;
            }
        }
        info = best;
        return found;
    }

    public static bool TryGetForData(int dataCodewords, out DataMatrixSymbolInfo info) {
        return TryGetForData(dataCodewords, DataMatrixShape.Square, null, null, out info);
    }

    public static bool TryGetForSize(int rows, int cols, out DataMatrixSymbolInfo info) {
        for (var i = 0; i < Symbols.Length; i++) {
            var symbol = Symbols[i];
            if (symbol.SymbolRows == rows && symbol.SymbolCols == cols) {
                info = symbol;
                return true;
            }
        }
        info = default;
        return false;
    }

    private static bool MatchesShape(DataMatrixSymbolInfo symbol, DataMatrixShape shape) {
        return shape switch {
            DataMatrixShape.Square => symbol.Family == DataMatrixSymbolFamily.Square,
            DataMatrixShape.Rectangular => symbol.Family != DataMatrixSymbolFamily.Square,
            DataMatrixShape.OriginalRectangular => symbol.Family == DataMatrixSymbolFamily.OriginalRectangular,
            DataMatrixShape.Dmre => symbol.Family == DataMatrixSymbolFamily.Dmre,
            DataMatrixShape.Any => true,
            _ => false
        };
    }

    private static bool IsBetterFit(DataMatrixSymbolInfo candidate, DataMatrixSymbolInfo current) {
        var candidateArea = candidate.SymbolRows * candidate.SymbolCols;
        var currentArea = current.SymbolRows * current.SymbolCols;
        if (candidateArea != currentArea) return candidateArea < currentArea;
        if (candidate.DataCodewords != current.DataCodewords) return candidate.DataCodewords < current.DataCodewords;
        return candidate.SymbolRows > current.SymbolRows;
    }

    private static readonly DataMatrixSymbolInfo[] Symbols = CreateSymbols();

    private static DataMatrixSymbolInfo[] CreateSymbols() {
        // Metrics are the current ISO/IEC 16022 square/original rectangular models plus the
        // ISO/IEC 21471 rectangular extension. Values are cross-checked against BWIPP commit
        // b22ce8fe921d28ac958313882665ea0e5c90bc9b (src/datamatrix.ps.src).
        return new[] {
            Create(10, 10, 1, 1, 5, 1, DataMatrixSymbolFamily.Square),
            Create(12, 12, 1, 1, 7, 1, DataMatrixSymbolFamily.Square),
            Create(14, 14, 1, 1, 10, 1, DataMatrixSymbolFamily.Square),
            Create(16, 16, 1, 1, 12, 1, DataMatrixSymbolFamily.Square),
            Create(18, 18, 1, 1, 14, 1, DataMatrixSymbolFamily.Square),
            Create(20, 20, 1, 1, 18, 1, DataMatrixSymbolFamily.Square),
            Create(22, 22, 1, 1, 20, 1, DataMatrixSymbolFamily.Square),
            Create(24, 24, 1, 1, 24, 1, DataMatrixSymbolFamily.Square),
            Create(26, 26, 1, 1, 28, 1, DataMatrixSymbolFamily.Square),
            Create(32, 32, 2, 2, 36, 1, DataMatrixSymbolFamily.Square),
            Create(36, 36, 2, 2, 42, 1, DataMatrixSymbolFamily.Square),
            Create(40, 40, 2, 2, 48, 1, DataMatrixSymbolFamily.Square),
            Create(44, 44, 2, 2, 56, 1, DataMatrixSymbolFamily.Square),
            Create(48, 48, 2, 2, 68, 1, DataMatrixSymbolFamily.Square),
            Create(52, 52, 2, 2, 84, 2, DataMatrixSymbolFamily.Square),
            Create(64, 64, 4, 4, 112, 2, DataMatrixSymbolFamily.Square),
            Create(72, 72, 4, 4, 144, 4, DataMatrixSymbolFamily.Square),
            Create(80, 80, 4, 4, 192, 4, DataMatrixSymbolFamily.Square),
            Create(88, 88, 4, 4, 224, 4, DataMatrixSymbolFamily.Square),
            Create(96, 96, 4, 4, 272, 4, DataMatrixSymbolFamily.Square),
            Create(104, 104, 4, 4, 336, 6, DataMatrixSymbolFamily.Square),
            Create(120, 120, 6, 6, 408, 6, DataMatrixSymbolFamily.Square),
            Create(132, 132, 6, 6, 496, 8, DataMatrixSymbolFamily.Square),
            Create(144, 144, 6, 6, 620, 10, DataMatrixSymbolFamily.Square),

            Create(8, 18, 1, 1, 7, 1, DataMatrixSymbolFamily.OriginalRectangular),
            Create(8, 32, 1, 2, 11, 1, DataMatrixSymbolFamily.OriginalRectangular),
            Create(12, 26, 1, 1, 14, 1, DataMatrixSymbolFamily.OriginalRectangular),
            Create(12, 36, 1, 2, 18, 1, DataMatrixSymbolFamily.OriginalRectangular),
            Create(16, 36, 1, 2, 24, 1, DataMatrixSymbolFamily.OriginalRectangular),
            Create(16, 48, 1, 2, 28, 1, DataMatrixSymbolFamily.OriginalRectangular),

            Create(8, 48, 1, 2, 15, 1, DataMatrixSymbolFamily.Dmre),
            Create(8, 64, 1, 4, 18, 1, DataMatrixSymbolFamily.Dmre),
            Create(8, 80, 1, 4, 22, 1, DataMatrixSymbolFamily.Dmre),
            Create(8, 96, 1, 4, 28, 1, DataMatrixSymbolFamily.Dmre),
            Create(8, 120, 1, 6, 32, 1, DataMatrixSymbolFamily.Dmre),
            Create(8, 144, 1, 6, 36, 1, DataMatrixSymbolFamily.Dmre),
            Create(12, 64, 1, 4, 27, 1, DataMatrixSymbolFamily.Dmre),
            Create(12, 88, 1, 4, 36, 1, DataMatrixSymbolFamily.Dmre),
            Create(16, 64, 1, 4, 36, 1, DataMatrixSymbolFamily.Dmre),
            Create(20, 36, 1, 2, 28, 1, DataMatrixSymbolFamily.Dmre),
            Create(20, 44, 1, 2, 34, 1, DataMatrixSymbolFamily.Dmre),
            Create(20, 64, 1, 4, 42, 1, DataMatrixSymbolFamily.Dmre),
            Create(22, 48, 1, 2, 38, 1, DataMatrixSymbolFamily.Dmre),
            Create(24, 48, 1, 2, 41, 1, DataMatrixSymbolFamily.Dmre),
            Create(24, 64, 1, 4, 46, 1, DataMatrixSymbolFamily.Dmre),
            Create(26, 40, 1, 2, 38, 1, DataMatrixSymbolFamily.Dmre),
            Create(26, 48, 1, 2, 42, 1, DataMatrixSymbolFamily.Dmre),
            Create(26, 64, 1, 4, 50, 1, DataMatrixSymbolFamily.Dmre)
        };
    }

    private static DataMatrixSymbolInfo Create(
        int rows,
        int columns,
        int regionRows,
        int regionColumns,
        int eccCodewords,
        int blockCount,
        DataMatrixSymbolFamily family) {
        var mappingRows = rows - 2 * regionRows;
        var mappingColumns = columns - 2 * regionColumns;
        var dataCodewords = mappingRows * mappingColumns / 8 - eccCodewords;
        var blockSizes = new int[blockCount];
        var baseBlockSize = dataCodewords / blockCount;
        var longerBlocks = dataCodewords % blockCount;
        for (var i = 0; i < blockSizes.Length; i++) blockSizes[i] = baseBlockSize + (i < longerBlocks ? 1 : 0);
        return new DataMatrixSymbolInfo(
            rows,
            columns,
            dataCodewords,
            eccCodewords,
            regionRows,
            regionColumns,
            blockSizes,
            eccCodewords / blockCount,
            family);
    }
}
