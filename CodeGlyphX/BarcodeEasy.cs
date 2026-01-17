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
