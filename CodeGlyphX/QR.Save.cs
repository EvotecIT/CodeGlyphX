using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QR {
    /// <summary>
    /// Saves a PNG QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePng(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a PNG QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePng(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a PNG QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePng(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a PNG QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePng(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves an SVG QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveSvg(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves an SVG QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveSvg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves an SVG QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvg(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves an SVG QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves an HTML QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveHtml(string payload, string path, QrEasyOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return OutputWriter.Write(path, Render(payload, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves an HTML QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveHtml(QrPayloadData payload, string path, QrEasyOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return OutputWriter.Write(path, Render(payload, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves an HTML QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveHtml(string payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        OutputWriter.Write(stream, Render(payload, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves an HTML QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveHtml(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        OutputWriter.Write(stream, Render(payload, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves a JPEG QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveJpeg(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a JPEG QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveJpeg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a JPEG QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveJpeg(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a JPEG QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveJpeg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a WebP QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveWebp(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a WebP QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveWebp(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a WebP QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveWebp(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a WebP QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveWebp(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a BMP QR to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveBmp(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Bmp, options));
    }

    /// <summary>
    /// Saves a BMP QR to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveBmp(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Bmp, options));
    }

    /// <summary>
    /// Saves a BMP QR to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveBmp(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Bmp, options));
    }

    /// <summary>
    /// Saves a BMP QR to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveBmp(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Bmp, options));
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePpm(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Ppm, options));
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePpm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Ppm, options));
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePpm(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Ppm, options));
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePpm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Ppm, options));
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePbm(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pbm, options));
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePbm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pbm, options));
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePbm(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pbm, options));
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePbm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pbm, options));
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePgm(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pgm, options));
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePgm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pgm, options));
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePgm(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pgm, options));
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePgm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pgm, options));
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePam(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pam, options));
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePam(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pam, options));
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePam(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pam, options));
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePam(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pam, options));
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXbm(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Xbm, options));
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXbm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Xbm, options));
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXbm(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Xbm, options));
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXbm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Xbm, options));
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXpm(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Xpm, options));
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXpm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Xpm, options));
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXpm(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Xpm, options));
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXpm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Xpm, options));
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveTga(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Tga, options));
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveTga(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Tga, options));
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveTga(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Tga, options));
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveTga(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Tga, options));
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveIco(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Ico, options));
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveIco(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Ico, options));
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveIco(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Ico, options));
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveIco(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Ico, options));
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveSvgz(string payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Svgz, options));
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveSvgz(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Svgz, options));
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvgz(string payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Svgz, options));
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvgz(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Svgz, options));
    }

    /// <summary>
    /// Saves a PDF QR to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePdf(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves a PDF QR to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePdf(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves a PDF QR to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePdf(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves a PDF QR to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePdf(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves an EPS QR to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveEps(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves an EPS QR to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveEps(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves an EPS QR to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveEps(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves an EPS QR to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveEps(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.Render(payload, format, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Detects a payload type and saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveAuto(string payload, string path, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.RenderAuto(payload, format, detectOptions, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.Render(payload, format, options, extras);
        return OutputWriter.Write(path, output);
    }
}
