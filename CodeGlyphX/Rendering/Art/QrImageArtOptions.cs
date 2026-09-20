using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Image-aware module geometry. Small scan anchors remain fixed at cell centers.</summary>
public sealed class QrImageArtOptions {
    /// <summary>Module silhouette, using the same geometry as the QR PNG renderer.</summary>
    public QrPngModuleShape Shape { get; set; } = QrPngModuleShape.Rounded;

    /// <summary>Artistic treatment. ModuleShape preserves the configured Shape; other values use their own geometry.</summary>
    public QrImageArtStyle Style { get; set; }

    /// <summary>Finder geometry. Other functional modules and the quiet zone remain unchanged.</summary>
    public QrImageFinderStyle Finders { get; set; }

    /// <summary>Nominal silhouette width relative to a module (0.65..1).</summary>
    public double Scale { get; set; } = 0.85;

    /// <summary>
    /// Adaptation to image detail (0..1). Detailed cells use smaller silhouettes, shifted toward
    /// smoother image regions. Connected shapes keep their centers aligned to preserve joins.
    /// This is local edge analysis, not subject recognition.
    /// </summary>
    public double DetailProtection { get; set; } = 0.65;

    /// <summary>Opaque ink for protected functional modules; luminance must not exceed 48/255.</summary>
    public Rgba32 FunctionalForeground { get; set; } = Rgba32.Black;

    /// <summary>Opaque light color for functional modules and the quiet zone; luminance must be at least 220/255.</summary>
    public Rgba32 FunctionalBackground { get; set; } = Rgba32.White;

    /// <summary>Optional source-aligned subject protection, reducing decoration while retaining fixed scan anchors.</summary>
    public QrImageSubjectOptions? Subject { get; set; }

    internal void Validate() {
        Subject?.Validate();
        if (!Enum.IsDefined(typeof(QrImageFinderStyle), Finders)) throw new ArgumentOutOfRangeException(nameof(Finders));
        if (!Enum.IsDefined(typeof(QrImageArtStyle), Style)) throw new ArgumentOutOfRangeException(nameof(Style));
        if (FunctionalBackground.A != 255 || 0.299 * FunctionalBackground.R + 0.587 * FunctionalBackground.G + 0.114 * FunctionalBackground.B < 220)
            throw new ArgumentException("Functional background must be opaque and light (luminance at least 220).", nameof(FunctionalBackground));
        if (FunctionalForeground.A != 255 || 0.299 * FunctionalForeground.R + 0.587 * FunctionalForeground.G + 0.114 * FunctionalForeground.B > 48)
            throw new ArgumentException("Functional foreground must be opaque and dark (luminance at most 48).", nameof(FunctionalForeground));
        if (!Enum.IsDefined(typeof(QrPngModuleShape), Shape)) throw new ArgumentOutOfRangeException(nameof(Shape));
        if (double.IsNaN(Scale) || Scale < 0.65 || Scale > 1) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (double.IsNaN(DetailProtection) || DetailProtection < 0 || DetailProtection > 1) throw new ArgumentOutOfRangeException(nameof(DetailProtection));
    }
}
