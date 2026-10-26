using CodeGlyphX.Rendering;
using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Coordinated palettes and QR treatments for the six procedural artwork families.</summary>
public static class QrArtPatternPresets {
    /// <summary>Creates independent, editable pattern options. Equal seeds reproduce equal artwork.</summary>
    /// <remarks>Each palette keeps to three or four related hues, so the dark modules read as deep shades of the
    /// artwork and the light modules as pale tints of it.</remarks>
    public static QrArtPatternOptions CreatePatternOptions(QrArtPattern pattern, int seed = 1) {
        ValidatePattern(pattern);
        var colors = pattern switch {
            // Base stone, clouding, grey veins and a sparse gold vein.
            QrArtPattern.Marble => new[] { new Rgba32(236, 231, 223), new Rgba32(207, 199, 189), new Rgba32(104, 96, 89), new Rgba32(181, 145, 88) },
            // Navy, cobalt, teal and seafoam swells.
            QrArtPattern.Waves => new[] { new Rgba32(24, 58, 128), new Rgba32(38, 104, 196), new Rgba32(22, 150, 168), new Rgba32(104, 200, 186) },
            // Amber centre deepening through orange and coral to a plum edge.
            QrArtPattern.Sunburst => new[] { new Rgba32(255, 192, 72), new Rgba32(244, 122, 52), new Rgba32(216, 64, 84), new Rgba32(140, 44, 104) },
            // Terracotta, ochre, deep teal and blush tiles on warm paper.
            QrArtPattern.Geometric => new[] { new Rgba32(196, 86, 62), new Rgba32(226, 162, 64), new Rgba32(42, 104, 110), new Rgba32(240, 196, 176) },
            // Three leaf greens and terracotta berries.
            QrArtPattern.Botanical => new[] { new Rgba32(34, 96, 64), new Rgba32(70, 140, 84), new Rgba32(138, 176, 118), new Rgba32(200, 104, 84) },
            // Teal traces, brass pads, navy junctions and vias.
            _ => new[] { new Rgba32(22, 116, 124), new Rgba32(206, 152, 58), new Rgba32(30, 62, 112), new Rgba32(30, 62, 112) }
        };
        var background = pattern switch {
            QrArtPattern.Circuit => new Rgba32(243, 247, 245),
            QrArtPattern.Botanical => new Rgba32(250, 247, 238),
            _ => new Rgba32(252, 245, 234)
        };
        // Each family has its own motif scale, so the six designs do not share one rhythm.
        var density = pattern switch { QrArtPattern.Marble => 0.75, QrArtPattern.Waves => 0.9, QrArtPattern.Geometric => 1.25, QrArtPattern.Botanical => 0.8, QrArtPattern.Circuit => 1.1, _ => 1.0 };
        return new QrArtPatternOptions { Pattern = pattern, Seed = seed, Scale = density, RotationDegrees = (int)pattern * 17, Colors = colors, Background = background };
    }

    /// <summary>
    /// Creates independent, editable rendering options. The default eighteen pixels per module
    /// supports comparison at the original and half size; inspect each result's scan checks.
    /// </summary>
    /// <remarks>Presets use one tinted colour model for the whole symbol: the quiet zone and light modules keep the art's
    /// crisp regions in a pale band, and every dark module, reserved ones included, is a deep tone of the art. The
    /// quiet-zone field fades softly into the washed canvas, and each family places the code differently.</remarks>
    public static QrImageCompositionOptions CreateCompositionOptions(QrArtPattern pattern, int moduleSize = 18) {
        ValidatePattern(pattern);
        if (moduleSize < 6 || moduleSize > 64) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        var style = pattern switch {
            QrArtPattern.Circuit => QrImageArtStyle.Circuit,
            _ => QrImageArtStyle.ModuleShape
        };
        var shape = pattern switch {
            QrArtPattern.Sunburst => QrModuleShape.Circle,
            QrArtPattern.Geometric => QrModuleShape.Squircle,
            QrArtPattern.Waves => QrModuleShape.ConnectedSquircle,
            QrArtPattern.Botanical => QrModuleShape.Leaf,
            _ => QrModuleShape.ConnectedRounded
        };
        var finders = pattern switch {
            QrArtPattern.Waves or QrArtPattern.Sunburst => QrImageFinderStyle.Circular,
            QrArtPattern.Geometric => QrImageFinderStyle.Chamfered,
            _ => QrImageFinderStyle.Rounded
        };
        // Each family has its own composition: the code sits off-centre in a margin sized for its motif, and the
        // art's density gradient runs away from the code. The code's footprint shows the art in a high key.
        var (padding, x, y, opacity, saturation) = pattern switch {
            QrArtPattern.Marble => (10, 0.68, 0.34, 0.92, 0.75),
            QrArtPattern.Waves => (12, 0.74, 0.7, 0.75, 0.7),
            QrArtPattern.Sunburst => (9, 0.36, 0.62, 0.7, 0.8),
            QrArtPattern.Geometric => (12, 0.28, 0.72, 0.72, 0.35),
            QrArtPattern.Botanical => (12, 0.7, 0.4, 0.92, 0.85),
            _ => (10, 0.42, 0.24, 0.85, 0.9)
        };
        // The canvas wash shares one light tone matched to each palette's temperature.
        var paper = pattern switch {
            QrArtPattern.Marble => new Rgba32(252, 250, 246),
            QrArtPattern.Waves => new Rgba32(246, 249, 252),
            QrArtPattern.Botanical => new Rgba32(249, 249, 241),
            QrArtPattern.Circuit => new Rgba32(245, 249, 248),
            _ => new Rgba32(255, 248, 238)
        };
        // Palette-owned dark tones where a deep tone of the art would read muddy: Geometric's deep teal, the
        // Sunburst's aubergine on the warm rays, and a teal-black for Waves rather than navy on mid-blue.
        var ink = pattern switch {
            QrArtPattern.Geometric => new Rgba32(18, 52, 58),
            QrArtPattern.Sunburst => new Rgba32(62, 16, 54),
            QrArtPattern.Waves => new Rgba32(6, 38, 50),
            _ => new Rgba32(21, 26, 47)
        };
        var options = new QrImageCompositionOptions {
            ModuleSize = moduleSize, Strength = 0.96,
            Canvas = new QrImageCanvasOptions { PaddingModules = padding, PositionX = x, PositionY = y, ArtOpacity = opacity, FadeModules = 2.5 },
            Art = new QrImageArtOptions {
                Style = style, Shape = shape, Scale = 0.95, DetailProtection = 0.3, FunctionalTint = 1,
                Finders = finders, FunctionalBackground = paper, FunctionalForeground = ink, LightModuleSaturation = saturation,
                DarkModuleArt = pattern switch { QrArtPattern.Geometric or QrArtPattern.Sunburst or QrArtPattern.Waves => 0, QrArtPattern.Marble => 0.5, _ => 1 }
            }
        };
        if (pattern == QrArtPattern.Sunburst) {
            // Frame the burst so its vanishing point sits just above and left of the off-centre code.
            options.ImageZoom = 1.5;
            options.ImagePositionX = 0.72; options.ImagePositionY = 0.57;
        }
        return options;
    }
    private static void ValidatePattern(QrArtPattern pattern) {
        if (!Enum.IsDefined(typeof(QrArtPattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
    }
}
