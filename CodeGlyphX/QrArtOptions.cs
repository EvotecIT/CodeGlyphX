using System;

namespace CodeGlyphX;

/// <summary>
/// High-level QR art options that map to scan-friendly render settings.
/// </summary>
public sealed class QrArtOptions {
    /// <summary>
    /// Theme to apply.
    /// </summary>
    public QrArtTheme Theme { get; set; }

    /// <summary>
    /// Variant to apply within the theme.
    /// </summary>
    public QrArtVariant Variant { get; set; } = QrArtVariant.Safe;

    /// <summary>
    /// Art intensity (0..100). Higher values increase decorative effects.
    /// </summary>
    public int Intensity { get; set; } = 50;

    /// <summary>
    /// Safety guardrail strength applied after the theme.
    /// </summary>
    public QrArtSafetyMode SafetyMode { get; set; } = QrArtSafetyMode.Safe;

    internal void Validate() {
        if (Intensity is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(Intensity));
    }
}

