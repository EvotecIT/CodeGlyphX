namespace CodeGlyphX.Rendering;

/// <summary>
/// Known image formats supported by <see cref="ImageReader"/>.
/// </summary>
public enum ImageFormat {
    /// <summary>
    /// Format could not be detected.
    /// </summary>
    Unknown,
    /// <summary>
    /// PNG image.
    /// </summary>
    Png,
    /// <summary>
    /// JPEG image.
    /// </summary>
    Jpeg,
    /// <summary>
    /// GIF image.
    /// </summary>
    Gif,
    /// <summary>
    /// BMP image.
    /// </summary>
    Bmp,
    /// <summary>
    /// PBM image.
    /// </summary>
    Pbm,
    /// <summary>
    /// PGM image.
    /// </summary>
    Pgm,
    /// <summary>
    /// PAM image.
    /// </summary>
    Pam,
    /// <summary>
    /// PPM image.
    /// </summary>
    Ppm,
    /// <summary>
    /// TGA image.
    /// </summary>
    Tga,
    /// <summary>
    /// TIFF image.
    /// </summary>
    Tiff,
    /// <summary>
    /// XPM image.
    /// </summary>
    Xpm,
    /// <summary>
    /// XBM image.
    /// </summary>
    Xbm,
    /// <summary>
    /// ICO image (embedded PNG/BMP).
    /// </summary>
    Ico,
    /// <summary>
    /// WebP image (managed VP8/VP8L stills; first-frame decode for animations).
    /// </summary>
    Webp,
}
