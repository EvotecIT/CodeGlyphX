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

public static partial class Barcode {
    /// <summary>
    /// Renders a barcode as PNG and writes it to a file.
    /// </summary>
    public static string SavePng(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var png = Png(type, content, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PNG and writes it to a stream.
    /// </summary>
    public static void SavePng(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePngRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a file.
    /// </summary>
    public static string SaveSvg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a stream.
    /// </summary>
    public static void SaveSvg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a file.
    /// </summary>
    public static string SaveHtml(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a stream.
    /// </summary>
    public static void SaveHtml(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a file.
    /// </summary>
    public static string SaveJpeg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var jpeg = Jpeg(type, content, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a stream.
    /// </summary>
    public static void SaveJpeg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        BarcodeJpegRenderer.RenderToStream(barcode, opts, stream, quality);
    }

    /// <summary>
    /// Renders a barcode as WebP and writes it to a file.
    /// </summary>
    public static string SaveWebp(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var webp = Webp(type, content, options);
        return webp.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as WebP and writes it to a stream.
    /// </summary>
    public static void SaveWebp(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        BarcodeWebpRenderer.RenderToStream(barcode, opts, stream, quality);
    }

    /// <summary>
    /// Renders a barcode as BMP and writes it to a file.
    /// </summary>
    public static string SaveBmp(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var bmp = Bmp(type, content, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as BMP and writes it to a stream.
    /// </summary>
    public static void SaveBmp(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeBmpRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PPM and writes it to a file.
    /// </summary>
    public static string SavePpm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var ppm = Ppm(type, content, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PPM and writes it to a stream.
    /// </summary>
    public static void SavePpm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePpmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PBM and writes it to a file.
    /// </summary>
    public static string SavePbm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pbm = Pbm(type, content, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PBM and writes it to a stream.
    /// </summary>
    public static void SavePbm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePbmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PGM and writes it to a file.
    /// </summary>
    public static string SavePgm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pgm = Pgm(type, content, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PGM and writes it to a stream.
    /// </summary>
    public static void SavePgm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePgmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PAM and writes it to a file.
    /// </summary>
    public static string SavePam(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pam = Pam(type, content, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PAM and writes it to a stream.
    /// </summary>
    public static void SavePam(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePamRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as XBM and writes it to a file.
    /// </summary>
    public static string SaveXbm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var xbm = Xbm(type, content, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as XBM and writes it to a stream.
    /// </summary>
    public static void SaveXbm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var xbm = Xbm(type, content, options);
        xbm.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as XPM and writes it to a file.
    /// </summary>
    public static string SaveXpm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var xpm = Xpm(type, content, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as XPM and writes it to a stream.
    /// </summary>
    public static void SaveXpm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var xpm = Xpm(type, content, options);
        xpm.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as TGA and writes it to a file.
    /// </summary>
    public static string SaveTga(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var tga = Tga(type, content, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as TGA and writes it to a stream.
    /// </summary>
    public static void SaveTga(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeTgaRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as ICO and writes it to a file.
    /// </summary>
    public static string SaveIco(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var ico = Ico(type, content, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as ICO and writes it to a stream.
    /// </summary>
    public static void SaveIco(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeIcoRenderer.RenderToStream(barcode, opts, stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders a barcode as SVGZ and writes it to a file.
    /// </summary>
    public static string SaveSvgz(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var svgz = Svgz(type, content, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as SVGZ and writes it to a stream.
    /// </summary>
    public static void SaveSvgz(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        BarcodeSvgzRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PDF and writes it to a file.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SavePdf(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var pdf = Pdf(type, content, options, mode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PDF and writes it to a stream.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SavePdf(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePdfRenderer.RenderToStream(barcode, opts, stream, mode);
    }

    /// <summary>
    /// Renders a barcode as EPS and writes it to a file.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SaveEps(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = Eps(type, content, options, mode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as EPS and writes it to a stream.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SaveEps(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = Eps(type, content, options, mode);
        eps.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as ASCII text and writes it to a file.
    /// </summary>
    public static string SaveAscii(BarcodeType type, string content, string path, BarcodeAsciiRenderOptions? options = null) {
        var ascii = Ascii(type, content, options);
        return ascii.WriteText(path);
    }

    /// <summary>
    /// Saves a barcode to a file based on the file extension (.png/.webp/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(type, content, path, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(type, content, path, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(type, content, path, options);
            case ".webp":
                return SaveWebp(type, content, path, options);
            case ".svg":
                return SaveSvg(type, content, path, options);
            case ".html":
            case ".htm":
                return SaveHtml(type, content, path, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(type, content, path, options);
            case ".bmp":
                return SaveBmp(type, content, path, options);
            case ".ppm":
                return SavePpm(type, content, path, options);
            case ".pbm":
                return SavePbm(type, content, path, options);
            case ".pgm":
                return SavePgm(type, content, path, options);
            case ".pam":
                return SavePam(type, content, path, options);
            case ".xbm":
                return SaveXbm(type, content, path, options);
            case ".xpm":
                return SaveXpm(type, content, path, options);
            case ".tga":
                return SaveTga(type, content, path, options);
            case ".ico":
                return SaveIco(type, content, path, options);
            case ".svgz":
                return SaveSvgz(type, content, path, options);
            case ".pdf":
                return SavePdf(type, content, path, options);
            case ".eps":
            case ".ps":
                return SaveEps(type, content, path, options);
            default:
                // Fallback to PNG for unknown extensions to keep the API forgiving.
                return SavePng(type, content, path, options);
        }
    }


}
