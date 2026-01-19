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

/// <summary>
/// Simple Data Matrix helpers with fluent and static APIs.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// DataMatrixCode.Save("Serial: ABC123", "datamatrix.png");
/// </code>
/// </example>
public static class DataMatrixCode {
    /// <summary>
    /// Starts a fluent Data Matrix builder for text payloads.
    /// </summary>
    public static DataMatrixBuilder Create(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        return new DataMatrixBuilder(text, mode, options);
    }

    /// <summary>
    /// Starts a fluent Data Matrix builder for byte payloads.
    /// </summary>
    public static DataMatrixBuilder Create(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        return new DataMatrixBuilder(data, mode, options);
    }

    /// <summary>
    /// Encodes a text payload as Data Matrix.
    /// </summary>
    public static BitMatrix Encode(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return DataMatrixEncoder.Encode(text, mode);
    }

    /// <summary>
    /// Encodes a byte payload as Data Matrix.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return DataMatrixEncoder.EncodeBytes(data, mode);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes a byte payload as Data Matrix.
    /// </summary>
    public static BitMatrix EncodeBytes(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return DataMatrixEncoder.EncodeBytes(data, mode);
    }

    /// <summary>
    /// Renders Data Matrix as PNG from bytes.
    /// </summary>
    public static byte[] Png(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVG from bytes.
    /// </summary>
    public static string Svg(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVGZ from bytes.
    /// </summary>
    public static byte[] Svgz(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as HTML from bytes.
    /// </summary>
    public static string Html(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as JPEG from bytes.
    /// </summary>
    public static byte[] Jpeg(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders Data Matrix as BMP from bytes.
    /// </summary>
    public static byte[] Bmp(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PPM from bytes.
    /// </summary>
    public static byte[] Ppm(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PBM from bytes.
    /// </summary>
    public static byte[] Pbm(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PGM from bytes.
    /// </summary>
    public static byte[] Pgm(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PAM from bytes.
    /// </summary>
    public static byte[] Pam(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XBM from bytes.
    /// </summary>
    public static string Xbm(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XPM from bytes.
    /// </summary>
    public static string Xpm(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as TGA from bytes.
    /// </summary>
    public static byte[] Tga(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as ICO from bytes.
    /// </summary>
    public static byte[] Ico(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(options), BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PDF from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as EPS from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as ASCII from bytes.
    /// </summary>
    public static string Ascii(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixAsciiRenderer.Render(modules, options);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a file for byte payloads.
    /// </summary>
    public static string SavePng(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var png = Png(data, mode, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a stream for byte payloads.
    /// </summary>
    public static void SavePng(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a file for byte payloads.
    /// </summary>
    public static string SaveHtml(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(data, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a stream for byte payloads.
    /// </summary>
    public static void SaveHtml(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(data, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(data, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var bmp = Bmp(data, mode, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a file for byte payloads.
    /// </summary>
    public static string SavePpm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ppm = Ppm(data, mode, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a file for byte payloads.
    /// </summary>
    public static string SavePbm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pbm = Pbm(data, mode, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a file for byte payloads.
    /// </summary>
    public static string SavePgm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pgm = Pgm(data, mode, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a file for byte payloads.
    /// </summary>
    public static string SavePam(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pam = Pam(data, mode, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a file for byte payloads.
    /// </summary>
    public static string SaveXbm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xbm = Xbm(data, mode, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a file for byte payloads.
    /// </summary>
    public static string SaveXpm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xpm = Xpm(data, mode, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a file for byte payloads.
    /// </summary>
    public static string SaveTga(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var tga = Tga(data, mode, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a file for byte payloads.
    /// </summary>
    public static string SaveIco(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ico = Ico(data, mode, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a file for byte payloads.
    /// </summary>
    public static string SaveSvgz(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svgz = Svgz(data, mode, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PDF to a file for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SavePdf(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
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
    public static string SaveEps(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(data, mode, options, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream for byte payloads.
    /// </summary>
    public static void SavePpm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream for byte payloads.
    /// </summary>
    public static void SavePbm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream for byte payloads.
    /// </summary>
    public static void SavePgm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream for byte payloads.
    /// </summary>
    public static void SavePam(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream for byte payloads.
    /// </summary>
    public static void SaveXbm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream for byte payloads.
    /// </summary>
    public static void SaveXpm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream for byte payloads.
    /// </summary>
    public static void SaveTga(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream for byte payloads.
    /// </summary>
    public static void SaveIco(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(options), stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream for byte payloads.
    /// </summary>
    public static void SaveSvgz(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PDF to a stream for byte payloads.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static void SavePdf(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
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
    public static void SaveEps(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
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
#endif

    /// <summary>
    /// Renders Data Matrix as PNG.
    /// </summary>
    public static byte[] Png(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PNG from bytes.
    /// </summary>
    public static byte[] Png(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVG.
    /// </summary>
    public static string Svg(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVGZ.
    /// </summary>
    public static byte[] Svgz(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVG from bytes.
    /// </summary>
    public static string Svg(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as SVGZ from bytes.
    /// </summary>
    public static byte[] Svgz(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as HTML.
    /// </summary>
    public static string Html(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as HTML from bytes.
    /// </summary>
    public static string Html(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as JPEG.
    /// </summary>
    public static byte[] Jpeg(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders Data Matrix as BMP.
    /// </summary>
    public static byte[] Bmp(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PPM.
    /// </summary>
    public static byte[] Ppm(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PBM.
    /// </summary>
    public static byte[] Pbm(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PGM.
    /// </summary>
    public static byte[] Pgm(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PAM.
    /// </summary>
    public static byte[] Pam(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XBM.
    /// </summary>
    public static string Xbm(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XPM.
    /// </summary>
    public static string Xpm(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as TGA.
    /// </summary>
    public static byte[] Tga(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as ICO.
    /// </summary>
    public static byte[] Ico(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(options), BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PDF.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, mode);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as EPS.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, mode);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as ASCII.
    /// </summary>
    public static string Ascii(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixAsciiRenderOptions? options = null) {
        var modules = Encode(text, mode);
        return MatrixAsciiRenderer.Render(modules, options);
    }

    /// <summary>
    /// Renders Data Matrix as JPEG from bytes.
    /// </summary>
    public static byte[] Jpeg(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders Data Matrix as BMP from bytes.
    /// </summary>
    public static byte[] Bmp(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PPM.
    /// </summary>
    public static byte[] Ppm(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PBM.
    /// </summary>
    public static byte[] Pbm(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PGM.
    /// </summary>
    public static byte[] Pgm(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PAM.
    /// </summary>
    public static byte[] Pam(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XBM.
    /// </summary>
    public static string Xbm(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as XPM.
    /// </summary>
    public static string Xpm(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as TGA.
    /// </summary>
    public static byte[] Tga(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as ICO.
    /// </summary>
    public static byte[] Ico(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(options), BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders Data Matrix as PDF from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as EPS from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(options), renderMode);
    }

    /// <summary>
    /// Renders Data Matrix as ASCII from bytes.
    /// </summary>
    public static string Ascii(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixAsciiRenderer.Render(modules, options);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a file.
    /// </summary>
    public static string SavePng(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var png = Png(text, mode, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a file for byte payloads.
    /// </summary>
    public static string SavePng(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var png = Png(data, mode, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a stream.
    /// </summary>
    public static void SavePng(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a stream for byte payloads.
    /// </summary>
    public static void SavePng(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a file.
    /// </summary>
    public static string SaveSvg(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(text, mode, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a file.
    /// </summary>
    public static string SaveSvgz(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svgz = Svgz(text, mode, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a file for byte payloads.
    /// </summary>
    public static string SaveSvgz(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svgz = Svgz(data, mode, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream.
    /// </summary>
    public static void SaveSvg(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(text, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream.
    /// </summary>
    public static void SaveSvgz(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream for byte payloads.
    /// </summary>
    public static void SaveSvgz(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a file.
    /// </summary>
    public static string SaveHtml(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(text, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a file for byte payloads.
    /// </summary>
    public static string SaveHtml(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(data, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a stream.
    /// </summary>
    public static void SaveHtml(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(text, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a stream for byte payloads.
    /// </summary>
    public static void SaveHtml(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(data, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a file.
    /// </summary>
    public static string SaveJpeg(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(text, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a file.
    /// </summary>
    public static string SaveBmp(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var bmp = Bmp(text, mode, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a file for text payloads.
    /// </summary>
    public static string SavePpm(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ppm = Ppm(text, mode, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a file for text payloads.
    /// </summary>
    public static string SavePbm(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pbm = Pbm(text, mode, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a file for text payloads.
    /// </summary>
    public static string SavePgm(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pgm = Pgm(text, mode, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a file for text payloads.
    /// </summary>
    public static string SavePam(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pam = Pam(text, mode, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a file for text payloads.
    /// </summary>
    public static string SaveXbm(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xbm = Xbm(text, mode, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a file for text payloads.
    /// </summary>
    public static string SaveXpm(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xpm = Xpm(text, mode, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a file for text payloads.
    /// </summary>
    public static string SaveTga(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var tga = Tga(text, mode, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a file for text payloads.
    /// </summary>
    public static string SaveIco(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ico = Ico(text, mode, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PDF to a file.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SavePdf(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var pdf = Pdf(text, mode, options, renderMode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix EPS to a file.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="mode">Vector or raster output.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SaveEps(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(text, mode, options, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(data, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var bmp = Bmp(data, mode, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a file for byte payloads.
    /// </summary>
    public static string SavePpm(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ppm = Ppm(data, mode, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a file for byte payloads.
    /// </summary>
    public static string SavePbm(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pbm = Pbm(data, mode, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a file for byte payloads.
    /// </summary>
    public static string SavePgm(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pgm = Pgm(data, mode, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a file for byte payloads.
    /// </summary>
    public static string SavePam(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pam = Pam(data, mode, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a file for byte payloads.
    /// </summary>
    public static string SaveXbm(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xbm = Xbm(data, mode, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a file for byte payloads.
    /// </summary>
    public static string SaveXpm(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xpm = Xpm(data, mode, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a file for byte payloads.
    /// </summary>
    public static string SaveTga(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var tga = Tga(data, mode, options);
        return tga.WriteBinary(path);
    }

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
    /// Attempts to decode a Data Matrix symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        return TryDecodePng(png, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(png, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(png, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        return TryDecodePngFile(path, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out string text) {
        return TryDecodePngFile(path, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        return TryDecodePng(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out string text) {
        return TryDecodeImage(image, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(image, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(image, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out string text) {
        return TryDecodeImage(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var data = RenderIO.ReadBinary(stream);
            if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from PNG bytes.
    /// </summary>
    public static string DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var text)) {
            throw new FormatException("PNG does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from a PNG file.
    /// </summary>
    public static string DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var text)) {
            throw new FormatException("PNG file does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from a PNG stream.
    /// </summary>
    public static string DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var text)) {
            throw new FormatException("PNG stream does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    private static MatrixPngRenderOptions BuildPngOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixPngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }

    private static IcoRenderOptions BuildIcoOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static MatrixSvgRenderOptions BuildSvgOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ColorUtils.ToCss(opts.Foreground),
            LightColor = ColorUtils.ToCss(opts.Background)
        };
    }

    private static MatrixHtmlRenderOptions BuildHtmlOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ColorUtils.ToCss(opts.Foreground),
            LightColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

    /// <summary>
    /// Fluent Data Matrix builder.
    /// </summary>
    public sealed class DataMatrixBuilder {
        private readonly string? _text;
        private readonly byte[]? _bytes;
        private DataMatrixEncodingMode _mode;
        private readonly MatrixOptions _options;

        internal DataMatrixBuilder(string text, DataMatrixEncodingMode mode, MatrixOptions? options) {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _mode = mode;
            _options = options ?? new MatrixOptions();
        }

        internal DataMatrixBuilder(byte[] data, DataMatrixEncodingMode mode, MatrixOptions? options) {
            _bytes = data ?? throw new ArgumentNullException(nameof(data));
            _mode = mode;
            _options = options ?? new MatrixOptions();
        }

        /// <summary>
        /// Mutates rendering options.
        /// </summary>
        public DataMatrixBuilder WithOptions(Action<MatrixOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options);
            return this;
        }

        /// <summary>
        /// Sets the encoding mode.
        /// </summary>
        public DataMatrixBuilder WithMode(DataMatrixEncodingMode mode) {
            _mode = mode;
            return this;
        }

        /// <summary>
        /// Sets module size in pixels.
        /// </summary>
        public DataMatrixBuilder WithModuleSize(int moduleSize) {
            _options.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size in modules.
        /// </summary>
        public DataMatrixBuilder WithQuietZone(int quietZone) {
            _options.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets foreground/background colors.
        /// </summary>
        public DataMatrixBuilder WithColors(Rgba32 foreground, Rgba32 background) {
            _options.Foreground = foreground;
            _options.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality (1..100).
        /// </summary>
        public DataMatrixBuilder WithJpegQuality(int quality) {
            _options.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Enables HTML email-safe table rendering.
        /// </summary>
        public DataMatrixBuilder WithHtmlEmailSafeTable(bool enabled = true) {
            _options.HtmlEmailSafeTable = enabled;
            return this;
        }

        /// <summary>
        /// Sets ICO output sizes (in pixels).
        /// </summary>
        public DataMatrixBuilder WithIcoSizes(params int[] sizes) {
            _options.IcoSizes = sizes;
            return this;
        }

        /// <summary>
        /// Sets ICO aspect ratio preservation behavior.
        /// </summary>
        public DataMatrixBuilder WithIcoPreserveAspectRatio(bool enabled = true) {
            _options.IcoPreserveAspectRatio = enabled;
            return this;
        }

        /// <summary>
        /// Encodes the Data Matrix as a module matrix.
        /// </summary>
        public BitMatrix Encode() {
            return _text is not null ? DataMatrixCode.Encode(_text, _mode) : DataMatrixCode.EncodeBytes(_bytes!, _mode);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() {
            return _text is not null ? DataMatrixCode.Png(_text, _mode, _options) : DataMatrixCode.Png(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders SVG markup.
        /// </summary>
        public string Svg() {
            return _text is not null ? DataMatrixCode.Svg(_text, _mode, _options) : DataMatrixCode.Svg(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders HTML markup.
        /// </summary>
        public string Html() {
            return _text is not null ? DataMatrixCode.Html(_text, _mode, _options) : DataMatrixCode.Html(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() {
            return _text is not null ? DataMatrixCode.Jpeg(_text, _mode, _options) : DataMatrixCode.Jpeg(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() {
            return _text is not null ? DataMatrixCode.Bmp(_text, _mode, _options) : DataMatrixCode.Bmp(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.Pdf(_text, _mode, _options, renderMode) : DataMatrixCode.Pdf(_bytes!, _mode, _options, renderMode);
        }

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public string Eps(RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.Eps(_text, _mode, _options, renderMode) : DataMatrixCode.Eps(_bytes!, _mode, _options, renderMode);
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? options = null) {
            return _text is not null ? DataMatrixCode.Ascii(_text, _mode, options) : DataMatrixCode.Ascii(_bytes!, _mode, options);
        }

        /// <summary>
        /// Saves output based on file extension.
        /// </summary>
        public string Save(string path, string? title = null) {
            if (_text is not null) return DataMatrixCode.Save(_text, path, _mode, _options, title);
            return DataMatrixCode.Save(_bytes!, path, _mode, _options, title);
        }

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) {
            return _text is not null ? DataMatrixCode.SavePng(_text, path, _mode, _options) : DataMatrixCode.SavePng(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) {
            return _text is not null ? DataMatrixCode.SaveSvg(_text, path, _mode, _options) : DataMatrixCode.SaveSvg(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            return _text is not null ? DataMatrixCode.SaveHtml(_text, path, _mode, _options, title) : DataMatrixCode.SaveHtml(_bytes!, path, _mode, _options, title);
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) {
            return _text is not null ? DataMatrixCode.SaveJpeg(_text, path, _mode, _options) : DataMatrixCode.SaveJpeg(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) {
            return _text is not null ? DataMatrixCode.SaveBmp(_text, path, _mode, _options) : DataMatrixCode.SaveBmp(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.SavePdf(_text, path, _mode, _options, renderMode) : DataMatrixCode.SavePdf(_bytes!, path, _mode, _options, renderMode);
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.SaveEps(_text, path, _mode, _options, renderMode) : DataMatrixCode.SaveEps(_bytes!, path, _mode, _options, renderMode);
        }
    }
}
