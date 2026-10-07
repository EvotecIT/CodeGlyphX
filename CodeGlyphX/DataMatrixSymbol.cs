using CodeGlyphX.DataMatrix;

namespace CodeGlyphX;

/// <summary>An encoded ECC200 Data Matrix symbol and its selected capacity.</summary>
public sealed class DataMatrixSymbol : MatrixSymbol {
    /// <summary>Gets the selected symbol row count, including finder borders.</summary>
    public int Rows => Height;
    /// <summary>Gets the selected symbol column count, including finder borders.</summary>
    public int Columns => Width;
    /// <summary>Gets whether the selected size belongs to Data Matrix Rectangular Extension.</summary>
    public bool IsDmre { get; }
    /// <summary>Gets the capacity for data codewords, including padding and control codewords.</summary>
    public int DataCodewordCapacity { get; }
    /// <summary>Gets the number of error-correction codewords.</summary>
    public int ErrorCorrectionCodewordCount { get; }

    internal DataMatrixSymbol(BitMatrix modules, DataMatrixSymbolInfo info) : base(SymbolFormat.DataMatrix, modules) {
        IsDmre = info.IsDmre;
        DataCodewordCapacity = info.DataCodewords;
        ErrorCorrectionCodewordCount = info.EccCodewords;
    }
}
