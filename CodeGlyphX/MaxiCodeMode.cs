namespace CodeGlyphX;

/// <summary>
/// MaxiCode operating modes defined by ISO/IEC 16023.
/// </summary>
public enum MaxiCodeMode {
    /// <summary>Select Mode 2 or 3 when a primary carrier message is supplied; otherwise select Mode 4.</summary>
    Auto = 0,
    /// <summary>Structured carrier message with a numeric postal code.</summary>
    StructuredCarrierNumeric = 2,
    /// <summary>Structured carrier message with an alphanumeric postal code.</summary>
    StructuredCarrierAlphanumeric = 3,
    /// <summary>Standard error correction.</summary>
    Standard = 4,
    /// <summary>Enhanced secondary-message error correction.</summary>
    FullEcc = 5,
    /// <summary>Reader programming.</summary>
    ReaderProgramming = 6
}
