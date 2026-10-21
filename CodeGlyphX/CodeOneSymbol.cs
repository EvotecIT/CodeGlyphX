namespace CodeGlyphX;

/// <summary>An encoded Code One symbol and its resolved size.</summary>
public sealed class CodeOneSymbol : MatrixSymbol {
    /// <summary>Gets the resolved version.</summary>
    public CodeOneVersion Version { get; }
    internal CodeOneSymbol(BitMatrix modules, CodeOneVersion version) : base(SymbolFormat.CodeOne, modules) { Version = version; }
}
