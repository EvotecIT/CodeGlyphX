namespace CodeGlyphX;

/// <summary>Explains why a scan stopped independently of whether it found symbols.</summary>
public enum ScanCompletionReason {
    /// <summary>Every requested recognition route completed.</summary>
    Completed,
    /// <summary>The configured result capacity was reached; further symbols may exist.</summary>
    SymbolLimitReached,
    /// <summary>The caller cancelled the operation.</summary>
    Cancelled,
    /// <summary>The configured total deadline elapsed.</summary>
    DeadlineExceeded
}
