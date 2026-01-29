namespace CodeGlyphX.Rendering;

/// <summary>
/// Supported output formats for rendered barcodes and QR codes.
/// </summary>
public enum OutputFormat {
    /// <summary>
    /// Unknown or unsupported format.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// PNG image.
    /// </summary>
    Png,
    /// <summary>
    /// SVG (text).
    /// </summary>
    Svg,
    /// <summary>
    /// SVGZ (compressed SVG).
    /// </summary>
    Svgz,
    /// <summary>
    /// HTML (text).
    /// </summary>
    Html,
    /// <summary>
    /// JPEG image.
    /// </summary>
    Jpeg,
    /// <summary>
    /// WebP image.
    /// </summary>
    Webp,
    /// <summary>
    /// BMP image.
    /// </summary>
    Bmp,
    /// <summary>
    /// PPM image.
    /// </summary>
    Ppm,
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
    /// XBM (text).
    /// </summary>
    Xbm,
    /// <summary>
    /// XPM (text).
    /// </summary>
    Xpm,
    /// <summary>
    /// TGA image.
    /// </summary>
    Tga,
    /// <summary>
    /// ICO image.
    /// </summary>
    Ico,
    /// <summary>
    /// PDF document.
    /// </summary>
    Pdf,
    /// <summary>
    /// EPS document (text).
    /// </summary>
    Eps,
    /// <summary>
    /// ASCII art (text).
    /// </summary>
    Ascii
}
