namespace CodeGlyphX.Payloads;

/// <summary>
/// Options controlling payload parsing and validation.
/// </summary>
public sealed class QrPayloadParseOptions {
    /// <summary>
    /// When enabled, parsed payloads are validated against stricter schema rules.
    /// </summary>
    public bool Strict { get; set; }
    /// <summary>
    /// Whether unknown Wi-Fi auth types are allowed in strict mode.
    /// </summary>
    public bool AllowUnknownWifiAuth { get; set; } = false;
}
