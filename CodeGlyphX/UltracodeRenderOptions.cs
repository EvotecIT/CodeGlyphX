using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>Pixel layout and structural paints for an Ultracode symbol.</summary>
public sealed class UltracodeRenderOptions {
    /// <summary>Gets or sets the square cell size in pixels. The default is six.</summary>
    public int CellSize { get; set; } = 6;
    /// <summary>Gets or sets the quiet-zone width in cells. At least one cell is required.</summary>
    public int QuietZone { get; set; } = 1;
    /// <summary>Gets or sets the structural ink. Data colours remain cyan, magenta, yellow and green.</summary>
    public Rgba32 Foreground { get; set; } = Rgba32.Black;
    /// <summary>Gets or sets the background and quiet-zone paint.</summary>
    public Rgba32 Background { get; set; } = Rgba32.White;
}
