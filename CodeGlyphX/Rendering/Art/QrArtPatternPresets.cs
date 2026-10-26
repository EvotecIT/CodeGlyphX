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
        return new QrArtPatternOptions { Pattern = pattern, Seed = seed, RotationDegrees = (int)pattern * 17, Colors = colors, Background = background };
    }

    /// <summary>
    /// Creates independent, editable rendering options. The default eighteen pixels per module
    /// supports comparison at the original and half size; inspect each result's scan checks.
    /// </summary>
    /// <remarks>Presets carry the artwork through the finder, timing and format zones as tints, and wash the
    /// surrounding canvas so the code stays the focus. The quiet zone remains a solid light colour.</remarks>
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
        var opacity = pattern switch {
            QrArtPattern.Marble => 0.9,
            QrArtPattern.Circuit => 0.7,
            QrArtPattern.Botanical => 0.55,
            _ => 0.4
        };
        // The quiet zone and the canvas wash share one light tone matched to each palette's temperature.
        var paper = pattern switch {
            QrArtPattern.Marble => new Rgba32(252, 250, 246),
            QrArtPattern.Waves => new Rgba32(246, 249, 252),
            QrArtPattern.Botanical => new Rgba32(249, 249, 241),
            QrArtPattern.Circuit => new Rgba32(245, 249, 248),
            _ => new Rgba32(255, 248, 238)
        };
        return new QrImageCompositionOptions {
            ModuleSize = moduleSize, Strength = 0.96,
            Canvas = new QrImageCanvasOptions { PaddingModules = 8, ArtOpacity = opacity, CornerRadiusModules = 2 },
            Art = new QrImageArtOptions {
                Style = style, Shape = shape, Scale = 0.95, DetailProtection = 0.3, FunctionalTint = 1,
                Finders = finders, FunctionalForeground = new Rgba32(21, 26, 47), FunctionalBackground = paper
            }
        };
    }

    private static void ValidatePattern(QrArtPattern pattern) {
        if (!Enum.IsDefined(typeof(QrArtPattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
    }
}
