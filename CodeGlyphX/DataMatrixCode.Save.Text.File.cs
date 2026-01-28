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
    /// Saves Data Matrix JPEG to a file.
    /// </summary>
    public static string SaveJpeg(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(text, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix WebP to a file.
    /// </summary>
    public static string SaveWebp(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var webp = Webp(text, mode, options);
        return webp.WriteBinary(path);
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
    /// Saves Data Matrix JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(data, mode, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves Data Matrix WebP to a file for byte payloads.
    /// </summary>
    public static string SaveWebp(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var webp = Webp(data, mode, options);
        return webp.WriteBinary(path);
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

}
