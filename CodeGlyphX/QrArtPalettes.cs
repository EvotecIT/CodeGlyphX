using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Curated, scan-friendly palette sets for QR art.
/// </summary>
public static class QrArtPalettes {
    /// <summary>
    /// Warm palette with high contrast on a light background.
    /// </summary>
    public static QrArtPalette Warm() => new(
        background: new Rgba32(252, 248, 242),
        foreground: new Rgba32(120, 46, 28),
        colors: new[] {
            new Rgba32(120, 46, 28),
            new Rgba32(201, 86, 46),
            new Rgba32(248, 162, 73),
            new Rgba32(255, 206, 124),
        });

    /// <summary>
    /// Cool palette with high contrast on a light background.
    /// </summary>
    public static QrArtPalette Cool() => new(
        background: new Rgba32(246, 249, 255),
        foreground: new Rgba32(18, 36, 88),
        colors: new[] {
            new Rgba32(18, 36, 88),
            new Rgba32(32, 96, 190),
            new Rgba32(74, 156, 255),
            new Rgba32(122, 204, 255),
        });

    /// <summary>
    /// Pastel palette with contrast-anchored accents.
    /// </summary>
    public static QrArtPalette Pastel() => new(
        background: new Rgba32(255, 252, 250),
        foreground: new Rgba32(74, 72, 102),
        colors: new[] {
            new Rgba32(74, 72, 102),
            new Rgba32(148, 120, 196),
            new Rgba32(238, 156, 182),
            new Rgba32(148, 210, 192),
        });

    /// <summary>
    /// High-contrast palette suitable for strong scanning reliability.
    /// </summary>
    public static QrArtPalette HighContrast() => new(
        background: new Rgba32(248, 248, 248),
        foreground: new Rgba32(18, 18, 18),
        colors: new[] {
            new Rgba32(18, 18, 18),
            new Rgba32(0, 88, 150),
            new Rgba32(0, 150, 88),
            new Rgba32(150, 44, 0),
        });

    /// <summary>
    /// Neon palette for dark canvases.
    /// </summary>
    public static QrArtPalette Neon() => new(
        background: new Rgba32(10, 12, 22),
        foreground: new Rgba32(0, 240, 220),
        colors: new[] {
            new Rgba32(0, 240, 220),
            new Rgba32(64, 180, 255),
            new Rgba32(255, 92, 255),
            new Rgba32(255, 220, 80),
        });
}

/// <summary>
/// Palette bundle with a suggested background/foreground and palette.
/// </summary>
public readonly struct QrArtPalette {
    /// <summary>
    /// Suggested background color.
    /// </summary>
    public Rgba32 Background { get; }

    /// <summary>
    /// Suggested foreground color.
    /// </summary>
    public Rgba32 Foreground { get; }

    /// <summary>
    /// Palette options for the foreground.
    /// </summary>
    public QrPngPaletteOptions Palette { get; }

    /// <summary>
    /// Create a palette bundle.
    /// </summary>
    public QrArtPalette(Rgba32 background, Rgba32 foreground, Rgba32[] colors) {
        Background = background;
        Foreground = foreground;
        Palette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            Colors = colors ?? new[] { foreground },
        };
    }
}

