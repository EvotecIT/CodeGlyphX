using System;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Rendering options for ASCII matrix output.
/// </summary>
public sealed class MatrixAsciiRenderOptions {
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
}
