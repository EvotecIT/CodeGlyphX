namespace CodeGlyphX.Pdf417;

/// <summary>
/// Result of decoding a PDF417 symbol.
/// </summary>
public sealed class Pdf417Decoded {
    /// <summary>
    /// Gets the decoded payload text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets Macro PDF417 metadata when present.
    /// </summary>
    public Pdf417MacroMetadata? Macro { get; }

    internal Pdf417Decoded(string text, Pdf417MacroMetadata? macro) {
        Text = text ?? string.Empty;
        Macro = macro;
    }
}
