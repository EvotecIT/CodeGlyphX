namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Optional JPEG metadata payloads (EXIF/XMP/ICC).
/// </summary>
public readonly struct JpegMetadata {
    /// <summary>
    /// EXIF payload (TIFF data, optional "Exif\0\0" header).
    /// </summary>
    public byte[]? Exif { get; }

    /// <summary>
    /// XMP payload (RDF/XML, optional XMP namespace header).
    /// </summary>
    public byte[]? Xmp { get; }

    /// <summary>
    /// ICC profile payload.
    /// </summary>
    public byte[]? Icc { get; }

    /// <summary>
    /// Indicates whether any metadata is present.
    /// </summary>
    public bool HasData => (Exif is { Length: > 0 }) || (Xmp is { Length: > 0 }) || (Icc is { Length: > 0 });

    /// <summary>
    /// Creates metadata with optional EXIF/XMP/ICC payloads.
    /// </summary>
    public JpegMetadata(byte[]? exif = null, byte[]? xmp = null, byte[]? icc = null) {
        Exif = exif;
        Xmp = xmp;
        Icc = icc;
    }
}
