namespace CodeGlyphX.Rendering;

/// <summary>
/// Output mode for vector-capable formats.
/// </summary>
/// <remarks>
/// Vector output is the default for PDF/EPS. Raster mode uses pixel rendering and is recommended
/// when gradients, logos, or bitmap-only effects are needed.
/// </remarks>
public enum RenderMode {
    /// <summary>
    /// Vector output (default).
    /// </summary>
    Vector = 0,
    /// <summary>
    /// Raster output.
    /// </summary>
    Raster = 1
}
