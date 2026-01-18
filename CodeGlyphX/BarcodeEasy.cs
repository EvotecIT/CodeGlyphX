using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX;

/// <summary>
/// One-line barcode helpers with sane defaults.
/// </summary>
public static class BarcodeEasy {
    /// <summary>
    /// Encodes a barcode value using the specified <see cref="BarcodeType"/>.
    /// </summary>
    public static Barcode1D Encode(BarcodeType type, string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        return BarcodeEncoder.Encode(type, content);
    }

    /// <summary>
    /// Renders a barcode as PNG.
    /// </summary>
    public static byte[] RenderPng(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Png(type, content, options);

    /// <summary>
    /// Renders a barcode as SVG.
    /// </summary>
    public static string RenderSvg(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Svg(type, content, options);

    /// <summary>
    /// Renders a barcode as HTML.
    /// </summary>
    public static string RenderHtml(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Html(type, content, options);

    /// <summary>
    /// Renders a barcode as JPEG.
    /// </summary>
    public static byte[] RenderJpeg(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Jpeg(type, content, options);

    /// <summary>
    /// Renders a barcode as BMP.
    /// </summary>
    public static byte[] RenderBmp(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Bmp(type, content, options);

    /// <summary>
    /// Renders a barcode as PPM.
    /// </summary>
    public static byte[] RenderPpm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Ppm(type, content, options);

    /// <summary>
    /// Renders a barcode as PBM.
    /// </summary>
    public static byte[] RenderPbm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Pbm(type, content, options);

    /// <summary>
    /// Renders a barcode as PGM.
    /// </summary>
    public static byte[] RenderPgm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Pgm(type, content, options);

    /// <summary>
    /// Renders a barcode as PAM.
    /// </summary>
    public static byte[] RenderPam(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Pam(type, content, options);

    /// <summary>
    /// Renders a barcode as XBM.
    /// </summary>
    public static string RenderXbm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Xbm(type, content, options);

    /// <summary>
    /// Renders a barcode as XPM.
    /// </summary>
    public static string RenderXpm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Xpm(type, content, options);

    /// <summary>
    /// Renders a barcode as TGA.
    /// </summary>
    public static byte[] RenderTga(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Tga(type, content, options);

    /// <summary>
    /// Renders a barcode as ICO.
    /// </summary>
    public static byte[] RenderIco(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Ico(type, content, options);

    /// <summary>
    /// Renders a barcode as SVGZ.
    /// </summary>
    public static byte[] RenderSvgz(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Barcode.Svgz(type, content, options);

    /// <summary>
    /// Renders a barcode as PDF.
    /// </summary>
    public static byte[] RenderPdf(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.Pdf(type, content, options, mode);

    /// <summary>
    /// Renders a barcode as EPS.
    /// </summary>
    public static string RenderEps(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.Eps(type, content, options, mode);

    /// <summary>
    /// Renders a barcode as ASCII text.
    /// </summary>
    public static string RenderAscii(BarcodeType type, string content, BarcodeAsciiRenderOptions? options = null) =>
        Barcode.Ascii(type, content, options);

    /// <summary>
    /// Renders a barcode as PNG to a stream.
    /// </summary>
    public static void RenderPngToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SavePng(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as SVG to a stream.
    /// </summary>
    public static void RenderSvgToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveSvg(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as HTML to a stream.
    /// </summary>
    public static void RenderHtmlToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, string? title = null) =>
        Barcode.SaveHtml(type, content, stream, options, title);

    /// <summary>
    /// Renders a barcode as JPEG to a stream.
    /// </summary>
    public static void RenderJpegToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveJpeg(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as BMP to a stream.
    /// </summary>
    public static void RenderBmpToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveBmp(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as PPM to a stream.
    /// </summary>
    public static void RenderPpmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SavePpm(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as PBM to a stream.
    /// </summary>
    public static void RenderPbmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SavePbm(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as PGM to a stream.
    /// </summary>
    public static void RenderPgmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SavePgm(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as PAM to a stream.
    /// </summary>
    public static void RenderPamToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SavePam(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as XBM to a stream.
    /// </summary>
    public static void RenderXbmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveXbm(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as XPM to a stream.
    /// </summary>
    public static void RenderXpmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveXpm(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as TGA to a stream.
    /// </summary>
    public static void RenderTgaToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveTga(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as ICO to a stream.
    /// </summary>
    public static void RenderIcoToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveIco(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as SVGZ to a stream.
    /// </summary>
    public static void RenderSvgzToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        Barcode.SaveSvgz(type, content, stream, options);

    /// <summary>
    /// Renders a barcode as PDF to a stream.
    /// </summary>
    public static void RenderPdfToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.SavePdf(type, content, stream, options, mode);

    /// <summary>
    /// Renders a barcode as EPS to a stream.
    /// </summary>
    public static void RenderEpsToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.SaveEps(type, content, stream, options, mode);

    /// <summary>
    /// Renders a barcode as PNG to a file.
    /// </summary>
    public static string RenderPngToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SavePng(type, content, path, options);

    /// <summary>
    /// Renders a barcode as SVG to a file.
    /// </summary>
    public static string RenderSvgToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveSvg(type, content, path, options);

    /// <summary>
    /// Renders a barcode as HTML to a file.
    /// </summary>
    public static string RenderHtmlToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) =>
        Barcode.SaveHtml(type, content, path, options, title);

    /// <summary>
    /// Renders a barcode as JPEG to a file.
    /// </summary>
    public static string RenderJpegToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveJpeg(type, content, path, options);

    /// <summary>
    /// Renders a barcode as BMP to a file.
    /// </summary>
    public static string RenderBmpToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveBmp(type, content, path, options);

    /// <summary>
    /// Renders a barcode as PPM to a file.
    /// </summary>
    public static string RenderPpmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SavePpm(type, content, path, options);

    /// <summary>
    /// Renders a barcode as PBM to a file.
    /// </summary>
    public static string RenderPbmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SavePbm(type, content, path, options);

    /// <summary>
    /// Renders a barcode as PGM to a file.
    /// </summary>
    public static string RenderPgmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SavePgm(type, content, path, options);

    /// <summary>
    /// Renders a barcode as PAM to a file.
    /// </summary>
    public static string RenderPamToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SavePam(type, content, path, options);

    /// <summary>
    /// Renders a barcode as XBM to a file.
    /// </summary>
    public static string RenderXbmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveXbm(type, content, path, options);

    /// <summary>
    /// Renders a barcode as XPM to a file.
    /// </summary>
    public static string RenderXpmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveXpm(type, content, path, options);

    /// <summary>
    /// Renders a barcode as TGA to a file.
    /// </summary>
    public static string RenderTgaToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveTga(type, content, path, options);

    /// <summary>
    /// Renders a barcode as ICO to a file.
    /// </summary>
    public static string RenderIcoToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveIco(type, content, path, options);

    /// <summary>
    /// Renders a barcode as SVGZ to a file.
    /// </summary>
    public static string RenderSvgzToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        Barcode.SaveSvgz(type, content, path, options);

    /// <summary>
    /// Renders a barcode as PDF to a file.
    /// </summary>
    public static string RenderPdfToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.SavePdf(type, content, path, options, mode);

    /// <summary>
    /// Renders a barcode as EPS to a file.
    /// </summary>
    public static string RenderEpsToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Barcode.SaveEps(type, content, path, options, mode);

    /// <summary>
    /// Renders a barcode as ASCII text to a file.
    /// </summary>
    public static string RenderAsciiToFile(BarcodeType type, string content, string path, BarcodeAsciiRenderOptions? options = null) =>
        Barcode.SaveAscii(type, content, path, options);

    /// <summary>
    /// Saves a barcode to a file based on the output extension.
    /// </summary>
    public static string RenderToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) =>
        Barcode.Save(type, content, path, options, title);
}
