using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Gradient styles for ANSI coloring.
/// </summary>
public enum AsciiGradientType {
    /// <summary>
    /// Left-to-right gradient.
    /// </summary>
    Horizontal,
    /// <summary>
    /// Top-to-bottom gradient.
    /// </summary>
    Vertical,
    /// <summary>
    /// Top-left to bottom-right gradient.
    /// </summary>
    Diagonal,
    /// <summary>
    /// Radial gradient from a center point.
    /// </summary>
    Radial
}

/// <summary>
/// Palette selection modes for ANSI coloring.
/// </summary>
public enum AsciiPaletteMode {
    /// <summary>
    /// Cycles palette entries per row.
    /// </summary>
    CycleRows,
    /// <summary>
    /// Cycles palette entries per column.
    /// </summary>
    CycleColumns,
    /// <summary>
    /// Cycles palette entries along diagonals.
    /// </summary>
    CycleDiagonal,
    /// <summary>
    /// Randomizes palette entries per module.
    /// </summary>
    Random
}

/// <summary>
/// Gradient settings for ANSI coloring.
/// </summary>
public sealed class AsciiGradientOptions {
    /// <summary>
    /// Gradient type used for dark modules.
    /// </summary>
    public AsciiGradientType Type { get; set; } = AsciiGradientType.Horizontal;
    /// <summary>
    /// Gradient start color.
    /// </summary>
    public Rgba32 StartColor { get; set; } = new(0, 255, 255);
    /// <summary>
    /// Gradient end color.
    /// </summary>
    public Rgba32 EndColor { get; set; } = new(255, 0, 255);
    /// <summary>
    /// Horizontal gradient center for radial mode (0-1).
    /// </summary>
    public double CenterX { get; set; } = 0.5;
    /// <summary>
    /// Vertical gradient center for radial mode (0-1).
    /// </summary>
    public double CenterY { get; set; } = 0.5;
}

/// <summary>
/// Palette settings for ANSI coloring.
/// </summary>
public sealed class AsciiPaletteOptions {
    /// <summary>
    /// Palette selection mode.
    /// </summary>
    public AsciiPaletteMode Mode { get; set; } = AsciiPaletteMode.CycleColumns;
    /// <summary>
    /// Palette colors used for dark modules.
    /// </summary>
    public Rgba32[] Colors { get; set; } = Array.Empty<Rgba32>();
    /// <summary>
    /// Seed used for random palette selection.
    /// </summary>
    public int Seed { get; set; } = 0;
}
