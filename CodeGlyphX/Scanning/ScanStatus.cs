namespace CodeGlyphX;

/// <summary>
/// Describes the outcome of a unified symbol scan.
/// </summary>
public enum ScanStatus {
    /// <summary>One or more symbols were decoded.</summary>
    Success,
    /// <summary>No supported symbol was decoded.</summary>
    NoSymbolFound,
    /// <summary>The caller cancelled the operation.</summary>
    Cancelled,
    /// <summary>The configured total deadline elapsed.</summary>
    DeadlineExceeded,
    /// <summary>The encoded image or region was invalid.</summary>
    InvalidImage,
    /// <summary>None of the requested formats support image scanning.</summary>
    UnsupportedFormats
}
