namespace CodeGlyphX;

/// <summary>An encoded PDF417 symbol and the dimensions and error correction selected by its encoder.</summary>
public sealed class Pdf417Symbol : MatrixSymbol {
    /// <summary>Gets the number of encoded rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the number of data columns, excluding start, stop, and row indicators.</summary>
    public int Columns { get; }
    /// <summary>Gets the selected error-correction level from 0 through 8.</summary>
    public int ErrorCorrectionLevel { get; }
    /// <summary>Gets whether compact PDF417 omits right row indicators.</summary>
    public bool Compact { get; }

    internal Pdf417Symbol(BitMatrix modules, int rows, int columns, int errorCorrectionLevel, bool compact)
        : base(SymbolFormat.Pdf417, modules) {
        Rows = rows;
        Columns = columns;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Compact = compact;
    }
}
