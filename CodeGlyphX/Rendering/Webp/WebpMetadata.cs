namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Optional WebP metadata chunks (ICCP/EXIF/XMP).
/// </summary>
public readonly struct WebpMetadata {
    /// <summary>
    /// ICC profile payload.
    /// </summary>
    public byte[]? Icc { get; }

    /// <summary>
    /// EXIF payload.
    /// </summary>
    public byte[]? Exif { get; }

    /// <summary>
    /// XMP payload.
    /// </summary>
    public byte[]? Xmp { get; }

    /// <summary>
    /// Indicates whether any metadata is present.
    /// </summary>
    public bool HasData => (Icc is { Length: > 0 }) || (Exif is { Length: > 0 }) || (Xmp is { Length: > 0 });

    /// <summary>
    /// Creates metadata with optional ICC/EXIF/XMP payloads.
    /// </summary>
    public WebpMetadata(byte[]? icc = null, byte[]? exif = null, byte[]? xmp = null) {
        Icc = icc;
        Exif = exif;
        Xmp = xmp;
    }
}
