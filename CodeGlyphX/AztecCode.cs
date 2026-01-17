using System;
using System.IO;
using CodeGlyphX.Aztec;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX;

/// <summary>
/// Aztec code helpers.
/// </summary>
public static class AztecCode {
    /// <summary>
    /// Encodes a text payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        return AztecEncoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes binary payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(ReadOnlySpan<byte> data, AztecEncodeOptions? options = null) {
        if (data.Length == 0) return AztecEncoder.Encode(Array.Empty<byte>(), options?.ErrorCorrectionPercent ?? 33, 0).Matrix;
        var bytes = data.ToArray();
        var eccPercent = options?.ErrorCorrectionPercent ?? 33;
        var userSpecifiedLayers = 0;
        if (options?.Layers is int layers && layers > 0) {
            var compact = options.Compact ?? layers <= 4;
            userSpecifiedLayers = compact ? -layers : layers;
        }
        return AztecEncoder.Encode(bytes, eccPercent, userSpecifiedLayers).Matrix;
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return AztecDecoder.TryDecode(modules, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value);
    }
#endif

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out text);
    }

    /// <summary>
    /// Decodes an Aztec symbol from PNG bytes.
    /// </summary>
    public static string DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var text)) {
            throw new FormatException("PNG does not contain a decodable Aztec symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes an Aztec symbol from a PNG file.
    /// </summary>
    public static string DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var text)) {
            throw new FormatException("PNG file does not contain a decodable Aztec symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes an Aztec symbol from a PNG stream.
    /// </summary>
    public static string DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var text)) {
            throw new FormatException("PNG stream does not contain a decodable Aztec symbol.");
        }
        return text;
    }

    /// <summary>
    /// Renders Aztec to PNG bytes.
    /// </summary>
    public static byte[] Png(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPngRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PNG bytes.
    /// </summary>
    public static byte[] Png(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPngRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVG markup.
    /// </summary>
    public static string Svg(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixSvgRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVG markup.
    /// </summary>
    public static string Svg(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixSvgRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to HTML markup.
    /// </summary>
    public static string Html(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, ToHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to HTML markup.
    /// </summary>
    public static string Html(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, ToHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to JPEG bytes.
    /// </summary>
    public static byte[] Jpeg(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return MatrixJpegRenderer.Render(modules, opts, renderOptions?.JpegQuality ?? 85);
    }

    /// <summary>
    /// Renders Aztec to JPEG bytes.
    /// </summary>
    public static byte[] Jpeg(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return MatrixJpegRenderer.Render(modules, opts, renderOptions?.JpegQuality ?? 85);
    }

    /// <summary>
    /// Renders Aztec to BMP bytes.
    /// </summary>
    public static byte[] Bmp(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixBmpRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to BMP bytes.
    /// </summary>
    public static byte[] Bmp(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixBmpRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PDF bytes.
    /// </summary>
    public static byte[] Pdf(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixPdfRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to PDF bytes.
    /// </summary>
    public static byte[] Pdf(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(data, encodeOptions);
        return MatrixPdfRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to EPS markup.
    /// </summary>
    public static string Eps(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixEpsRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to EPS markup.
    /// </summary>
    public static string Eps(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(data, encodeOptions);
        return MatrixEpsRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to ASCII text.
    /// </summary>
    public static string Ascii(string text, AztecEncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, renderOptions);
    }

    /// <summary>
    /// Renders Aztec to ASCII text.
    /// </summary>
    public static string Ascii(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, renderOptions);
    }

    /// <summary>
    /// Saves Aztec to a file based on extension (.png/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return RenderIO.WriteBinary(path, Png(text, encodeOptions, renderOptions));

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return RenderIO.WriteBinary(path, Png(text, encodeOptions, renderOptions));
            case ".svg":
                return RenderIO.WriteText(path, Svg(text, encodeOptions, renderOptions));
            case ".html":
            case ".htm":
            {
                var html = Html(text, encodeOptions, renderOptions);
                if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
                return RenderIO.WriteText(path, html);
            }
            case ".jpg":
            case ".jpeg":
                return RenderIO.WriteBinary(path, Jpeg(text, encodeOptions, renderOptions));
            case ".bmp":
                return RenderIO.WriteBinary(path, Bmp(text, encodeOptions, renderOptions));
            case ".pdf":
                return RenderIO.WriteBinary(path, Pdf(text, encodeOptions, renderOptions));
            case ".eps":
            case ".ps":
                return RenderIO.WriteText(path, Eps(text, encodeOptions, renderOptions));
            case ".txt":
                return RenderIO.WriteText(path, Ascii(text, encodeOptions));
            default:
                return RenderIO.WriteBinary(path, Png(text, encodeOptions, renderOptions));
        }
    }

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
    /// Saves Aztec SVG to a stream.
    /// </summary>
    public static void SaveSvg(string text, Stream stream, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        RenderIO.WriteText(stream, Svg(text, encodeOptions, renderOptions));
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

    private static MatrixPngRenderOptions ToPngOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixPngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }

    private static MatrixSvgRenderOptions ToSvgOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background)
        };
    }

    private static MatrixHtmlRenderOptions ToHtmlOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

    private static string ToCss(Rgba32 color) {
        if (color.A == 255) {
            return $"rgb({color.R},{color.G},{color.B})";
        }
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a:0.###})";
    }
}
