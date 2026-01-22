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

/// <summary>
/// Simple PDF417 helpers with fluent and static APIs.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// Pdf417Code.Save("Document ID: 98765", "pdf417.png");
/// </code>
/// </example>
public static partial class Pdf417Code {
    /// <summary>
    /// Starts a fluent PDF417 builder for text payloads.
    /// </summary>
    public static Pdf417Builder Create(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return new Pdf417Builder(text, encodeOptions, renderOptions);
    }

    /// <summary>
    /// Starts a fluent PDF417 builder for byte payloads.
    /// </summary>
    public static Pdf417Builder Create(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return new Pdf417Builder(data, encodeOptions, renderOptions);
    }

    /// <summary>
    /// Encodes a text payload as PDF417.
    /// </summary>
    public static BitMatrix Encode(string text, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes a Macro PDF417 payload.
    /// </summary>
    public static BitMatrix EncodeMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.EncodeMacro(text, macro, options);
    }

    /// <summary>
    /// Encodes a byte payload as PDF417.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.EncodeBytes(data, options);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes a byte payload as PDF417.
    /// </summary>
    public static BitMatrix EncodeBytes(ReadOnlySpan<byte> data, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.EncodeBytes(data, options);
    }

    /// <summary>
    /// Renders PDF417 as PNG from bytes.
    /// </summary>
    public static byte[] Png(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVG from bytes.
    /// </summary>
    public static string Svg(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVGZ from bytes.
    /// </summary>
    public static byte[] Svgz(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as HTML from bytes.
    /// </summary>
    public static string Html(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as JPEG from bytes.
    /// </summary>
    public static byte[] Jpeg(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders PDF417 as BMP from bytes.
    /// </summary>
    public static byte[] Bmp(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PPM from bytes.
    /// </summary>
    public static byte[] Ppm(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PBM from bytes.
    /// </summary>
    public static byte[] Pbm(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PGM from bytes.
    /// </summary>
    public static byte[] Pgm(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PAM from bytes.
    /// </summary>
    public static byte[] Pam(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XBM from bytes.
    /// </summary>
    public static string Xbm(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XPM from bytes.
    /// </summary>
    public static string Xpm(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as TGA from bytes.
    /// </summary>
    public static byte[] Tga(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as ICO from bytes.
    /// </summary>
    public static byte[] Ico(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(renderOptions), BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PDF from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as EPS from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as ASCII from bytes.
    /// </summary>
    public static string Ascii(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }

#endif

    /// <summary>
    /// Renders PDF417 as PNG.
    /// </summary>
    public static byte[] Png(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PNG from bytes.
    /// </summary>
    public static byte[] Png(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVG.
    /// </summary>
    public static string Svg(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVGZ.
    /// </summary>
    public static byte[] Svgz(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVG from bytes.
    /// </summary>
    public static string Svg(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as SVGZ from bytes.
    /// </summary>
    public static byte[] Svgz(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as HTML.
    /// </summary>
    public static string Html(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as HTML from bytes.
    /// </summary>
    public static string Html(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as JPEG.
    /// </summary>
    public static byte[] Jpeg(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders PDF417 as BMP.
    /// </summary>
    public static byte[] Bmp(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PPM.
    /// </summary>
    public static byte[] Ppm(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PBM.
    /// </summary>
    public static byte[] Pbm(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PGM.
    /// </summary>
    public static byte[] Pgm(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PAM.
    /// </summary>
    public static byte[] Pam(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XBM.
    /// </summary>
    public static string Xbm(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XPM.
    /// </summary>
    public static string Xpm(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as TGA.
    /// </summary>
    public static byte[] Tga(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as ICO.
    /// </summary>
    public static byte[] Ico(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(renderOptions), BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PDF.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as EPS.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as ASCII.
    /// </summary>
    public static string Ascii(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }

    /// <summary>
    /// Renders PDF417 as JPEG from bytes.
    /// </summary>
    public static byte[] Jpeg(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders PDF417 as BMP from bytes.
    /// </summary>
    public static byte[] Bmp(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PPM.
    /// </summary>
    public static byte[] Ppm(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PBM.
    /// </summary>
    public static byte[] Pbm(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PGM.
    /// </summary>
    public static byte[] Pgm(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PAM.
    /// </summary>
    public static byte[] Pam(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XBM.
    /// </summary>
    public static string Xbm(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as XPM.
    /// </summary>
    public static string Xpm(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as TGA.
    /// </summary>
    public static byte[] Tga(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as ICO.
    /// </summary>
    public static byte[] Ico(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(renderOptions), BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders PDF417 as PDF from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static byte[] Pdf(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as EPS from bytes.
    /// </summary>
    /// <param name="data">Payload bytes.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    public static string Eps(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders PDF417 as ASCII from bytes.
    /// </summary>
    public static string Ascii(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }
}
