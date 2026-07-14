namespace CodeGlyphX;

/// <summary>
/// Decorative guardrail strength for QR art styling.
/// </summary>
public enum QrArtGuardrailMode {
    /// <summary>
    /// Strong guardrails that preserve more conventional QR geometry.
    /// </summary>
    Conservative,
    /// <summary>
    /// Moderate guardrails with additional artistic freedom.
    /// </summary>
    Balanced,
    /// <summary>
    /// Light guardrails that allow bolder styling.
    /// </summary>
    Bold,
}
