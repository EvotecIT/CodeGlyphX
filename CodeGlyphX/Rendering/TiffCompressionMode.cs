namespace CodeGlyphX.Rendering;

/// <summary>
/// TIFF compression selection for writers.
/// </summary>
public enum TiffCompressionMode {
    /// <summary>
    /// Picks the smallest output (current default).
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Writes uncompressed strips.
    /// </summary>
    None,
    /// <summary>
    /// Forces PackBits compression.
    /// </summary>
    PackBits,
    /// <summary>
    /// Forces Deflate compression (zlib).
    /// </summary>
    Deflate,
    /// <summary>
    /// Forces LZW compression.
    /// </summary>
    Lzw
}
