using System;

namespace CodeGlyphX;

/// <summary>
/// High-level QR art options that map to decorative render settings.
/// </summary>
public sealed class QrArtOptions {
    /// <summary>
    /// Theme to apply.
    /// </summary>
    public QrArtTheme Theme { get; set; }

    /// <summary>
    /// Variant to apply within the theme.
    /// </summary>
    public QrArtVariant Variant { get; set; } = QrArtVariant.Conservative;

    /// <summary>
    /// Art intensity (0..100). Higher values increase decorative effects.
    /// </summary>
    public int Intensity { get; set; } = 50;

    /// <summary>
    /// Decorative guardrail strength applied after the theme.
    /// </summary>
    public QrArtGuardrailMode GuardrailMode { get; set; } = QrArtGuardrailMode.Conservative;

    internal void Validate() {
        if (Intensity is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(Intensity));
    }
}

