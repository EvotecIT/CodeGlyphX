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
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves PDF417 ICO to a file for byte payloads.
    /// </summary>
    public static string SaveIco(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
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
    public static string SavePdf(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
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
    public static string SaveEps(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(data, encodeOptions, renderOptions, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream.
    /// </summary>
    public static void SaveJpeg(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream.
    /// </summary>
    public static void SaveBmp(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PPM to a stream.
    /// </summary>
    public static void SavePpm(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PBM to a stream.
    /// </summary>
    public static void SavePbm(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PGM to a stream.
    /// </summary>
    public static void SavePgm(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PAM to a stream.
    /// </summary>
    public static void SavePam(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XBM to a stream.
    /// </summary>
    public static void SaveXbm(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XPM to a stream.
    /// </summary>
    public static void SaveXpm(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 TGA to a stream.
    /// </summary>
    public static void SaveTga(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 ICO to a stream.
    /// </summary>
    public static void SaveIco(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Saves PDF417 PDF to a stream.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SavePdf(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        MatrixPdfRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, renderMode);
    }

    /// <summary>
    /// Saves PDF417 EPS to a stream.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SaveEps(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, renderMode);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PPM to a stream for byte payloads.
    /// </summary>
    public static void SavePpm(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PBM to a stream for byte payloads.
    /// </summary>
    public static void SavePbm(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PGM to a stream for byte payloads.
    /// </summary>
    public static void SavePgm(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PAM to a stream for byte payloads.
    /// </summary>
    public static void SavePam(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XBM to a stream for byte payloads.
    /// </summary>
    public static void SaveXbm(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 XPM to a stream for byte payloads.
    /// </summary>
    public static void SaveXpm(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 TGA to a stream for byte payloads.
    /// </summary>
    public static void SaveTga(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 ICO to a stream for byte payloads.
    /// </summary>
    public static void SaveIco(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
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
    public static void SavePdf(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
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
    public static void SaveEps(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream, renderMode);
    }

    /// <summary>
    /// Saves PDF417 to a file based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(text, path, encodeOptions, renderOptions);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(text, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(text, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(text, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(text, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(text, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(text, path, encodeOptions, renderOptions);
            case ".ppm":
                return SavePpm(text, path, encodeOptions, renderOptions);
            case ".pbm":
                return SavePbm(text, path, encodeOptions, renderOptions);
            case ".pgm":
                return SavePgm(text, path, encodeOptions, renderOptions);
            case ".pam":
                return SavePam(text, path, encodeOptions, renderOptions);
            case ".xbm":
                return SaveXbm(text, path, encodeOptions, renderOptions);
            case ".xpm":
                return SaveXpm(text, path, encodeOptions, renderOptions);
            case ".tga":
                return SaveTga(text, path, encodeOptions, renderOptions);
            case ".ico":
                return SaveIco(text, path, encodeOptions, renderOptions);
            case ".svgz":
                return SaveSvgz(text, path, encodeOptions, renderOptions);
            case ".pdf":
                return SavePdf(text, path, encodeOptions, renderOptions);
            case ".eps":
            case ".ps":
                return SaveEps(text, path, encodeOptions, renderOptions);
            default:
                return SavePng(text, path, encodeOptions, renderOptions);
        }
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(data, path, encodeOptions, renderOptions);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, encodeOptions, renderOptions);
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
