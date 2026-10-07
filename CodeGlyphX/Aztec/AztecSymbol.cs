namespace CodeGlyphX;

/// <summary>An encoded Aztec symbol and its selected layer and data-codeword counts.</summary>
public sealed class AztecSymbol : MatrixSymbol {
    /// <summary>Gets whether the symbol uses the compact Aztec layout.</summary>
    public bool Compact { get; }
    /// <summary>Gets the square module grid size.</summary>
    public int Size => Width;
    /// <summary>Gets the selected number of layers.</summary>
    public int Layers { get; }
    /// <summary>Gets the number of encoded data codewords.</summary>
    public int DataCodewordCount { get; }

    internal AztecSymbol(bool compact, int size, int layers, int codeWords, BitMatrix matrix)
        : base(SymbolFormat.Aztec, matrix) {
        Compact = compact;
        Layers = layers;
        DataCodewordCount = codeWords;
    }
}
