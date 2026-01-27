namespace CodeGlyphX;

/// <summary>
/// Safety guardrail strength for QR art styling.
/// </summary>
public enum QrArtSafetyMode {
    /// <summary>
    /// Strong guardrails for maximum scan reliability.
    /// </summary>
    Safe,
    /// <summary>
    /// Balanced guardrails with moderate artistic freedom.
    /// </summary>
    Balanced,
    /// <summary>
    /// Light guardrails that allow bolder styling.
    /// </summary>
    Bold,
}
