using System;
using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simple QR helpers with fluent and static APIs.
/// </summary>
public static class QR {
    /// <summary>
    /// Starts a fluent QR builder for plain text.
    /// </summary>
    public static QrBuilder Create(string payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Starts a fluent QR builder for a payload with embedded defaults.
    /// </summary>
    public static QrBuilder Create(QrPayloadData payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Encodes a payload into a QR code.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Encodes a payload with embedded defaults into a QR code.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] Png(string payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Renders a QR code as PNG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Png(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string Svg(string payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Renders a QR code as SVG for a payload with embedded defaults.
    /// </summary>
    public static string Svg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Renders a QR code as HTML.
    /// </summary>
    public static string Html(string payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Renders a QR code as HTML for a payload with embedded defaults.
    /// </summary>
    public static string Html(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Renders a QR code as JPEG.
    /// </summary>
    public static byte[] Jpeg(string payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Renders a QR code as JPEG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Jpeg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Saves a PNG QR to a file.
    /// </summary>
    public static string SavePng(string payload, string path, QrEasyOptions? options = null) {
        return Png(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PNG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePng(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Png(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PNG QR to a stream.
    /// </summary>
    public static void SavePng(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPngToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a PNG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePng(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPngToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves an SVG QR to a file.
    /// </summary>
    public static string SaveSvg(string payload, string path, QrEasyOptions? options = null) {
        return Svg(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an SVG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveSvg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Svg(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an SVG QR to a stream.
    /// </summary>
    public static void SaveSvg(string payload, Stream stream, QrEasyOptions? options = null) {
        Svg(payload, options).WriteText(stream);
    }

    /// <summary>
    /// Saves an SVG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveSvg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        Svg(payload, options).WriteText(stream);
    }

    /// <summary>
    /// Saves an HTML QR to a file.
    /// </summary>
    public static string SaveHtml(string payload, string path, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves an HTML QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveHtml(QrPayloadData payload, string path, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves an HTML QR to a stream.
    /// </summary>
    public static void SaveHtml(string payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves an HTML QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveHtml(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves a JPEG QR to a file.
    /// </summary>
    public static string SaveJpeg(string payload, string path, QrEasyOptions? options = null) {
        return Jpeg(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a JPEG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveJpeg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Jpeg(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a JPEG QR to a stream.
    /// </summary>
    public static void SaveJpeg(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderJpegToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a JPEG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveJpeg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderJpegToStream(payload, stream, options);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out QrDecoded decoded) {
        decoded = null!;
        if (png is null) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out QrDecoded decoded) {
        decoded = null!;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodePng(data, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out QrDecoded decoded) {
        decoded = null!;
        if (stream is null) return false;
        var data = stream.ReadBinary();
        return TryDecodePng(data, out decoded);
    }

    /// <summary>
    /// Decodes a QR code from a PNG byte array.
    /// </summary>
    public static QrDecoded DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR code from a PNG file.
    /// </summary>
    public static QrDecoded DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR code from a PNG stream.
    /// </summary>
    public static QrDecoded DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }

    /// <summary>
    /// Fluent QR builder.
    /// </summary>
    public sealed class QrBuilder {
        private readonly string _payload;
        private readonly QrPayloadData? _payloadData;

        /// <summary>
        /// Rendering options used by this builder.
        /// </summary>
        public QrEasyOptions Options { get; }

        internal QrBuilder(string payload, QrEasyOptions? options) {
            _payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Options = options ?? new QrEasyOptions();
        }

        internal QrBuilder(QrPayloadData payload, QrEasyOptions? options) {
            _payloadData = payload ?? throw new ArgumentNullException(nameof(payload));
            _payload = payload.Text;
            Options = options ?? new QrEasyOptions();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public QrBuilder WithOptions(Action<QrEasyOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Options);
            return this;
        }

        /// <summary>
        /// Sets the module size in pixels.
        /// </summary>
        public QrBuilder WithModuleSize(int moduleSize) {
            Options.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets the quiet zone size in modules.
        /// </summary>
        public QrBuilder WithQuietZone(int quietZone) {
            Options.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets foreground and background colors.
        /// </summary>
        public QrBuilder WithColors(Rgba32 foreground, Rgba32 background) {
            Options.Foreground = foreground;
            Options.Background = background;
            return this;
        }

        /// <summary>
        /// Sets the render style preset.
        /// </summary>
        public QrBuilder WithStyle(QrRenderStyle style) {
            Options.Style = style;
            return this;
        }

        /// <summary>
        /// Sets an embedded logo from PNG bytes.
        /// </summary>
        public QrBuilder WithLogoPng(byte[] png) {
            Options.LogoPng = png;
            return this;
        }

        /// <summary>
        /// Sets an embedded logo from a PNG file.
        /// </summary>
        public QrBuilder WithLogoFile(string path) {
            Options.LogoPng = RenderIO.ReadBinary(path);
            return this;
        }

        /// <summary>
        /// Sets error correction level.
        /// </summary>
        public QrBuilder WithErrorCorrection(QrErrorCorrectionLevel ecc) {
            Options.ErrorCorrectionLevel = ecc;
            return this;
        }

        /// <summary>
        /// Encodes the QR code.
        /// </summary>
        public QrCode Encode() => _payloadData is null ? QrEasy.Encode(_payload, Options) : QrEasy.Encode(_payloadData, Options);

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => _payloadData is null ? QrEasy.RenderPng(_payload, Options) : QrEasy.RenderPng(_payloadData, Options);

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => _payloadData is null ? QrEasy.RenderSvg(_payload, Options) : QrEasy.RenderSvg(_payloadData, Options);

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => _payloadData is null ? QrEasy.RenderHtml(_payload, Options) : QrEasy.RenderHtml(_payloadData, Options);

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => _payloadData is null ? QrEasy.RenderJpeg(_payload, Options) : QrEasy.RenderJpeg(_payloadData, Options);

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => _payloadData is null ? QR.SavePng(_payload, path, Options) : QR.SavePng(_payloadData, path, Options);

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) {
            if (_payloadData is null) {
                QR.SavePng(_payload, stream, Options);
            } else {
                QR.SavePng(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => _payloadData is null ? QR.SaveSvg(_payload, path, Options) : QR.SaveSvg(_payloadData, path, Options);

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) {
            if (_payloadData is null) {
                QR.SaveSvg(_payload, stream, Options);
            } else {
                QR.SaveSvg(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) => _payloadData is null ? QR.SaveHtml(_payload, path, Options, title) : QR.SaveHtml(_payloadData, path, Options, title);

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            if (_payloadData is null) {
                QR.SaveHtml(_payload, stream, Options, title);
            } else {
                QR.SaveHtml(_payloadData, stream, Options, title);
            }
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => _payloadData is null ? QR.SaveJpeg(_payload, path, Options) : QR.SaveJpeg(_payloadData, path, Options);

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) {
            if (_payloadData is null) {
                QR.SaveJpeg(_payload, stream, Options);
            } else {
                QR.SaveJpeg(_payloadData, stream, Options);
            }
        }
    }
}
