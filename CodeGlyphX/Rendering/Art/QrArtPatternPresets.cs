using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Coordinated palettes and QR treatments for the six procedural artwork families.</summary>
public static class QrArtPatternPresets {
    /// <summary>Creates independent, editable pattern options. Equal seeds reproduce equal artwork.</summary>
    public static QrArtPatternOptions CreatePatternOptions(QrArtPattern pattern, int seed = 1) {
        ValidatePattern(pattern);
        var colors = pattern switch {
            QrArtPattern.Marble => new[] { new Rgba32(247, 38, 138), new Rgba32(102, 46, 226), new Rgba32(0, 202, 210), new Rgba32(255, 189, 35) },
            QrArtPattern.Waves => new[] { new Rgba32(16, 58, 190), new Rgba32(0, 174, 202), new Rgba32(19, 217, 164), new Rgba32(252, 218, 77) },
            QrArtPattern.Sunburst => new[] { new Rgba32(255, 193, 30), new Rgba32(255, 72, 69), new Rgba32(213, 25, 143), new Rgba32(85, 37, 172) },
            QrArtPattern.Geometric => new[] { new Rgba32(243, 44, 106), new Rgba32(29, 163, 198), new Rgba32(253, 170, 22), new Rgba32(98, 51, 174) },
            QrArtPattern.Botanical => new[] { new Rgba32(6, 123, 95), new Rgba32(39, 179, 118), new Rgba32(225, 66, 136), new Rgba32(252, 155, 39) },
            _ => new[] { new Rgba32(37, 81, 198), new Rgba32(0, 163, 176), new Rgba32(237, 58, 146), new Rgba32(246, 164, 20) }
        };
        return new QrArtPatternOptions { Pattern = pattern, Seed = seed, RotationDegrees = (int)pattern * 17, Colors = colors };
    }

    /// <summary>
    /// Creates independent, editable rendering options. The default eighteen pixels per module
    /// supports comparison at the original and half size; inspect each result's scan checks.
    /// </summary>
    public static QrImageCompositionOptions CreateCompositionOptions(QrArtPattern pattern, int moduleSize = 18) {
        ValidatePattern(pattern);
        if (moduleSize < 6 || moduleSize > 64) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        var style = pattern switch {
            QrArtPattern.Waves => QrImageArtStyle.Ribbons,
            QrArtPattern.Sunburst => QrImageArtStyle.CrossStitch,
            QrArtPattern.Botanical => QrImageArtStyle.Weave,
            QrArtPattern.Circuit => QrImageArtStyle.Circuit,
            _ => QrImageArtStyle.ModuleShape
        };
        var finders = pattern switch {
            QrArtPattern.Waves or QrArtPattern.Botanical => QrImageFinderStyle.Circular,
            QrArtPattern.Sunburst or QrArtPattern.Geometric => QrImageFinderStyle.Chamfered,
            _ => QrImageFinderStyle.Rounded
        };
        return new QrImageCompositionOptions {
            ModuleSize = moduleSize, Strength = 0.96,
            Canvas = new QrImageCanvasOptions { PaddingModules = 8 },
            Art = new QrImageArtOptions {
                Style = style, Shape = QrPngModuleShape.ConnectedRounded, Scale = 0.95, DetailProtection = 0.3,
                Finders = finders, FunctionalForeground = new Rgba32(21, 26, 47), FunctionalBackground = new Rgba32(255, 249, 239)
            }
        };
    }

    private static void ValidatePattern(QrArtPattern pattern) {
        if (!Enum.IsDefined(typeof(QrArtPattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
    }
}
