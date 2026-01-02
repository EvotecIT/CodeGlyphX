namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Options for <see cref="BarcodePngRenderer"/>.
/// </summary>
public sealed class BarcodePngRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 10;

    /// <summary>
    /// Gets or sets the barcode height in modules.
    /// </summary>
    public int HeightModules { get; set; } = 40;

    /// <summary>
    /// Gets or sets the foreground (bar) color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = Rgba32.Black;

    /// <summary>
    /// Gets or sets the background (space) color.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;
}
