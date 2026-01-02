namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Options for <see cref="QrPngRenderer"/>.
/// </summary>
public sealed class QrPngRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single QR module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>
    /// Gets or sets the foreground (dark) color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = Rgba32.Black;

    /// <summary>
    /// Gets or sets the background (light) color.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;
}
