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

#if NET8_0_OR_GREATER
public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix PNG to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePng(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var png = Png(data, mode, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PNG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePng(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveSvg(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveSvg(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix HTML to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveJpeg(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(data, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix WebP to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveWebp(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var webp = Webp(data, mode, options);
        return webp.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveBmp(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var bmp = Bmp(data, mode, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePpm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ppm = Ppm(data, mode, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePbm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pbm = Pbm(data, mode, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePgm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pgm = Pgm(data, mode, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SavePam(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var pam = Pam(data, mode, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXbm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xbm = Xbm(data, mode, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveXpm(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var xpm = Xpm(data, mode, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveTga(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var tga = Tga(data, mode, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveIco(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var ico = Ico(data, mode, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a file for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SaveEps(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(data, mode, options, renderMode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var jpegOptions = options?.JpegOptions;
        if (jpegOptions is null) {
            MatrixJpegRenderer.RenderToStream(modules, opts, stream, options?.JpegQuality ?? 85);
        } else {
            MatrixJpegRenderer.RenderToStream(modules, opts, stream, jpegOptions);
        }
    }

    /// <summary>
    /// Saves Data Matrix WebP to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveWebp(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        MatrixWebpRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix BMP to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveBmp(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PPM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePpm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PBM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePbm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PGM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePgm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPgmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix PAM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SavePam(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixPamRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XBM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXbm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXbmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix XPM to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveXpm(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixXpmRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix TGA to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveTga(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixTgaRenderer.RenderToStream(modules, BuildPngOptions(options), stream);
    }

    /// <summary>
    /// Saves Data Matrix ICO to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveIco(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        MatrixIcoRenderer.RenderToStream(modules, BuildPngOptions(options), stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Saves Data Matrix SVGZ to a stream for byte payloads.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
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
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static void SaveEps(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, mode);
        MatrixEpsRenderer.RenderToStream(modules, BuildPngOptions(options), stream, renderMode);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    [Obsolete("Use the overload with RenderExtras and set HtmlTitle instead.")]
    public static string Save(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode, MatrixOptions? options, string? title) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

}
#endif
