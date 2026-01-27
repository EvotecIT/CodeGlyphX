using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Rendering options for ASCII matrix output.
/// </summary>
public sealed partial class MatrixAsciiRenderOptions {
    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Module width in characters.
    /// </summary>
    public int ModuleWidth { get; set; } = 2;

    /// <summary>
    /// Module height in rows.
    /// </summary>
    public int ModuleHeight { get; set; } = 1;

    /// <summary>
    /// Additional scale multiplier applied to both module width and height.
    /// </summary>
    public int Scale { get; set; } = 1;

    /// <summary>
    /// Character(s) used for dark modules.
    /// </summary>
    public string Dark { get; set; } = "#";

    /// <summary>
    /// Character(s) used for light modules.
    /// </summary>
    public string Light { get; set; } = " ";

    /// <summary>
    /// Line separator used between rows.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;

    /// <summary>
    /// When true, prefers Unicode block glyphs (for example, █) when defaults are used.
    /// </summary>
    public bool UseUnicodeBlocks { get; set; }

    /// <summary>
    /// When true, swaps dark and light output.
    /// </summary>
    public bool Invert { get; set; }

    /// <summary>
    /// When true, emits ANSI color escape codes for dark (and optionally light) modules.
    /// </summary>
    public bool UseAnsiColors { get; set; }

    /// <summary>
    /// When true, uses 24-bit ANSI colors; otherwise maps to ANSI 256-color.
    /// </summary>
    public bool UseAnsiTrueColor { get; set; } = true;

    /// <summary>
    /// ANSI color for dark modules.
    /// </summary>
    public Rgba32 AnsiDarkColor { get; set; } = new(0, 0, 0);

    /// <summary>
    /// ANSI color for light modules.
    /// </summary>
    public Rgba32 AnsiLightColor { get; set; } = new(255, 255, 255);

    /// <summary>
    /// When true, also colorizes light modules; otherwise leaves them uncolored.
    /// </summary>
    public bool AnsiColorizeLight { get; set; }
}
