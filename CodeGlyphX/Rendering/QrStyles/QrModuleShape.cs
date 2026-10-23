namespace CodeGlyphX.Rendering;

/// <summary>
/// QR module shape shared by supported raster and vector renderers.
/// </summary>
public enum QrModuleShape {
    /// <summary>
    /// Full square modules.
    /// </summary>
    Square,
    /// <summary>
    /// Circular (dot) modules.
    /// </summary>
    Circle,
    /// <summary>
    /// Rounded-corner square modules.
    /// </summary>
    Rounded,
    /// <summary>
    /// Rounded modules that connect to adjacent dark modules.
    /// </summary>
    ConnectedRounded,
    /// <summary>
    /// Squircle modules that connect to adjacent dark modules.
    /// </summary>
    ConnectedSquircle,
    /// <summary>
    /// Diamond-shaped modules.
    /// </summary>
    Diamond,
    /// <summary>
    /// Soft diamond (rounded diamond) modules.
    /// </summary>
    SoftDiamond,
    /// <summary>
    /// Squircle (superellipse) modules.
    /// </summary>
    Squircle,
    /// <summary>
    /// Leaf (lens) modules.
    /// </summary>
    Leaf,
    /// <summary>
    /// Wavy circle modules.
    /// </summary>
    Wave,
    /// <summary>
    /// Organic blob modules.
    /// </summary>
    Blob,
    /// <summary>
    /// Dot modules (small circles inside the cell).
    /// </summary>
    Dot,
    /// <summary>
    /// Dot grid modules (four small dots per cell). Decorative – verify before print;
    /// excluded from scan-safe presets because the cell centre is light.
    /// </summary>
    DotGrid,
    /// <summary>Joined ink strokes with rounded outer corners and concave inner-corner fills.</summary>
    Liquid,
    /// <summary>Square modules with the top-left diagonal corner rounded.</summary>
    Classy,
    /// <summary>Modules with half-cell corner radii.</summary>
    ExtraRounded,
    /// <summary>Vertical capsules joining dark neighbours above and below.</summary>
    BarsVertical,
    /// <summary>Horizontal capsules joining dark neighbours to either side.</summary>
    BarsHorizontal,
    /// <summary>Plus symbols. Decorative – verify before print; excluded from scan-safe presets.</summary>
    Plus,
    /// <summary>Star symbols. Decorative – verify before print; excluded from scan-safe presets.</summary>
    Star,
    /// <summary>Heart symbols. Decorative – verify before print; excluded from scan-safe presets.</summary>
    Heart,
    /// <summary>Squares with diagonally cut corners, also suitable for whole finder rings.</summary>
    CutCorner,
}
