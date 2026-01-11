namespace CodeGlyphX.DataMatrix;

/// <summary>
/// High-level data encoding modes for Data Matrix.
/// </summary>
public enum DataMatrixEncodingMode {
    /// <summary>Choose the smallest mode automatically.</summary>
    Auto,
    /// <summary>ASCII encodation (default for Latin-1 text).</summary>
    Ascii,
    /// <summary>C40 encodation (uppercase optimized).</summary>
    C40,
    /// <summary>Text encodation (lowercase optimized).</summary>
    Text,
    /// <summary>X12 encodation (ANSI X12 subset).</summary>
    X12,
    /// <summary>EDIFACT encodation (ASCII 32-95).</summary>
    Edifact,
    /// <summary>Base256 encodation (for binary or UTF-8 data).</summary>
    Base256
}
