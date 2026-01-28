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
    /// Saves PDF417 PNG to a file.
    /// </summary>
    public static string SavePng(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var png = Png(text, encodeOptions, renderOptions);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PNG to a file for byte payloads.
    /// </summary>
    public static string SavePng(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var png = Png(data, encodeOptions, renderOptions);
        return png.WriteBinary(path);
    }
    /// <summary>
    /// Saves PDF417 SVG to a file.
    /// </summary>
    public static string SaveSvg(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(text, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVGZ to a file for text payloads.
    /// </summary>
    public static string SaveSvgz(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svgz = Svgz(text, encodeOptions, renderOptions);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVGZ to a file for byte payloads.
    /// </summary>
    public static string SaveSvgz(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svgz = Svgz(data, encodeOptions, renderOptions);
        return svgz.WriteBinary(path);
    }
    /// <summary>
    /// Saves PDF417 HTML to a file.
    /// </summary>
    public static string SaveHtml(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(text, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 HTML to a file for byte payloads.
    /// </summary>
    public static string SaveHtml(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }
    /// <summary>
    /// Saves PDF417 JPEG to a file.
    /// </summary>
    public static string SaveJpeg(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(text, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 WebP to a file.
    /// </summary>
    public static string SaveWebp(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var webp = Webp(text, encodeOptions, renderOptions);
        return webp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file.
    /// </summary>
    public static string SaveBmp(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(text, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PPM to a file.
    /// </summary>
    public static string SavePpm(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var ppm = Ppm(text, encodeOptions, renderOptions);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PBM to a file.
    /// </summary>
    public static string SavePbm(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pbm = Pbm(text, encodeOptions, renderOptions);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PGM to a file for text payloads.
    /// </summary>
    public static string SavePgm(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pgm = Pgm(text, encodeOptions, renderOptions);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PAM to a file for text payloads.
    /// </summary>
    public static string SavePam(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pam = Pam(text, encodeOptions, renderOptions);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 XBM to a file for text payloads.
    /// </summary>
    public static string SaveXbm(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xbm = Xbm(text, encodeOptions, renderOptions);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 XPM to a file for text payloads.
    /// </summary>
    public static string SaveXpm(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xpm = Xpm(text, encodeOptions, renderOptions);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 TGA to a file.
    /// </summary>
    public static string SaveTga(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var tga = Tga(text, encodeOptions, renderOptions);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 ICO to a file.
    /// </summary>
    public static string SaveIco(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var ico = Ico(text, encodeOptions, renderOptions);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PDF to a file.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SavePdf(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var pdf = Pdf(text, encodeOptions, renderOptions, renderMode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 EPS to a file.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string SaveEps(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var eps = Eps(text, encodeOptions, renderOptions, renderMode);
        return eps.WriteText(path);
    }

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
    /// Saves PDF417 JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(data, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 WebP to a file for byte payloads.
    /// </summary>
    public static string SaveWebp(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var webp = Webp(data, encodeOptions, renderOptions);
        return webp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(data, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PPM to a file for byte payloads.
    /// </summary>
    public static string SavePpm(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var ppm = Ppm(data, encodeOptions, renderOptions);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PBM to a file for byte payloads.
    /// </summary>
    public static string SavePbm(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pbm = Pbm(data, encodeOptions, renderOptions);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PGM to a file for byte payloads.
    /// </summary>
    public static string SavePgm(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pgm = Pgm(data, encodeOptions, renderOptions);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PAM to a file for byte payloads.
    /// </summary>
    public static string SavePam(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var pam = Pam(data, encodeOptions, renderOptions);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 XBM to a file for byte payloads.
    /// </summary>
    public static string SaveXbm(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xbm = Xbm(data, encodeOptions, renderOptions);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 XPM to a file for byte payloads.
    /// </summary>
    public static string SaveXpm(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var xpm = Xpm(data, encodeOptions, renderOptions);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 TGA to a file for byte payloads.
    /// </summary>
    public static string SaveTga(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var tga = Tga(data, encodeOptions, renderOptions);
        return tga.WriteBinary(path);
    }
}
