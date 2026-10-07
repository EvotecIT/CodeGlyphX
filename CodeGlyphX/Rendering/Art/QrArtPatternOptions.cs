using CodeGlyphX.Rendering;
using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Repeatable artwork made from local geometry, without an image service or model.</summary>
public enum QrArtPattern {
    /// <summary>Flowing, multicolor marble bands.</summary>
    Marble,
    /// <summary>Layered waves of color.</summary>
    Waves,
    /// <summary>Twisting radial rays.</summary>
    Sunburst,
    /// <summary>Colorful triangles and circular tile cutouts.</summary>
    Geometric,
    /// <summary>Leaves and stems on a paper-colored ground.</summary>
    Botanical,
    /// <summary>Angular traces and circular junctions.</summary>
    Circuit
}

/// <summary>Palette, scale and orientation of a deterministic procedural artwork.</summary>
public sealed class QrArtPatternOptions {
    /// <summary>Artwork family; defaults to marble.</summary>
    public QrArtPattern Pattern { get; set; }
    /// <summary>Seed controlling phase, tile colors and motif placement. Equal inputs reproduce equal pixels.</summary>
    public int Seed { get; set; } = 1;
    /// <summary>Pattern density relative to the canvas (0.5..8). Larger values produce smaller motifs.</summary>
    public double Scale { get; set; } = 1;
    /// <summary>Clockwise pattern rotation, in degrees. Any finite angle is accepted.</summary>
    public double RotationDegrees { get; set; }
    /// <summary>Two to sixteen opaque colors. The palette is copied during rendering.</summary>
    public Rgba32[] Colors { get; set; } = new Rgba32[] {
        new(255, 58, 140), new(113, 60, 224), new(20, 196, 220), new(255, 191, 55)
    };
    /// <summary>Opaque paper color for geometric, botanical and circuit patterns.</summary>
    public Rgba32 Background { get; set; } = new(255, 248, 235);

    internal void Validate() {
        if (!Enum.IsDefined(typeof(QrArtPattern), Pattern)) throw new ArgumentOutOfRangeException(nameof(Pattern));
        if (double.IsNaN(Scale) || Scale < 0.5 || Scale > 8) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (double.IsNaN(RotationDegrees) || double.IsInfinity(RotationDegrees)) throw new ArgumentOutOfRangeException(nameof(RotationDegrees));
        if (Colors is null || Colors.Length < 2 || Colors.Length > 16) throw new ArgumentException("Use two to sixteen opaque palette colors.", nameof(Colors));
        foreach (var color in Colors) {
            if (color.A != 255) throw new ArgumentException("Palette colors must be opaque.", nameof(Colors));
        }
        if (Background.A != 255) throw new ArgumentException("Pattern background must be opaque.", nameof(Background));
    }
}
