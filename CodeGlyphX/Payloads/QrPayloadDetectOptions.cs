namespace CodeGlyphX.Payloads;

/// <summary>
/// Options that control payload auto-detection heuristics.
/// </summary>
public sealed class QrPayloadDetectOptions {
    /// <summary>
    /// Allows detecting bare email addresses (e.g. user@example.com).
    /// </summary>
    public bool AllowBareEmail { get; set; } = true;

    /// <summary>
    /// Allows detecting bare phone numbers (e.g. +1 555 123 4567).
    /// </summary>
    public bool AllowBarePhone { get; set; } = true;

    /// <summary>
    /// Allows detecting bare URLs (e.g. example.com).
    /// </summary>
    public bool AllowBareUrl { get; set; } = true;

    /// <summary>
    /// Prefer treating ambiguous inputs with whitespace as plain text.
    /// </summary>
    public bool PreferTextWhenAmbiguous { get; set; } = true;
}
