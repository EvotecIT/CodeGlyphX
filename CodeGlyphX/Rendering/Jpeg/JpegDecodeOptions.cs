namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// JPEG decoding options.
/// </summary>
/// <example>
/// <code>
/// var options = new JpegDecodeOptions(highQualityChroma: true, allowTruncated: true);
/// var rgba = JpegReader.DecodeRgba32(data, out var width, out var height, options);
/// </code>
/// </example>
public readonly struct JpegDecodeOptions {
    /// <summary>
    /// Enables higher-quality chroma upsampling when components are subsampled.
    /// </summary>
    public bool HighQualityChroma { get; }

    /// <summary>
    /// Allows truncated scan data (best-effort decode).
    /// </summary>
    public bool AllowTruncated { get; }

    /// <summary>
    /// Creates JPEG decode options.
    /// </summary>
    public JpegDecodeOptions(bool highQualityChroma = false, bool allowTruncated = false) {
        HighQualityChroma = highQualityChroma;
        AllowTruncated = allowTruncated;
    }
}
