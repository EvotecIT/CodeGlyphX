using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX;

/// <summary>
/// One-line barcode helpers with sane defaults.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// var png = BarcodeEasy.RenderPng(BarcodeType.Code128, "PRODUCT-12345");
/// var svg = BarcodeEasy.RenderSvg(BarcodeType.Code128, "PRODUCT-12345");
/// </code>
/// </example>
public static class BarcodeEasy {
    /// <summary>
    /// Encodes a barcode value using the specified <see cref="BarcodeType"/>.
    /// </summary>
    public static Barcode1D Encode(BarcodeType type, string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        return BarcodeEncoder.Encode(type, content);
    }

    private static RenderedOutput Render(BarcodeType type, string content, OutputFormat format, BarcodeOptions? options = null, RenderExtras? extras = null) {
        return Barcode.Render(type, content, format, options, extras);
    }

    /// <summary>
    /// Renders a barcode as PNG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPng(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Png, options).Data;

    /// <summary>
    /// Renders a barcode as SVG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderSvg(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Svg, options).GetText();

    /// <summary>
    /// Renders a barcode as HTML.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderHtml(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Html, options).GetText();

    /// <summary>
    /// Renders a barcode as JPEG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderJpeg(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Jpeg, options).Data;

    /// <summary>
    /// Renders a barcode as WebP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderWebp(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Webp, options).Data;

    /// <summary>
    /// Renders a barcode as BMP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderBmp(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Bmp, options).Data;

    /// <summary>
    /// Renders a barcode as PPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPpm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Ppm, options).Data;

    /// <summary>
    /// Renders a barcode as PBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPbm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Pbm, options).Data;

    /// <summary>
    /// Renders a barcode as PGM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPgm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Pgm, options).Data;

    /// <summary>
    /// Renders a barcode as PAM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPam(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Pam, options).Data;

    /// <summary>
    /// Renders a barcode as XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderXbm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Xbm, options).GetText();

    /// <summary>
    /// Renders a barcode as XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderXpm(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Xpm, options).GetText();

    /// <summary>
    /// Renders a barcode as TGA.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderTga(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Tga, options).Data;

    /// <summary>
    /// Renders a barcode as ICO.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderIco(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Ico, options).Data;

    /// <summary>
    /// Renders a barcode as SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderSvgz(BarcodeType type, string content, BarcodeOptions? options = null) =>
        Render(type, content, OutputFormat.Svgz, options).Data;

    /// <summary>
    /// Renders a barcode as PDF.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] RenderPdf(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Render(type, content, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }).Data;

    /// <summary>
    /// Renders a barcode as EPS.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderEps(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) =>
        Render(type, content, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }).GetText();

    /// <summary>
    /// Renders a barcode as ASCII text.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderAscii(BarcodeType type, string content, BarcodeAsciiRenderOptions? options = null) =>
        Render(type, content, OutputFormat.Ascii, null, new RenderExtras { BarcodeAscii = options }).GetText();

    /// <summary>
    /// Renders a barcode as PNG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPngToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Png, options));

    /// <summary>
    /// Renders a barcode as SVG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderSvgToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Svg, options));

    /// <summary>
    /// Renders a barcode as HTML to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderHtmlToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Renders a barcode as JPEG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderJpegToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Jpeg, options));

    /// <summary>
    /// Renders a barcode as WebP to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderWebpToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Webp, options));

    /// <summary>
    /// Renders a barcode as BMP to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderBmpToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Bmp, options));

    /// <summary>
    /// Renders a barcode as PPM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPpmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Ppm, options));

    /// <summary>
    /// Renders a barcode as PBM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPbmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Pbm, options));

    /// <summary>
    /// Renders a barcode as PGM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPgmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Pgm, options));

    /// <summary>
    /// Renders a barcode as PAM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPamToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Pam, options));

    /// <summary>
    /// Renders a barcode as XBM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderXbmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Xbm, options));

    /// <summary>
    /// Renders a barcode as XPM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderXpmToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Xpm, options));

    /// <summary>
    /// Renders a barcode as TGA to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderTgaToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Tga, options));

    /// <summary>
    /// Renders a barcode as ICO to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderIcoToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Ico, options));

    /// <summary>
    /// Renders a barcode as SVGZ to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderSvgzToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) =>
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Svgz, options));

    /// <summary>
    /// Renders a barcode as PDF to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderPdfToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Renders a barcode as EPS to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void RenderEpsToStream(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        OutputWriter.Write(stream, Render(type, content, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Renders a barcode as PNG to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPngToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Png, options));

    /// <summary>
    /// Renders a barcode as SVG to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderSvgToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Svg, options));

    /// <summary>
    /// Renders a barcode as HTML to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderHtmlToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return OutputWriter.Write(path, Render(type, content, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Renders a barcode as JPEG to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderJpegToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Jpeg, options));

    /// <summary>
    /// Renders a barcode as WebP to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderWebpToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Webp, options));

    /// <summary>
    /// Renders a barcode as BMP to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderBmpToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Bmp, options));

    /// <summary>
    /// Renders a barcode as PPM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPpmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Ppm, options));

    /// <summary>
    /// Renders a barcode as PBM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPbmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Pbm, options));

    /// <summary>
    /// Renders a barcode as PGM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPgmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Pgm, options));

    /// <summary>
    /// Renders a barcode as PAM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPamToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Pam, options));

    /// <summary>
    /// Renders a barcode as XBM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderXbmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Xbm, options));

    /// <summary>
    /// Renders a barcode as XPM to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderXpmToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Xpm, options));

    /// <summary>
    /// Renders a barcode as TGA to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderTgaToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Tga, options));

    /// <summary>
    /// Renders a barcode as ICO to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderIcoToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Ico, options));

    /// <summary>
    /// Renders a barcode as SVGZ to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderSvgzToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null) =>
        OutputWriter.Write(path, Render(type, content, OutputFormat.Svgz, options));

    /// <summary>
    /// Renders a barcode as PDF to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderPdfToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(type, content, OutputFormat.Pdf, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Renders a barcode as EPS to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderEpsToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return OutputWriter.Write(path, Render(type, content, OutputFormat.Eps, options, new RenderExtras { VectorMode = mode }));
    }

    /// <summary>
    /// Renders a barcode as ASCII text to a file.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string RenderAsciiToFile(BarcodeType type, string content, string path, BarcodeAsciiRenderOptions? options = null) {
        return OutputWriter.Write(path, Render(type, content, OutputFormat.Ascii, null, new RenderExtras { BarcodeAscii = options }));
    }

    /// <summary>
    /// Saves a barcode to a file based on the output extension.
    /// </summary>
    public static string RenderToFile(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return Barcode.Save(type, content, path, options, extras);
    }
}
