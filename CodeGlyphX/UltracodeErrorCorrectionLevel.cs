namespace CodeGlyphX;

/// <summary>Ultracode's five error-correction levels.</summary>
public enum UltracodeErrorCorrectionLevel {
    /// <summary>EC1, the lowest redundancy and greatest payload capacity.</summary>
    EC1 = 1,
    /// <summary>EC2, the default level.</summary>
    EC2 = 2,
    /// <summary>EC3.</summary>
    EC3 = 3,
    /// <summary>EC4.</summary>
    EC4 = 4,
    /// <summary>EC5, the greatest redundancy.</summary>
    EC5 = 5
}
