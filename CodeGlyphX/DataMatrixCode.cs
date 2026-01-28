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

/// <summary>
/// Simple Data Matrix helpers with fluent and static APIs.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// DataMatrixCode.Save("Serial: ABC123", "datamatrix.png");
/// </code>
/// </example>
public static partial class DataMatrixCode {
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
    /// Renders Data Matrix as WebP from bytes.
    /// </summary>
    public static byte[] Webp(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        return MatrixWebpRenderer.Render(modules, opts, quality);
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
    /// Renders Data Matrix as WebP.
    /// </summary>
    public static byte[] Webp(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = Encode(text, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        return MatrixWebpRenderer.Render(modules, opts, quality);
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
    /// Renders Data Matrix as WebP from bytes.
    /// </summary>
    public static byte[] Webp(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.WebpQuality ?? 100;
        return MatrixWebpRenderer.Render(modules, opts, quality);
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
}
