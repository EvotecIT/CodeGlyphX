using System;
using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
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
    /// Detects a payload type and encodes it into a QR code.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.EncodeAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults into a QR code.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] Png(string payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PNG.
    /// </summary>
    public static byte[] PngAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPngAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PNG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Png(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPng(payload, options);

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string Svg(string payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as SVG.
    /// </summary>
    public static string SvgAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderSvgAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as SVG for a payload with embedded defaults.
    /// </summary>
    public static string Svg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderSvg(payload, options);

    /// <summary>
    /// Renders a QR code as HTML.
    /// </summary>
    public static string Html(string payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as HTML.
    /// </summary>
    public static string HtmlAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderHtmlAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as HTML for a payload with embedded defaults.
    /// </summary>
    public static string Html(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderHtml(payload, options);

    /// <summary>
    /// Renders a QR code as JPEG.
    /// </summary>
    public static byte[] Jpeg(string payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as JPEG.
    /// </summary>
    public static byte[] JpegAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderJpegAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as JPEG for a payload with embedded defaults.
    /// </summary>
    public static byte[] Jpeg(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderJpeg(payload, options);

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    public static byte[] Bmp(string payload, QrEasyOptions? options = null) => QrEasy.RenderBmp(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as BMP.
    /// </summary>
    public static byte[] BmpAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderBmpAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as BMP for a payload with embedded defaults.
    /// </summary>
    public static byte[] Bmp(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderBmp(payload, options);

    /// <summary>
    /// Renders a QR code as PDF.
    /// </summary>
    public static byte[] Pdf(string payload, QrEasyOptions? options = null) => QrEasy.RenderPdf(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as PDF.
    /// </summary>
    public static byte[] PdfAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderPdfAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as PDF for a payload with embedded defaults.
    /// </summary>
    public static byte[] Pdf(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderPdf(payload, options);

    /// <summary>
    /// Renders a QR code as EPS.
    /// </summary>
    public static string Eps(string payload, QrEasyOptions? options = null) => QrEasy.RenderEps(payload, options);

    /// <summary>
    /// Detects a payload type and renders a QR code as EPS.
    /// </summary>
    public static string EpsAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderEpsAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Renders a QR code as EPS for a payload with embedded defaults.
    /// </summary>
    public static string Eps(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.RenderEps(payload, options);

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    public static string Ascii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAscii(payload, asciiOptions, options);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as ASCII text.
    /// </summary>
    public static string AsciiAuto(string payload, QrPayloadDetectOptions? detectOptions = null, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAsciiAuto(payload, detectOptions, asciiOptions, options);
    }

    /// <summary>
    /// Renders a QR code as ASCII text for a payload with embedded defaults.
    /// </summary>
    public static string Ascii(QrPayloadData payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        return QrEasy.RenderAscii(payload, asciiOptions, options);
    }

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
    /// Saves a BMP QR to a file.
    /// </summary>
    public static string SaveBmp(string payload, string path, QrEasyOptions? options = null) {
        return Bmp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a BMP QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveBmp(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Bmp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a BMP QR to a stream.
    /// </summary>
    public static void SaveBmp(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderBmpToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a BMP QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveBmp(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderBmpToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a PDF QR to a file.
    /// </summary>
    public static string SavePdf(string payload, string path, QrEasyOptions? options = null) {
        return Pdf(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PDF QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePdf(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Pdf(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PDF QR to a stream.
    /// </summary>
    public static void SavePdf(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPdfToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a PDF QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePdf(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPdfToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves an EPS QR to a file.
    /// </summary>
    public static string SaveEps(string payload, string path, QrEasyOptions? options = null) {
        return Eps(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an EPS QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveEps(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Eps(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an EPS QR to a stream.
    /// </summary>
    public static void SaveEps(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderEpsToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves an EPS QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveEps(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderEpsToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension (.png/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string payload, string path, QrEasyOptions? options = null, string? title = null) {
        return SaveByExtension(path, payload, null, options, title);
    }

    /// <summary>
    /// Detects a payload type and saves a QR code to a file based on the file extension (.png/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveAuto(string payload, string path, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, string? title = null) {
        var detected = QrPayloads.Detect(payload, detectOptions);
        return SaveByExtension(path, detected.Text, detected, options, title);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension (.png/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(QrPayloadData payload, string path, QrEasyOptions? options = null, string? title = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        return SaveByExtension(path, payload.Text, payload, options, title);
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
    /// Attempts to decode all QR codes from a PNG byte array.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (png is null) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
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
    /// Attempts to decode all QR codes from a PNG file.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, out decoded);
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
    /// Attempts to parse a raw QR payload into a structured representation.
    /// </summary>
    public static bool TryParsePayload(string payload, out QrParsedPayload parsed) {
        return QrPayloadParser.TryParse(payload, out parsed);
    }

    /// <summary>
    /// Parses a raw QR payload into a structured representation.
    /// </summary>
    public static QrParsedPayload ParsePayload(string payload) {
        return QrPayloadParser.Parse(payload);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (stream is null) return false;
        var data = stream.ReadBinary();
        return TryDecodeAllPng(data, out decoded);
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
        /// Renders PDF bytes.
        /// </summary>
        public byte[] Pdf() => _payloadData is null ? QrEasy.RenderPdf(_payload, Options) : QrEasy.RenderPdf(_payloadData, Options);

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        public string Eps() => _payloadData is null ? QrEasy.RenderEps(_payload, Options) : QrEasy.RenderEps(_payloadData, Options);

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

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) => _payloadData is null ? QR.SaveBmp(_payload, path, Options) : QR.SaveBmp(_payloadData, path, Options);

        /// <summary>
        /// Saves BMP to a stream.
        /// </summary>
        public void SaveBmp(Stream stream) {
            if (_payloadData is null) {
                QR.SaveBmp(_payload, stream, Options);
            } else {
                QR.SaveBmp(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
        public string SavePdf(string path) => _payloadData is null ? QR.SavePdf(_payload, path, Options) : QR.SavePdf(_payloadData, path, Options);

        /// <summary>
        /// Saves PDF to a stream.
        /// </summary>
        public void SavePdf(Stream stream) {
            if (_payloadData is null) {
                QR.SavePdf(_payload, stream, Options);
            } else {
                QR.SavePdf(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
        public string SaveEps(string path) => _payloadData is null ? QR.SaveEps(_payload, path, Options) : QR.SaveEps(_payloadData, path, Options);

        /// <summary>
        /// Saves EPS to a stream.
        /// </summary>
        public void SaveEps(Stream stream) {
            if (_payloadData is null) {
                QR.SaveEps(_payload, stream, Options);
            } else {
                QR.SaveEps(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? asciiOptions = null) => _payloadData is null
            ? QR.Ascii(_payload, asciiOptions, Options)
            : QR.Ascii(_payloadData, asciiOptions, Options);

        /// <summary>
        /// Saves ASCII to a file.
        /// </summary>
        public string SaveAscii(string path, MatrixAsciiRenderOptions? asciiOptions = null) => Ascii(asciiOptions).WriteText(path);

        /// <summary>
        /// Saves based on file extension (.png/.svg/.html/.jpg/.bmp/.pdf/.eps). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) => _payloadData is null
            ? QR.Save(_payload, path, Options, title)
            : QR.Save(_payloadData, path, Options, title);
    }

    private static string SaveByExtension(string path, string payload, QrPayloadData? payloadData, QrEasyOptions? options, string? title) {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) {
            return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
        }

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
            case ".svg":
                return payloadData is null ? SaveSvg(payload, path, options) : SaveSvg(payloadData, path, options);
            case ".html":
            case ".htm":
                return payloadData is null ? SaveHtml(payload, path, options, title) : SaveHtml(payloadData, path, options, title);
            case ".jpg":
            case ".jpeg":
                return payloadData is null ? SaveJpeg(payload, path, options) : SaveJpeg(payloadData, path, options);
            case ".bmp":
                return payloadData is null ? SaveBmp(payload, path, options) : SaveBmp(payloadData, path, options);
            case ".pdf":
                return payloadData is null ? SavePdf(payload, path, options) : SavePdf(payloadData, path, options);
            case ".eps":
            case ".ps":
                return payloadData is null ? SaveEps(payload, path, options) : SaveEps(payloadData, path, options);
            default:
                // Fallback to PNG for unknown extensions to keep the API forgiving.
                return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
        }
    }
}
