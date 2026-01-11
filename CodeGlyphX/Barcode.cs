using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX;

/// <summary>
/// Simple barcode helpers with fluent and static APIs.
/// </summary>
public static class Barcode {
    /// <summary>
    /// Starts a fluent barcode builder.
    /// </summary>
    public static BarcodeBuilder Create(BarcodeType type, string content, BarcodeOptions? options = null) {
        return new BarcodeBuilder(type, content, options);
    }

    /// <summary>
    /// Encodes a 1D barcode.
    /// </summary>
    public static Barcode1D Encode(BarcodeType type, string content) {
        return BarcodeEncoder.Encode(type, content);
    }

    /// <summary>
    /// Renders a barcode as PNG.
    /// </summary>
    public static byte[] Png(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePngRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as SVG.
    /// </summary>
    public static string Svg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        return SvgBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as HTML.
    /// </summary>
    public static string Html(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildHtmlOptions(options);
        return HtmlBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as JPEG.
    /// </summary>
    public static byte[] Jpeg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        return BarcodeJpegRenderer.Render(barcode, opts, quality);
    }

    /// <summary>
    /// Renders a barcode as PNG and writes it to a file.
    /// </summary>
    public static string SavePng(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var png = Png(type, content, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PNG and writes it to a stream.
    /// </summary>
    public static void SavePng(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePngRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a file.
    /// </summary>
    public static string SaveSvg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a stream.
    /// </summary>
    public static void SaveSvg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a file.
    /// </summary>
    public static string SaveHtml(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a stream.
    /// </summary>
    public static void SaveHtml(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a file.
    /// </summary>
    public static string SaveJpeg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var jpeg = Jpeg(type, content, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a stream.
    /// </summary>
    public static void SaveJpeg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        BarcodeJpegRenderer.RenderToStream(barcode, opts, stream, quality);
    }

    /// <summary>
    /// Saves a barcode to a file based on the file extension (.png/.svg/.html/.jpg).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(type, content, path, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(type, content, path, options);
            case ".svg":
                return SaveSvg(type, content, path, options);
            case ".html":
            case ".htm":
                return SaveHtml(type, content, path, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(type, content, path, options);
            default:
                // Fallback to PNG for unknown extensions to keep the API forgiving.
                return SavePng(type, content, path, options);
        }
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out BarcodeDecoded decoded) {
        return TryDecodePng(png, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint.
    /// </summary>
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Decodes a barcode from PNG bytes.
    /// </summary>
    public static BarcodeDecoded DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var decoded)) {
            throw new FormatException("PNG does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG file.
    /// </summary>
    public static BarcodeDecoded DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var decoded)) {
            throw new FormatException("PNG file does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG stream.
    /// </summary>
    public static BarcodeDecoded DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var decoded)) {
            throw new FormatException("PNG stream does not contain a decodable barcode.");
        }
        return decoded;
    }

    private static BarcodePngRenderOptions BuildPngOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodePngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            Foreground = opts.Foreground,
            Background = opts.Background,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = opts.LabelColor,
        };
    }

    private static BarcodeSvgRenderOptions BuildSvgOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

    private static BarcodeHtmlRenderOptions BuildHtmlOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

    /// <summary>
    /// Fluent barcode builder.
    /// </summary>
    public sealed class BarcodeBuilder {
        private readonly BarcodeType _type;
        private readonly string _content;

        /// <summary>
        /// Rendering options used by this builder.
        /// </summary>
        public BarcodeOptions Options { get; }

        internal BarcodeBuilder(BarcodeType type, string content, BarcodeOptions? options) {
            _type = type;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Options = options ?? new BarcodeOptions();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public BarcodeBuilder WithOptions(Action<BarcodeOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Options);
            return this;
        }

        /// <summary>
        /// Sets module size.
        /// </summary>
        public BarcodeBuilder WithModuleSize(int moduleSize) {
            Options.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size.
        /// </summary>
        public BarcodeBuilder WithQuietZone(int quietZone) {
            Options.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets barcode height in modules.
        /// </summary>
        public BarcodeBuilder WithHeight(int heightModules) {
            Options.HeightModules = heightModules;
            return this;
        }

        /// <summary>
        /// Sets foreground and background colors.
        /// </summary>
        public BarcodeBuilder WithColors(Rgba32 foreground, Rgba32 background) {
            Options.Foreground = foreground;
            Options.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality.
        /// </summary>
        public BarcodeBuilder WithJpegQuality(int quality) {
            Options.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Sets label text rendered under bars.
        /// </summary>
        public BarcodeBuilder WithLabel(string? text) {
            Options.LabelText = text;
            return this;
        }

        /// <summary>
        /// Sets label font size in pixels.
        /// </summary>
        public BarcodeBuilder WithLabelFontSize(int fontSizePx) {
            Options.LabelFontSize = fontSizePx;
            return this;
        }

        /// <summary>
        /// Sets label margin in pixels.
        /// </summary>
        public BarcodeBuilder WithLabelMargin(int marginPx) {
            Options.LabelMargin = marginPx;
            return this;
        }

        /// <summary>
        /// Sets label color.
        /// </summary>
        public BarcodeBuilder WithLabelColor(Rgba32 color) {
            Options.LabelColor = color;
            return this;
        }

        /// <summary>
        /// Sets label font family (SVG/HTML).
        /// </summary>
        public BarcodeBuilder WithLabelFontFamily(string fontFamily) {
            Options.LabelFontFamily = fontFamily;
            return this;
        }

        /// <summary>
        /// Encodes the barcode.
        /// </summary>
        public Barcode1D Encode() => Barcode.Encode(_type, _content);

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Barcode.Png(_type, _content, Options);

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Barcode.Svg(_type, _content, Options);

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Barcode.Html(_type, _content, Options);

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Barcode.Jpeg(_type, _content, Options);

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => Barcode.SavePng(_type, _content, path, Options);

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => Barcode.SavePng(_type, _content, stream, Options);

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => Barcode.SaveSvg(_type, _content, path, Options);

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => Barcode.SaveSvg(_type, _content, stream, Options);

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) => Barcode.SaveHtml(_type, _content, path, Options, title);

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) => Barcode.SaveHtml(_type, _content, stream, Options, title);

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => Barcode.SaveJpeg(_type, _content, path, Options);

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => Barcode.SaveJpeg(_type, _content, stream, Options);

        /// <summary>
        /// Saves based on file extension (.png/.svg/.html/.jpg). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) => Barcode.Save(_type, _content, path, Options, title);
    }
}
