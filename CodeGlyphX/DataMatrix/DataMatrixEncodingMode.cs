namespace CodeGlyphX.DataMatrix;

/// <summary>
/// High-level data encoding modes for Data Matrix.
/// </summary>
public enum DataMatrixEncodingMode {
    /// <summary>Choose the smallest mode automatically.</summary>
    Auto,
    /// <summary>ASCII encodation (default for Latin-1 text).</summary>
    Ascii,
    /// <summary>Base256 encodation (for binary or UTF-8 data).</summary>
    Base256
}
