namespace CodeGlyphX;

/// <summary>Checked Ultracode error-correction and sampled orientation information.</summary>
public sealed class UltracodeSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the recovered error-correction level.</summary>
    public UltracodeErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets the number of corrected or erased codeword positions.</summary>
    public int CorrectedCodewords { get; }
    /// <summary>Gets the clockwise print rotation relative to the sampled colour grid.</summary>
    public int RotationDegrees { get; }
    internal UltracodeSymbolMetadata(UltracodeDecoded decoded) {
        ErrorCorrectionLevel = decoded.ErrorCorrectionLevel;
        CorrectedCodewords = decoded.CorrectedCodewords;
        RotationDegrees = decoded.RotationDegrees;
    }
}
