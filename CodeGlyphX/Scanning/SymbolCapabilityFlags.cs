using System;

namespace CodeGlyphX;

/// <summary>
/// Declares operations implemented for a symbol format.
/// </summary>
[Flags]
public enum SymbolCapabilityFlags {
    /// <summary>No operation is implemented.</summary>
    None = 0,
    /// <summary>Module generation is implemented.</summary>
    Encode = 1 << 0,
    /// <summary>Module-level decoding is implemented.</summary>
    DecodeModules = 1 << 1,
    /// <summary>Recognition from raw or decoded images is implemented.</summary>
    ScanImage = 1 << 2,
    /// <summary>Multiple instances can be returned from one image scan.</summary>
    ScanMultiple = 1 << 3,
    /// <summary>GS1 payload encoding is implemented.</summary>
    Gs1Encode = 1 << 4,
    /// <summary>GS1/FNC1 payload decoding is implemented.</summary>
    Gs1Decode = 1 << 5,
    /// <summary>ECI payload encoding is implemented.</summary>
    EciEncode = 1 << 6,
    /// <summary>ECI payload decoding is implemented.</summary>
    EciDecode = 1 << 7,
    /// <summary>Structured-append encoding is implemented.</summary>
    StructuredAppendEncode = 1 << 8,
    /// <summary>Structured-append decoding is implemented.</summary>
    StructuredAppendDecode = 1 << 9,
    /// <summary>The image scanner reports a detected quadrilateral.</summary>
    ReportsGeometry = 1 << 10
}
