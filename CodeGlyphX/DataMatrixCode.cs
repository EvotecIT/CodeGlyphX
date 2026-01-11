using System;
using System.IO;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX;

/// <summary>
/// Simple Data Matrix helpers with fluent and static APIs.
/// </summary>
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
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(ReadOnlySpan<byte> data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.svg/.html/.jpg).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
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
    /// Renders Data Matrix as SVG from bytes.
    /// </summary>
    public static string Svg(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(options));
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
    /// Renders Data Matrix as JPEG from bytes.
    /// </summary>
    public static byte[] Jpeg(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
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
    /// Saves Data Matrix SVG to a file for byte payloads.
    /// </summary>
    public static string SaveSvg(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream.
    /// </summary>
    public static void SaveSvg(string text, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(text, mode, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Saves Data Matrix SVG to a stream for byte payloads.
    /// </summary>
    public static void SaveSvg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var svg = Svg(data, mode, options);
        svg.WriteText(stream);
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
    /// Saves Data Matrix JPEG to a file for byte payloads.
    /// </summary>
    public static string SaveJpeg(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var jpeg = Jpeg(data, mode, options);
        return jpeg.WriteBinary(path);
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
    /// Saves Data Matrix JPEG to a stream for byte payloads.
    /// </summary>
    public static void SaveJpeg(byte[] data, Stream stream, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        var modules = EncodeBytes(data, mode);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 85;
        MatrixJpegRenderer.RenderToStream(modules, opts, stream, quality);
    }

    /// <summary>
    /// Saves Data Matrix to a file based on extension (.png/.svg/.html/.jpg).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
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
            default:
                return SavePng(text, path, mode, options);
        }
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.svg/.html/.jpg).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
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
            default:
                return SavePng(data, path, mode, options);
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out text);
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
    }
}
