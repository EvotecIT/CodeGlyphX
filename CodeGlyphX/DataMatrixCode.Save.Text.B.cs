using System;
using System.IO;
using System.Threading;
using CodeGlyphX.DataMatrix;
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

public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix ICO to a file for byte payloads.
    /// </summary>
    public static string SaveIco(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ico = Ico(data, mode, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PDF to a file for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SavePdf(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var pdf = Pdf(data, mode, options, renderMode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix EPS to a file for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SaveEps(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(data, mode, options, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a stream.
    /// </summary>
    public static void SaveJpeg(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream.
    /// </summary>
    public static void SaveBmp(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream.
    /// </summary>
    public static void SavePpm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream.
    /// </summary>
    public static void SavePbm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream.
    /// </summary>
    public static void SavePgm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream.
    /// </summary>
    public static void SavePam(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream.
    /// </summary>
    public static void SaveXbm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream.
    /// </summary>
    public static void SaveXpm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream.
    /// </summary>
    public static void SaveTga(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream.
    /// </summary>
    public static void SaveIco(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(options), stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Saves Data Matrix PDF to a stream.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SavePdf(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, mode);
        MatrixPdfRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix EPS to a stream.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SaveEps(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream for byte payloads.
    /// </summary>
    public static void SavePpm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream for byte payloads.
    /// </summary>
    public static void SavePbm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream for byte payloads.
    /// </summary>
    public static void SavePgm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream for byte payloads.
    /// </summary>
    public static void SavePam(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream for byte payloads.
    /// </summary>
    public static void SaveXbm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream for byte payloads.
    /// </summary>
    public static void SaveXpm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream for byte payloads.
    /// </summary>
    public static void SaveTga(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream for byte payloads.
    /// </summary>
    public static void SaveIco(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(options), stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Saves Data Matrix PDF to a stream for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SavePdf(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        MatrixPdfRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix EPS to a stream for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SaveEps(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix to a file based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(text, path, mode, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(text, path, mode, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(text, path, mode, options);
            case ".svg":
                return SaveSvg(text, path, mode, options);
            case ".html":
            case ".htm":
                return SaveHtml(text, path, mode, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(text, path, mode, options);
            case ".bmp":
                return SaveBmp(text, path, mode, options);
            case ".ppm":
                return SavePpm(text, path, mode, options);
            case ".pbm":
                return SavePbm(text, path, mode, options);
            case ".pgm":
                return SavePgm(text, path, mode, options);
            case ".pam":
                return SavePam(text, path, mode, options);
            case ".xbm":
                return SaveXbm(text, path, mode, options);
            case ".xpm":
                return SaveXpm(text, path, mode, options);
            case ".tga":
                return SaveTga(text, path, mode, options);
            case ".ico":
                return SaveIco(text, path, mode, options);
            case ".svgz":
                return SaveSvgz(text, path, mode, options);
            case ".pdf":
                return SavePdf(text, path, mode, options);
            case ".eps":
            case ".ps":
                return SaveEps(text, path, mode, options);
            default:
                return SavePng(text, path, mode, options);
        }
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(data, path, mode, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, mode, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, mode, options);
            case ".svg":
                return SaveSvg(data, path, mode, options);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, mode, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, mode, options);
            case ".bmp":
                return SaveBmp(data, path, mode, options);
            case ".ppm":
                return SavePpm(data, path, mode, options);
            case ".pbm":
                return SavePbm(data, path, mode, options);
            case ".pgm":
                return SavePgm(data, path, mode, options);
            case ".pam":
                return SavePam(data, path, mode, options);
            case ".xbm":
                return SaveXbm(data, path, mode, options);
            case ".xpm":
                return SaveXpm(data, path, mode, options);
            case ".tga":
                return SaveTga(data, path, mode, options);
            case ".ico":
                return SaveIco(data, path, mode, options);
            case ".svgz":
                return SaveSvgz(data, path, mode, options);
            case ".pdf":
                return SavePdf(data, path, mode, options);
            case ".eps":
            case ".ps":
                return SaveEps(data, path, mode, options);
            default:
                return SavePng(data, path, mode, options);
        }
    }

    /// <summary>

}
