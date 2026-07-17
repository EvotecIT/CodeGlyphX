namespace CodeGlyphX;

/// <summary>
/// Selects the scanner speed and accuracy tradeoff.
/// </summary>
public enum ScanProfile {
    /// <summary>Minimizes recognition work.</summary>
    Fast,
    /// <summary>Balances recognition rate and latency.</summary>
    Balanced,
    /// <summary>Prioritizes recognition rate.</summary>
    Robust,
    /// <summary>Uses bounded defaults suitable for screenshots and UI capture.</summary>
    Screen
}
