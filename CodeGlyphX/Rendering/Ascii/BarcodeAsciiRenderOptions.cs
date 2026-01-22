using System;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Rendering options for ASCII 1D barcodes.
/// </summary>
public sealed partial class BarcodeAsciiRenderOptions {
    /// <summary>
    /// Module width in characters.
    /// </summary>
    public int ModuleWidth { get; set; } = 1;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.BarcodeQuietZone;

    /// <summary>
    /// Height in text rows.
    /// </summary>
    public int Height { get; set; } = RenderDefaults.BarcodeHeightModules;

    /// <summary>
    /// Character(s) used for bars.
    /// </summary>
    public string Dark { get; set; } = "#";

    /// <summary>
    /// Character(s) used for spaces.
    /// </summary>
    public string Light { get; set; } = " ";

    /// <summary>
    /// Line separator used between rows.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;
}
