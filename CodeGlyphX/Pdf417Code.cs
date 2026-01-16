using System;
using System.IO;
using System.Text;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX;

/// <summary>
/// Simple PDF417 helpers with fluent and static APIs.
/// </summary>
public static class Pdf417Code {
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
    /// Renders PDF417 as ASCII from bytes.
    /// </summary>
    public static string Ascii(ReadOnlySpan<byte> data, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }

    /// <summary>
    /// Saves PDF417 PNG to a file for byte payloads.
    /// </summary>
    public static string SavePng(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var png = Png(data, encodeOptions, renderOptions);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 PNG to a stream for byte payloads.
    /// </summary>
    public static void SavePng(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 HTML to a file for byte payloads.
    /// </summary>
    public static string SaveHtml(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 HTML to a stream for byte payloads.
    /// </summary>
    public static void SaveHtml(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(data, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(data, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(ReadOnlySpan<byte> data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.svg/.html/.jpg/.bmp).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(data, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(data, path, encodeOptions, renderOptions);
            default:
                return SavePng(data, path, encodeOptions, renderOptions);
        }
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
    /// Renders PDF417 as SVG from bytes.
    /// </summary>
    public static string Svg(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions));
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
    /// Renders PDF417 as ASCII from bytes.
    /// </summary>
    public static string Ascii(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }

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
    /// Saves PDF417 PNG to a stream.
    /// </summary>
    public static void SavePng(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 PNG to a stream for byte payloads.
    /// </summary>
    public static void SavePng(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixPngRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 SVG to a file.
    /// </summary>
    public static string SaveSvg(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(text, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves PDF417 SVG to a stream.
    /// </summary>
    public static void SaveSvg(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(text, encodeOptions, renderOptions);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var svg = Svg(data, encodeOptions, renderOptions);
        svg.WriteText(stream);
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
    /// Saves PDF417 HTML to a stream.
    /// </summary>
    public static void SaveHtml(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(text, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 HTML to a stream for byte payloads.
    /// </summary>
    public static void SaveHtml(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var html = Html(data, encodeOptions, renderOptions);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a file.
    /// </summary>
    public static string SaveJpeg(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(text, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file.
    /// </summary>
    public static string SaveBmp(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(text, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var jpeg = Jpeg(data, encodeOptions, renderOptions);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 BMP to a file for byte payloads.
    /// </summary>
    public static string SaveBmp(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var bmp = Bmp(data, encodeOptions, renderOptions);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream.
    /// </summary>
    public static void SaveJpeg(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream.
    /// </summary>
    public static void SaveBmp(string text, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves PDF417 BMP to a stream for byte payloads.
    /// </summary>
    public static void SaveBmp(byte[] data, Stream stream, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeBytes(data, encodeOptions);
        MatrixBmpRenderer.RenderToStream(modules, BuildPngOptions(renderOptions), stream);
    }

    /// <summary>
    /// Saves PDF417 to a file based on extension (.png/.svg/.html/.jpg/.bmp).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(text, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(text, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(text, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(text, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(text, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(text, path, encodeOptions, renderOptions);
            default:
                return SavePng(text, path, encodeOptions, renderOptions);
        }
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.svg/.html/.jpg/.bmp).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(data, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(data, path, encodeOptions, renderOptions);
            default:
                return SavePng(data, path, encodeOptions, renderOptions);
        }
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out text);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out text);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out text);
    }

    /// <summary>
    /// Decodes a PDF417 symbol from PNG bytes.
    /// </summary>
    public static string DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var text)) {
            throw new FormatException("PNG does not contain a decodable PDF417 symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a PDF417 symbol from a PNG file.
    /// </summary>
    public static string DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var text)) {
            throw new FormatException("PNG file does not contain a decodable PDF417 symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a PDF417 symbol from a PNG stream.
    /// </summary>
    public static string DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var text)) {
            throw new FormatException("PNG stream does not contain a decodable PDF417 symbol.");
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
    /// Fluent PDF417 builder.
    /// </summary>
    public sealed class Pdf417Builder {
        private readonly string? _text;
        private readonly byte[]? _bytes;
        private readonly Pdf417EncodeOptions _encodeOptions;
        private readonly MatrixOptions _renderOptions;

        internal Pdf417Builder(string text, Pdf417EncodeOptions? encodeOptions, MatrixOptions? renderOptions) {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _encodeOptions = encodeOptions ?? new Pdf417EncodeOptions();
            _renderOptions = renderOptions ?? new MatrixOptions();
        }

        internal Pdf417Builder(byte[] data, Pdf417EncodeOptions? encodeOptions, MatrixOptions? renderOptions) {
            _bytes = data ?? throw new ArgumentNullException(nameof(data));
            _encodeOptions = encodeOptions ?? new Pdf417EncodeOptions();
            _renderOptions = renderOptions ?? new MatrixOptions();
        }

        /// <summary>
        /// Mutates PDF417 encoding options.
        /// </summary>
        public Pdf417Builder WithEncodeOptions(Action<Pdf417EncodeOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_encodeOptions);
            return this;
        }

        /// <summary>
        /// Mutates rendering options.
        /// </summary>
        public Pdf417Builder WithRenderOptions(Action<MatrixOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_renderOptions);
            return this;
        }

        /// <summary>
        /// Sets module size in pixels.
        /// </summary>
        public Pdf417Builder WithModuleSize(int moduleSize) {
            _renderOptions.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size in modules.
        /// </summary>
        public Pdf417Builder WithQuietZone(int quietZone) {
            _renderOptions.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets foreground/background colors.
        /// </summary>
        public Pdf417Builder WithColors(Rgba32 foreground, Rgba32 background) {
            _renderOptions.Foreground = foreground;
            _renderOptions.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality (1..100).
        /// </summary>
        public Pdf417Builder WithJpegQuality(int quality) {
            _renderOptions.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Enables HTML email-safe table rendering.
        /// </summary>
        public Pdf417Builder WithHtmlEmailSafeTable(bool enabled = true) {
            _renderOptions.HtmlEmailSafeTable = enabled;
            return this;
        }

        /// <summary>
        /// Sets compaction mode.
        /// </summary>
        public Pdf417Builder WithCompaction(Pdf417Compaction compaction) {
            _encodeOptions.Compaction = compaction;
            return this;
        }

        /// <summary>
        /// Sets error correction level (0..8 or -1 for auto).
        /// </summary>
        public Pdf417Builder WithErrorCorrection(int level) {
            _encodeOptions.ErrorCorrectionLevel = level;
            return this;
        }

        /// <summary>
        /// Sets text encoding for byte compaction.
        /// </summary>
        public Pdf417Builder WithTextEncoding(Encoding encoding) {
            _encodeOptions.TextEncoding = encoding;
            return this;
        }

        /// <summary>
        /// Sets target aspect ratio (width/height).
        /// </summary>
        public Pdf417Builder WithAspectRatio(float ratio) {
            _encodeOptions.TargetAspectRatio = ratio;
            return this;
        }

        /// <summary>
        /// Sets column constraints.
        /// </summary>
        public Pdf417Builder WithColumns(int minColumns, int maxColumns) {
            _encodeOptions.MinColumns = minColumns;
            _encodeOptions.MaxColumns = maxColumns;
            return this;
        }

        /// <summary>
        /// Sets row constraints.
        /// </summary>
        public Pdf417Builder WithRows(int minRows, int maxRows) {
            _encodeOptions.MinRows = minRows;
            _encodeOptions.MaxRows = maxRows;
            return this;
        }

        /// <summary>
        /// Enables compact PDF417.
        /// </summary>
        public Pdf417Builder WithCompact(bool compact = true) {
            _encodeOptions.Compact = compact;
            return this;
        }

        /// <summary>
        /// Encodes the PDF417 as a module matrix.
        /// </summary>
        public BitMatrix Encode() {
            return _text is not null ? Pdf417Code.Encode(_text, _encodeOptions) : Pdf417Code.EncodeBytes(_bytes!, _encodeOptions);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() {
            return _text is not null ? Pdf417Code.Png(_text, _encodeOptions, _renderOptions) : Pdf417Code.Png(_bytes!, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Renders SVG markup.
        /// </summary>
        public string Svg() {
            return _text is not null ? Pdf417Code.Svg(_text, _encodeOptions, _renderOptions) : Pdf417Code.Svg(_bytes!, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Renders HTML markup.
        /// </summary>
        public string Html() {
            return _text is not null ? Pdf417Code.Html(_text, _encodeOptions, _renderOptions) : Pdf417Code.Html(_bytes!, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() {
            return _text is not null ? Pdf417Code.Jpeg(_text, _encodeOptions, _renderOptions) : Pdf417Code.Jpeg(_bytes!, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() {
            return _text is not null ? Pdf417Code.Bmp(_text, _encodeOptions, _renderOptions) : Pdf417Code.Bmp(_bytes!, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? options = null) {
            return _text is not null ? Pdf417Code.Ascii(_text, _encodeOptions, options) : Pdf417Code.Ascii(_bytes!, _encodeOptions, options);
        }

        /// <summary>
        /// Saves output based on file extension.
        /// </summary>
        public string Save(string path, string? title = null) {
            if (_text is not null) return Pdf417Code.Save(_text, path, _encodeOptions, _renderOptions, title);
            return Pdf417Code.Save(_bytes!, path, _encodeOptions, _renderOptions, title);
        }

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) {
            return _text is not null ? Pdf417Code.SavePng(_text, path, _encodeOptions, _renderOptions) : Pdf417Code.SavePng(_bytes!, path, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) {
            return _text is not null ? Pdf417Code.SaveSvg(_text, path, _encodeOptions, _renderOptions) : Pdf417Code.SaveSvg(_bytes!, path, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            return _text is not null ? Pdf417Code.SaveHtml(_text, path, _encodeOptions, _renderOptions, title) : Pdf417Code.SaveHtml(_bytes!, path, _encodeOptions, _renderOptions, title);
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) {
            return _text is not null ? Pdf417Code.SaveJpeg(_text, path, _encodeOptions, _renderOptions) : Pdf417Code.SaveJpeg(_bytes!, path, _encodeOptions, _renderOptions);
        }

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) {
            return _text is not null ? Pdf417Code.SaveBmp(_text, path, _encodeOptions, _renderOptions) : Pdf417Code.SaveBmp(_bytes!, path, _encodeOptions, _renderOptions);
        }
    }
}
