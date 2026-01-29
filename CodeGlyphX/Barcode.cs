using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Simple barcode helpers with fluent and static APIs.
/// </summary>
/// <remarks>
/// Use <see cref="Save(CodeGlyphX.BarcodeType,string,string,CodeGlyphX.BarcodeOptions,CodeGlyphX.Rendering.RenderExtras)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "barcode.png");
/// </code>
/// </example>
public static partial class Barcode {
    /// <summary>
    /// Starts a fluent barcode builder.
    /// </summary>
    public static BarcodeBuilder Create(BarcodeType type, string content, BarcodeOptions? options = null) {
        return new BarcodeBuilder(type, content, options);
    }

    /// <summary>
    /// Encodes a 1D barcode.
    /// </summary>
    public static Barcode1D Encode(BarcodeType type, string content) {
        return BarcodeEncoder.Encode(type, content);
    }

    /// <summary>
    /// Renders a barcode as PNG.
    /// </summary>
    public static byte[] Png(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePngRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as SVG.
    /// </summary>
    public static string Svg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        return SvgBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as HTML.
    /// </summary>
    public static string Html(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildHtmlOptions(options);
        return HtmlBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as JPEG.
    /// </summary>
    public static byte[] Jpeg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        return BarcodeJpegRenderer.Render(barcode, opts, quality);
    }

    /// <summary>
    /// Renders a barcode as WebP.
    /// </summary>
    public static byte[] Webp(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        return BarcodeWebpRenderer.Render(barcode, opts, quality);
    }

    /// <summary>
    /// Renders a barcode as BMP.
    /// </summary>
    public static byte[] Bmp(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeBmpRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PPM.
    /// </summary>
    public static byte[] Ppm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePpmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PBM.
    /// </summary>
    public static byte[] Pbm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePbmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PGM.
    /// </summary>
    public static byte[] Pgm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePgmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PAM.
    /// </summary>
    public static byte[] Pam(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePamRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as XBM.
    /// </summary>
    public static string Xbm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeXbmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as XPM.
    /// </summary>
    public static string Xpm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeXpmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as TGA.
    /// </summary>
    public static byte[] Tga(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeTgaRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as ICO.
    /// </summary>
    public static byte[] Ico(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeIcoRenderer.Render(barcode, opts, BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders a barcode as SVGZ.
    /// </summary>
    public static byte[] Svgz(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        return BarcodeSvgzRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PDF.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] Pdf(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePdfRenderer.Render(barcode, opts, mode);
    }

    /// <summary>
    /// Renders a barcode as EPS.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string Eps(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeEpsRenderer.Render(barcode, opts, mode);
    }

    /// <summary>
    /// Renders a barcode as ASCII text.
    /// </summary>
    public static string Ascii(BarcodeType type, string content, BarcodeAsciiRenderOptions? options = null) {
        var barcode = Encode(type, content);
        return BarcodeAsciiRenderer.Render(barcode, options);
    }
}
