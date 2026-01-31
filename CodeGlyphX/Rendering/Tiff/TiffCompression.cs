namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// TIFF compression options supported by the writer.
/// </summary>
public enum TiffCompression {
    /// <summary>
    /// No compression.
    /// </summary>
    None = 1,
    /// <summary>
    /// LZW compression.
    /// </summary>
    Lzw = 5,
    /// <summary>
    /// Deflate compression (zlib).
    /// </summary>
    Deflate = 8,
    /// <summary>
    /// PackBits compression.
    /// </summary>
    PackBits = 32773
}
