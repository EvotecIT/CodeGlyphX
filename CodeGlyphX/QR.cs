using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simple QR helpers with fluent and static APIs.
/// </summary>
/// <remarks>
/// Use <see cref="Save(string,string,CodeGlyphX.QrEasyOptions,string)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// QR.Save("https://example.com", "qr.png");
/// </code>
/// </example>
public static partial class QR {
    /// <summary>
    /// Starts a fluent QR builder for plain text.
    /// </summary>
    public static QrBuilder Create(string payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Starts a fluent QR builder for a payload with embedded defaults.
    /// </summary>
    public static QrBuilder Create(QrPayloadData payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Encodes a payload into a QR code.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Detects a payload type and encodes it into a QR code.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.EncodeAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults into a QR code.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] Png(string payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PNG.
    /// </summary>
    public static byte[] PngAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPngAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PNG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Png(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string Svg(string payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as SVG.
    /// </summary>
    public static string SvgAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderSvgAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as SVG for a payload with embedded defaults.
    /// </summary>
    public static string Svg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Renders a QR code as HTML.
    /// </summary>
    public static string Html(string payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as HTML.
    /// </summary>
    public static string HtmlAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderHtmlAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as HTML for a payload with embedded defaults.
    /// </summary>
    public static string Html(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Renders a QR code as JPEG.
    /// </summary>
    public static byte[] Jpeg(string payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as JPEG.
    /// </summary>
    public static byte[] JpegAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderJpegAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as JPEG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Jpeg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    public static byte[] Bmp(string payload, QrEasyOptions? options = null) => QrEasy.RenderBmp(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as BMP.
    /// </summary>
    public static byte[] BmpAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderBmpAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as BMP for a payload with embedded defaults.
    /// </summary>
    public static byte[] Bmp(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderBmp(payload, options);

    /// <summary>
    /// Renders a QR code as PPM.
    /// </summary>
    public static byte[] Ppm(string payload, QrEasyOptions? options = null) => QrEasy.RenderPpm(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PPM.
    /// </summary>
    public static byte[] PpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPpmAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PPM for a payload with embedded defaults.
    /// </summary>
    public static byte[] Ppm(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPpm(payload, options);

    /// <summary>
    /// Renders a QR code as PBM.
    /// </summary>
    public static byte[] Pbm(string payload, QrEasyOptions? options = null) => QrEasy.RenderPbm(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PBM.
    /// </summary>
    public static byte[] PbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPbmAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PBM for a payload with embedded defaults.
    /// </summary>
    public static byte[] Pbm(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPbm(payload, options);

    /// <summary>
    /// Renders a QR code as PGM.
    /// </summary>
    public static byte[] Pgm(string payload, QrEasyOptions? options = null) => QrEasy.RenderPgm(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PGM.
    /// </summary>
    public static byte[] PgmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPgmAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PGM for a payload with embedded defaults.
    /// </summary>
    public static byte[] Pgm(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPgm(payload, options);

    /// <summary>
    /// Renders a QR code as PAM.
    /// </summary>
    public static byte[] Pam(string payload, QrEasyOptions? options = null) => QrEasy.RenderPam(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PAM.
    /// </summary>
    public static byte[] PamAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPamAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PAM for a payload with embedded defaults.
    /// </summary>
    public static byte[] Pam(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPam(payload, options);

    /// <summary>
    /// Renders a QR code as XBM.
    /// </summary>
    public static string Xbm(string payload, QrEasyOptions? options = null) => QrEasy.RenderXbm(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as XBM.
    /// </summary>
    public static string XbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderXbmAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as XBM for a payload with embedded defaults.
    /// </summary>
    public static string Xbm(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderXbm(payload, options);

    /// <summary>
    /// Renders a QR code as XPM.
    /// </summary>
    public static string Xpm(string payload, QrEasyOptions? options = null) => QrEasy.RenderXpm(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as XPM.
    /// </summary>
    public static string XpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderXpmAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as XPM for a payload with embedded defaults.
    /// </summary>
    public static string Xpm(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderXpm(payload, options);

    /// <summary>
    /// Renders a QR code as TGA.
    /// </summary>
    public static byte[] Tga(string payload, QrEasyOptions? options = null) => QrEasy.RenderTga(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as TGA.
    /// </summary>
    public static byte[] TgaAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderTgaAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as TGA for a payload with embedded defaults.
    /// </summary>
    public static byte[] Tga(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderTga(payload, options);

    /// <summary>
    /// Renders a QR code as ICO.
    /// </summary>
    public static byte[] Ico(string payload, QrEasyOptions? options = null) => QrEasy.RenderIco(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as ICO.
    /// </summary>
    public static byte[] IcoAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderIcoAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as ICO for a payload with embedded defaults.
    /// </summary>
    public static byte[] Ico(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderIco(payload, options);

    /// <summary>
    /// Renders a QR code as SVGZ.
    /// </summary>
    public static byte[] Svgz(string payload, QrEasyOptions? options = null) => QrEasy.RenderSvgz(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as SVGZ.
    /// </summary>
    public static byte[] SvgzAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderSvgzAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as SVGZ for a payload with embedded defaults.
    /// </summary>
    public static byte[] Svgz(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderSvgz(payload, options);

    /// <summary>
    /// Renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] Pdf(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) => QrEasy.RenderPdf(payload, options, mode);

    /// <summary>
    /// Detects a payload type and renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] PdfAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return QrEasy.RenderPdfAuto(payload, detectOptions, options, mode);
    }

    /// <summary>
    /// Renders a QR code as PDF for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] Pdf(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) => QrEasy.RenderPdf(payload, options, mode);

    /// <summary>
    /// Renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string Eps(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) => QrEasy.RenderEps(payload, options, mode);

    /// <summary>
    /// Detects a payload type and renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string EpsAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return QrEasy.RenderEpsAuto(payload, detectOptions, options, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string Eps(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) => QrEasy.RenderEps(payload, options, mode);

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    public static string Ascii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAscii(payload, asciiOptions, options);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as ASCII text.
    /// </summary>
    public static string AsciiAuto(string payload, QrPayloadDetectOptions? detectOptions = null, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAsciiAuto(payload, detectOptions, asciiOptions, options);
    }

    /// <summary>
    /// Renders a QR code as ASCII text for a payload with embedded defaults.
    /// </summary>
    public static string Ascii(QrPayloadData payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAscii(payload, asciiOptions, options);
    }
}

