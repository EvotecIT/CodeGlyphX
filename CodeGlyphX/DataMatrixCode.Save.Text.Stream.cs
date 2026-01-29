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
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix PNG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePng(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePng(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }
    /// <summary>
    /// Saves Data Matrix SVG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvg(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(text, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvgz(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvgz(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixSvgzRenderer.RenderToStream(modules, BuildSvgOptions(options), stream);
    }
    /// <summary>
    /// Saves Data Matrix HTML to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveHtml(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        var html = Html(data, mode, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }
    /// <summary>
    /// Saves Data Matrix JPEG to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveJpeg(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix WebP to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveWebp(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        MatrixWebpRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveBmp(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePpm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePbm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePgm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePam(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXbm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXpm(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveTga(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveEps(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveJpeg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix WebP to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveWebp(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        MatrixWebpRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveBmp(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePpm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePbm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePgm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePam(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXbm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXpm(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveTga(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveEps(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

}
