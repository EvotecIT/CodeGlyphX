namespace CodeGlyphX;

/// <summary>
/// Supported raw pixel formats for symbol scanning.
/// </summary>
/// <remarks>
/// <see cref="SymbolScanner"/> accepts every value. Older direct decoder entry points continue to accept
/// only <see cref="Bgra32"/> and <see cref="Rgba32"/> unless their documentation states otherwise.
/// </remarks>
public enum PixelFormat {
    /// <summary>
    /// 4 bytes per pixel: B, G, R, A.
    /// </summary>
    Bgra32,
    /// <summary>
    /// 4 bytes per pixel: R, G, B, A.
    /// </summary>
    Rgba32,
    /// <summary>
    /// 1 byte per pixel: grayscale intensity.
    /// </summary>
    Gray8,
    /// <summary>
    /// 2 bytes per pixel: little-endian 16-bit grayscale intensity.
    /// </summary>
    Gray16LittleEndian,
    /// <summary>
    /// 3 bytes per pixel: R, G, B.
    /// </summary>
    Rgb24,
    /// <summary>
    /// 3 bytes per pixel: B, G, R.
    /// </summary>
    Bgr24,
    /// <summary>
    /// 4 bytes per pixel: A, R, G, B.
    /// </summary>
    Argb32,
    /// <summary>
    /// 4 bytes per pixel: A, B, G, R.
    /// </summary>
    Abgr32,
    /// <summary>
    /// 2 bytes per pixel: little-endian RGB 5:6:5.
    /// </summary>
    Rgb565LittleEndian,
}
