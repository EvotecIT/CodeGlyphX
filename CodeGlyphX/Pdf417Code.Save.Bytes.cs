using System;
using System.IO;
using System.Threading;
using System.Text;
using CodeGlyphX.Pdf417;
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

#if NET8_0_OR_GREATER
public static partial class Pdf417Code {
    /// <summary>
    /// Saves PDF417 PNG to a file for byte payloads.
    /// </summary>
    public static string SavePng(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var png = Png(data, encodeOptions, renderOptions);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PNG to a stream for byte payloads.
    /// </summary>
    public static void SavePng(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVGZ to a file for byte payloads.
    /// </summary>
    public static string SaveSvgz(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svgz = Svgz(data, encodeOptions, renderOptions);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 SVGZ to a stream for byte payloads.
    /// </summary>
    public static void SaveSvgz(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 HTML to a file for byte payloads.
    /// </summary>
    public static string SaveHtml(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 HTML to a stream for byte payloads.
    /// </summary>
    public static void SaveHtml(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(data, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 WebP to a file for byte payloads.
    /// </summary>
    public static string SaveWebp(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var webp = Webp(data, encodeOptions, renderOptions);
        return webp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(data, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PPM to a file for byte payloads.
    /// </summary>
    public static string SavePpm(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var ppm = Ppm(data, encodeOptions, renderOptions);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PBM to a file for byte payloads.
    /// </summary>
    public static string SavePbm(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pbm = Pbm(data, encodeOptions, renderOptions);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PGM to a file for byte payloads.
    /// </summary>
    public static string SavePgm(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pgm = Pgm(data, encodeOptions, renderOptions);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PAM to a file for byte payloads.
    /// </summary>
    public static string SavePam(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pam = Pam(data, encodeOptions, renderOptions);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 XBM to a file for byte payloads.
    /// </summary>
    public static string SaveXbm(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xbm = Xbm(data, encodeOptions, renderOptions);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 XPM to a file for byte payloads.
    /// </summary>
    public static string SaveXpm(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xpm = Xpm(data, encodeOptions, renderOptions);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 TGA to a file for byte payloads.
    /// </summary>
    public static string SaveTga(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var tga = Tga(data, encodeOptions, renderOptions);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 ICO to a file for byte payloads.
    /// </summary>
    public static string SaveIco(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var ico = Ico(data, encodeOptions, renderOptions);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PDF to a file for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SavePdf(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var pdf = Pdf(data, encodeOptions, renderOptions, renderMode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 EPS to a file for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SaveEps(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(data, encodeOptions, renderOptions, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 WebP to a stream for byte payloads.
    /// </summary>
    public static void SaveWebp(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.WebpQuality ?? 100;
        MatrixWebpRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PPM to a stream for byte payloads.
    /// </summary>
    public static void SavePpm(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PBM to a stream for byte payloads.
    /// </summary>
    public static void SavePbm(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PGM to a stream for byte payloads.
    /// </summary>
    public static void SavePgm(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PAM to a stream for byte payloads.
    /// </summary>
    public static void SavePam(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XBM to a stream for byte payloads.
    /// </summary>
    public static void SaveXbm(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XPM to a stream for byte payloads.
    /// </summary>
    public static void SaveXpm(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 TGA to a stream for byte payloads.
    /// </summary>
    public static void SaveTga(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 ICO to a stream for byte payloads.
    /// </summary>
    public static void SaveIco(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Saves PDF417 PDF to a stream for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SavePdf(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPdfRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, renderMode);
    }

    /// <summary>
    /// Saves PDF417 EPS to a stream for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SaveEps(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, renderMode);
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.webp/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(data, path, encodeOptions, renderOptions);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, encodeOptions, renderOptions);
            case ".webp":
                return SaveWebp(data, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(data, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(data, path, encodeOptions, renderOptions);
            case ".ppm":
                return SavePpm(data, path, encodeOptions, renderOptions);
            case ".pbm":
                return SavePbm(data, path, encodeOptions, renderOptions);
            case ".pgm":
                return SavePgm(data, path, encodeOptions, renderOptions);
            case ".pam":
                return SavePam(data, path, encodeOptions, renderOptions);
            case ".xbm":
                return SaveXbm(data, path, encodeOptions, renderOptions);
            case ".xpm":
                return SaveXpm(data, path, encodeOptions, renderOptions);
            case ".tga":
                return SaveTga(data, path, encodeOptions, renderOptions);
            case ".ico":
                return SaveIco(data, path, encodeOptions, renderOptions);
            case ".svgz":
                return SaveSvgz(data, path, encodeOptions, renderOptions);
            case ".pdf":
                return SavePdf(data, path, encodeOptions, renderOptions);
            case ".eps":
            case ".ps":
                return SaveEps(data, path, encodeOptions, renderOptions);
            default:
                return SavePng(data, path, encodeOptions, renderOptions);
        }
    }

}
#endif
