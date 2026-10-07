namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// JPEG encoding options.
/// </summary>
public sealed class JpegEncodeOptions {
    /// <summary>
    /// JPEG quality (1..100).
    /// </summary>
    public int Quality { get; set; } = 85;

    /// <summary>
    /// Chroma subsampling mode.
    /// </summary>
    public JpegSubsampling Subsampling { get; set; } = JpegSubsampling.Y444;

    /// <summary>
    /// Enables progressive JPEG encoding.
    /// </summary>
    public bool Progressive { get; set; }

    /// <summary>
    /// Enables optimized Huffman tables.
    /// </summary>
    public bool OptimizeHuffman { get; set; }

    /// <summary>
    /// Optional metadata segments (EXIF/XMP/ICC).
    /// </summary>
    public JpegMetadata Metadata { get; set; } = default;

    /// <summary>
    /// Writes a JFIF APP0 header when true.
    /// </summary>
    public bool WriteJfifHeader { get; set; } = true;

    /// <summary>Copies encoding settings and metadata payloads for independent ownership.</summary>
    internal JpegEncodeOptions Clone() {
        var copy = (JpegEncodeOptions)MemberwiseClone();
        copy.Metadata = new JpegMetadata(
            Metadata.Exif is null ? null : (byte[])Metadata.Exif.Clone(),
            Metadata.Xmp is null ? null : (byte[])Metadata.Xmp.Clone(),
            Metadata.Icc is null ? null : (byte[])Metadata.Icc.Clone());
        return copy;
    }
}
