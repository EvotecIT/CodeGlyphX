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
/// Use <see cref="Save(string,string,CodeGlyphX.QrEasyOptions,CodeGlyphX.Rendering.RenderExtras)"/> to pick the output format by file extension.
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

    private static RenderedOutput Render(string payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.Render(payload, format, options, extras);
    }

    private static RenderedOutput RenderAuto(string payload, OutputFormat format, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.RenderAuto(payload, format, detectOptions, options, extras);
    }

    private static RenderedOutput Render(QrPayloadData payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.Render(payload, format, options, extras);
    }

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Png(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Png, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as PNG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PngAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Png, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as PNG for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Png(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Png, options).Data;

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Svg(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Svg, options).GetText();

    /// <summary>
    /// Detects a payload type and renders a QR code as SVG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SvgAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Svg, detectOptions, options).GetText();
    }

    /// <summary>
    /// Renders a QR code as SVG for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Svg(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Svg, options).GetText();

    /// <summary>
    /// Renders a QR code as HTML.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Html(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Html, options).GetText();

    /// <summary>
    /// Detects a payload type and renders a QR code as HTML.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string HtmlAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Html, detectOptions, options).GetText();
    }

    /// <summary>
    /// Renders a QR code as HTML for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Html(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Html, options).GetText();

    /// <summary>
    /// Renders a QR code as JPEG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Jpeg(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Jpeg, options).Data;

    /// <summary>
    /// Renders a QR code as WebP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Webp(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Webp, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as JPEG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] JpegAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Jpeg, detectOptions, options).Data;
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as WebP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] WebpAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Webp, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as JPEG for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Jpeg(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Jpeg, options).Data;

    /// <summary>
    /// Renders a QR code as WebP for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Webp(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Webp, options).Data;

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Bmp(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Bmp, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as BMP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] BmpAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Bmp, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as BMP for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Bmp(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Bmp, options).Data;

    /// <summary>
    /// Renders a QR code as PPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ppm(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Ppm, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as PPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Ppm, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as PPM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ppm(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Ppm, options).Data;

    /// <summary>
    /// Renders a QR code as PBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pbm(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pbm, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as PBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Pbm, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as PBM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pbm(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pbm, options).Data;

    /// <summary>
    /// Renders a QR code as PGM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pgm(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pgm, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as PGM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PgmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Pgm, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as PGM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pgm(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pgm, options).Data;

    /// <summary>
    /// Renders a QR code as PAM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pam(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pam, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as PAM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PamAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Pam, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as PAM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pam(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Pam, options).Data;

    /// <summary>
    /// Renders a QR code as XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xbm(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Xbm, options).GetText();

    /// <summary>
    /// Detects a payload type and renders a QR code as XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string XbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Xbm, detectOptions, options).GetText();
    }

    /// <summary>
    /// Renders a QR code as XBM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xbm(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Xbm, options).GetText();

    /// <summary>
    /// Renders a QR code as XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xpm(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Xpm, options).GetText();

    /// <summary>
    /// Detects a payload type and renders a QR code as XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string XpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Xpm, detectOptions, options).GetText();
    }

    /// <summary>
    /// Renders a QR code as XPM for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xpm(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Xpm, options).GetText();

    /// <summary>
    /// Renders a QR code as TGA.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Tga(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Tga, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as TGA.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] TgaAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Tga, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as TGA for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Tga(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Tga, options).Data;

    /// <summary>
    /// Renders a QR code as ICO.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ico(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Ico, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as ICO.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] IcoAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Ico, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as ICO for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ico(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Ico, options).Data;

    /// <summary>
    /// Renders a QR code as SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Svgz(string payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Svgz, options).Data;

    /// <summary>
    /// Detects a payload type and renders a QR code as SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] SvgzAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Svgz, detectOptions, options).Data;
    }

    /// <summary>
    /// Renders a QR code as SVGZ for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Svgz(QrPayloadData payload, QrEasyOptions? options = null) => Render(payload, OutputFormat.Svgz, options).Data;

    /// <summary>
    /// Renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pdf(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }).Data;
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PdfAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return RenderAuto(payload, OutputFormat.Pdf, detectOptions, options, new RenderExtras { VectorMode = mode }).Data;
    }

    /// <summary>
    /// Renders a QR code as PDF for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pdf(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Render(payload, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }).Data;
    }

    /// <summary>
    /// Renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Eps(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }).GetText();
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string EpsAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return RenderAuto(payload, OutputFormat.Eps, detectOptions, options, new RenderExtras { VectorMode = mode }).GetText();
    }

    /// <summary>
    /// Renders a QR code as EPS for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Eps(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Render(payload, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }).GetText();
    }

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Ascii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { MatrixAscii = asciiOptions }).GetText();
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII text with auto-fit.
    /// </summary>
    public static string AsciiConsole(string payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as ASCII text.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string AsciiAuto(string payload, QrPayloadDetectOptions? detectOptions = null, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Ascii, detectOptions, options, new RenderExtras { MatrixAscii = asciiOptions }).GetText();
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as console-friendly ASCII text with auto-fit.
    /// </summary>
    public static string AsciiConsoleAuto(string payload, QrPayloadDetectOptions? detectOptions = null, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Ascii, detectOptions, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }

    /// <summary>
    /// Renders a QR code as ASCII text for a payload with embedded defaults.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Ascii(QrPayloadData payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { MatrixAscii = asciiOptions }).GetText();
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII text with auto-fit for a payload with embedded defaults.
    /// </summary>
    public static string AsciiConsole(QrPayloadData payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }
}
