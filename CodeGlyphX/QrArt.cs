using System;

namespace CodeGlyphX;

/// <summary>
/// High-level QR art helpers.
/// </summary>
public static class QrArt {
    /// <summary>
    /// Creates a single art configuration using a theme, variant, and intensity.
    /// </summary>
    public static QrArtOptions Theme(
        QrArtTheme theme,
        QrArtVariant variant = QrArtVariant.Safe,
        int intensity = 50,
        QrArtSafetyMode safetyMode = QrArtSafetyMode.Safe) {
        if (intensity < 0) intensity = 0;
        if (intensity > 100) intensity = 100;

        return new QrArtOptions {
            Theme = theme,
            Variant = variant,
            Intensity = intensity,
            SafetyMode = safetyMode,
        };
    }
}

