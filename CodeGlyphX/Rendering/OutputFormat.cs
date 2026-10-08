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
    Png = 1,
    /// <summary>
    /// SVG (text).
    /// </summary>
    Svg = 2,
    /// <summary>
    /// SVGZ (compressed SVG).
    /// </summary>
    Svgz = 3,
    /// <summary>
    /// HTML (text).
    /// </summary>
    Html = 4,
    /// <summary>
    /// JPEG image.
    /// </summary>
    Jpeg = 5,
    /// <summary>
    /// WebP image.
    /// </summary>
    Webp = 6,
    /// <summary>
    /// BMP image.
    /// </summary>
    Bmp = 7,
    /// <summary>
    /// PPM image.
    /// </summary>
    Ppm = 8,
    /// <summary>
    /// PBM image.
    /// </summary>
    Pbm = 9,
    /// <summary>
    /// PGM image.
    /// </summary>
    Pgm = 10,
    /// <summary>
    /// PAM image.
    /// </summary>
    Pam = 11,
    /// <summary>
    /// XBM (text).
    /// </summary>
    Xbm = 12,
    /// <summary>
    /// XPM (text).
    /// </summary>
    Xpm = 13,
    /// <summary>
    /// TGA image.
    /// </summary>
    Tga = 14,
    /// <summary>
    /// ICO image.
    /// </summary>
    Ico = 15,
    /// <summary>
    /// PDF document.
    /// </summary>
    Pdf = 16,
    /// <summary>
    /// EPS document (text).
    /// </summary>
    Eps = 17,
    /// <summary>
    /// ASCII art (text).
    /// </summary>
    Ascii = 18,
    /// <summary>
    /// GIF image.
    /// </summary>
    Gif = 19,
    /// <summary>
    /// TIFF image.
    /// </summary>
    Tiff = 20
}
