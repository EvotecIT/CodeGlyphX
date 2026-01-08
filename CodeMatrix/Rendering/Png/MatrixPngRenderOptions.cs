namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Options for rendering generic 2D matrices to PNG.
/// </summary>
public sealed class MatrixPngRenderOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 6;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = Rgba32.Black;

    /// <summary>
    /// Background color.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;
}
