using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
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
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class AztecCode {
    /// <summary>
    /// Saves Aztec PNG to a file.
    /// </summary>
    public static string SavePng(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Png(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PNG to a stream.
    /// </summary>
    public static void SavePng(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Png(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVG to a file.
    /// </summary>
    public static string SaveSvg(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Svg(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVGZ to a file.
    /// </summary>
    public static string SaveSvgz(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Svgz(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVG to a stream.
    /// </summary>
    public static void SaveSvg(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Svg(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVGZ to a stream.
    /// </summary>
    public static void SaveSvgz(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Svgz(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec HTML to a file.
    /// </summary>
    public static string SaveHtml(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(text, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Saves Aztec HTML to a stream.
    /// </summary>
    public static void SaveHtml(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(text, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        RenderIO.WriteText(stream, html);
    }

    /// <summary>
    /// Saves Aztec JPEG to a file.
    /// </summary>
    public static string SaveJpeg(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Jpeg(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec JPEG to a stream.
    /// </summary>
    public static void SaveJpeg(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Jpeg(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec BMP to a file.
    /// </summary>
    public static string SaveBmp(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Bmp(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec BMP to a stream.
    /// </summary>
    public static void SaveBmp(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Bmp(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PPM to a file.
    /// </summary>
    public static string SavePpm(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Ppm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PPM to a stream.
    /// </summary>
    public static void SavePpm(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Ppm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PBM to a file.
    /// </summary>
    public static string SavePbm(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pbm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PBM to a stream.
    /// </summary>
    public static void SavePbm(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pbm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PGM to a file.
    /// </summary>
    public static string SavePgm(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pgm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PGM to a stream.
    /// </summary>
    public static void SavePgm(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pgm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PAM to a file.
    /// </summary>
    public static string SavePam(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pam(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PAM to a stream.
    /// </summary>
    public static void SavePam(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pam(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XBM to a file.
    /// </summary>
    public static string SaveXbm(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Xbm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XBM to a stream.
    /// </summary>
    public static void SaveXbm(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Xbm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XPM to a file.
    /// </summary>
    public static string SaveXpm(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Xpm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XPM to a stream.
    /// </summary>
    public static void SaveXpm(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Xpm(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec TGA to a file.
    /// </summary>
    public static string SaveTga(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Tga(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec TGA to a stream.
    /// </summary>
    public static void SaveTga(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Tga(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec ICO to a file.
    /// </summary>
    public static string SaveIco(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Ico(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec ICO to a stream.
    /// </summary>
    public static void SaveIco(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Ico(text, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PDF to a file.
    /// </summary>
    public static string SavePdf(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        return RenderIO.WriteBinary(path, Pdf(text, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec PDF to a stream.
    /// </summary>
    public static void SavePdf(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        RenderIO.WriteBinary(stream, Pdf(text, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec EPS to a file.
    /// </summary>
    public static string SaveEps(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        return RenderIO.WriteText(path, Eps(text, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec EPS to a stream.
    /// </summary>
    public static void SaveEps(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        RenderIO.WriteText(stream, Eps(text, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec PNG to a file.
    /// </summary>
    public static string SavePng(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Png(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PNG to a stream.
    /// </summary>
    public static void SavePng(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Png(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVG to a file.
    /// </summary>
    public static string SaveSvg(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Svg(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVG to a stream.
    /// </summary>
    public static void SaveSvg(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Svg(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec HTML to a file.
    /// </summary>
    public static string SaveHtml(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Saves Aztec HTML to a stream.
    /// </summary>
    public static void SaveHtml(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        RenderIO.WriteText(stream, html);
    }

    /// <summary>
    /// Saves Aztec JPEG to a file.
    /// </summary>
    public static string SaveJpeg(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Jpeg(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec JPEG to a stream.
    /// </summary>
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Jpeg(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec BMP to a file.
    /// </summary>
    public static string SaveBmp(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Bmp(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec BMP to a stream.
    /// </summary>
    public static void SaveBmp(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Bmp(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PPM to a file.
    /// </summary>
    public static string SavePpm(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Ppm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PPM to a stream.
    /// </summary>
    public static void SavePpm(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Ppm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PBM to a file.
    /// </summary>
    public static string SavePbm(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pbm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PBM to a stream.
    /// </summary>
    public static void SavePbm(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pbm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PGM to a file.
    /// </summary>
    public static string SavePgm(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pgm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PGM to a stream.
    /// </summary>
    public static void SavePgm(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pgm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PAM to a file.
    /// </summary>
    public static string SavePam(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Pam(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PAM to a stream.
    /// </summary>
    public static void SavePam(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Pam(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XBM to a file.
    /// </summary>
    public static string SaveXbm(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Xbm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XBM to a stream.
    /// </summary>
    public static void SaveXbm(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Xbm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XPM to a file.
    /// </summary>
    public static string SaveXpm(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteText(path, Xpm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec XPM to a stream.
    /// </summary>
    public static void SaveXpm(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Xpm(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec TGA to a file.
    /// </summary>
    public static string SaveTga(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Tga(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec TGA to a stream.
    /// </summary>
    public static void SaveTga(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Tga(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec ICO to a file.
    /// </summary>
    public static string SaveIco(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Ico(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec ICO to a stream.
    /// </summary>
    public static void SaveIco(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Ico(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVGZ to a file.
    /// </summary>
    public static string SaveSvgz(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return RenderIO.WriteBinary(path, Svgz(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec SVGZ to a stream.
    /// </summary>
    public static void SaveSvgz(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteBinary(stream, Svgz(data, encodeOptions, renderOptions));
    }

    /// <summary>
    /// Saves Aztec PDF to a file.
    /// </summary>
    public static string SavePdf(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        return RenderIO.WriteBinary(path, Pdf(data, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec PDF to a stream.
    /// </summary>
    public static void SavePdf(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        RenderIO.WriteBinary(stream, Pdf(data, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec EPS to a file.
    /// </summary>
    public static string SaveEps(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        return RenderIO.WriteText(path, Eps(data, encodeOptions, renderOptions, renderMode));
    }

    /// <summary>
    /// Saves Aztec EPS to a stream.
    /// </summary>
    public static void SaveEps(ReadOnlySpan<byte> data, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        RenderIO.WriteText(stream, Eps(data, encodeOptions, renderOptions, renderMode));
    }


}
