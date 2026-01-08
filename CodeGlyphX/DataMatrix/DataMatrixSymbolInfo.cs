using System;

namespace CodeGlyphX.DataMatrix;

internal readonly struct DataMatrixSymbolInfo {
    public int SymbolRows { get; }
    public int SymbolCols { get; }
    public int DataCodewords { get; }
    public int EccCodewords { get; }
    public int RegionRows { get; }
    public int RegionCols { get; }
    public int[] DataBlockSizes { get; }
    public int EccBlockSize { get; }

    public int CodewordCount => DataCodewords + EccCodewords;
    public int BlockCount => DataBlockSizes.Length;
    public int RegionTotalRows => SymbolRows / RegionRows;
    public int RegionTotalCols => SymbolCols / RegionCols;
    public int RegionDataRows => RegionTotalRows - 2;
    public int RegionDataCols => RegionTotalCols - 2;
    public int DataRegionRows => RegionDataRows * RegionRows;
    public int DataRegionCols => RegionDataCols * RegionCols;

    public DataMatrixSymbolInfo(
        int symbolRows,
        int symbolCols,
        int dataCodewords,
        int eccCodewords,
        int regionRows,
        int regionCols,
        int[] dataBlockSizes,
        int eccBlockSize) {
        SymbolRows = symbolRows;
        SymbolCols = symbolCols;
        DataCodewords = dataCodewords;
        EccCodewords = eccCodewords;
        RegionRows = regionRows;
        RegionCols = regionCols;
        DataBlockSizes = dataBlockSizes;
        EccBlockSize = eccBlockSize;
    }

    public static bool TryGetForData(int dataCodewords, out DataMatrixSymbolInfo info) {
        foreach (var symbol in Symbols) {
            if (dataCodewords <= symbol.DataCodewords) {
                info = symbol;
                return true;
            }
        }
        info = default;
        return false;
    }

    public static bool TryGetForSize(int rows, int cols, out DataMatrixSymbolInfo info) {
        foreach (var symbol in Symbols) {
            if (symbol.SymbolRows == rows && symbol.SymbolCols == cols) {
                info = symbol;
                return true;
            }
        }
        info = default;
        return false;
    }

    private static readonly DataMatrixSymbolInfo[] Symbols = CreateSymbols();

    private static DataMatrixSymbolInfo[] CreateSymbols() {
        // Square symbols (ECC200).
        return new[] {
            new DataMatrixSymbolInfo(10, 10, 3, 5, 1, 1, new[] { 3 }, 5),
            new DataMatrixSymbolInfo(12, 12, 5, 7, 1, 1, new[] { 5 }, 7),
            new DataMatrixSymbolInfo(14, 14, 8, 10, 1, 1, new[] { 8 }, 10),
            new DataMatrixSymbolInfo(16, 16, 12, 12, 1, 1, new[] { 12 }, 12),
            new DataMatrixSymbolInfo(18, 18, 18, 14, 1, 1, new[] { 18 }, 14),
            new DataMatrixSymbolInfo(20, 20, 22, 18, 1, 1, new[] { 22 }, 18),
            new DataMatrixSymbolInfo(22, 22, 30, 20, 1, 1, new[] { 30 }, 20),
            new DataMatrixSymbolInfo(24, 24, 36, 24, 1, 1, new[] { 36 }, 24),
            new DataMatrixSymbolInfo(26, 26, 44, 28, 1, 1, new[] { 44 }, 28),
            new DataMatrixSymbolInfo(32, 32, 62, 36, 2, 2, new[] { 62 }, 36),
            new DataMatrixSymbolInfo(36, 36, 86, 42, 2, 2, new[] { 86 }, 42),
            new DataMatrixSymbolInfo(40, 40, 114, 48, 2, 2, new[] { 114 }, 48),
            new DataMatrixSymbolInfo(44, 44, 144, 56, 2, 2, new[] { 144 }, 56),
            new DataMatrixSymbolInfo(48, 48, 174, 68, 2, 2, new[] { 174 }, 68),
            new DataMatrixSymbolInfo(52, 52, 204, 84, 2, 2, new[] { 102, 102 }, 42),
            new DataMatrixSymbolInfo(64, 64, 280, 112, 4, 4, new[] { 140, 140 }, 56),
            new DataMatrixSymbolInfo(72, 72, 368, 144, 4, 4, new[] { 92, 92, 92, 92 }, 36),
            new DataMatrixSymbolInfo(80, 80, 456, 192, 4, 4, new[] { 114, 114, 114, 114 }, 48),
            new DataMatrixSymbolInfo(88, 88, 576, 224, 4, 4, new[] { 144, 144, 144, 144 }, 56),
            new DataMatrixSymbolInfo(96, 96, 696, 272, 4, 4, new[] { 174, 174, 174, 174 }, 68),
            new DataMatrixSymbolInfo(104, 104, 816, 336, 4, 4, new[] { 136, 136, 136, 136, 136, 136 }, 56),
            new DataMatrixSymbolInfo(120, 120, 1050, 408, 6, 6, new[] { 175, 175, 175, 175, 175, 175 }, 68),
            new DataMatrixSymbolInfo(132, 132, 1304, 496, 6, 6, new[] { 163, 163, 163, 163, 163, 163, 163, 163 }, 62),
            new DataMatrixSymbolInfo(144, 144, 1558, 620, 6, 6, new[] { 156, 156, 156, 156, 156, 156, 156, 156, 155, 155 }, 62)
        };
    }
}
