namespace CodeGlyphX;

/// <summary>The numeric value recovered from an Aztec Rune.</summary>
public sealed class AztecRuneSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the value from zero through 255.</summary>
    public int Value { get; }

    internal AztecRuneSymbolMetadata(int value) { Value = value; }
}
