using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QR {
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
        /// Sets foreground color.
        /// </summary>
        public QrBuilder WithForeground(Rgba32 color) {
            Options.Foreground = color;
            return this;
        }

        /// <summary>
        /// Sets background color.
        /// </summary>
        public QrBuilder WithBackground(Rgba32 color) {
            Options.Background = color;
            return this;
        }

        /// <summary>
        /// Uses a transparent background (alpha = 0).
        /// </summary>
        public QrBuilder WithTransparentBackground() {
            Options.Background = Rgba32.Transparent;
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
        /// Sets module shape override.
        /// </summary>
        public QrBuilder WithModuleShape(QrPngModuleShape shape) {
            Options.ModuleShape = shape;
            return this;
        }

        /// <summary>
        /// Sets module scale override (0.1..1.0).
        /// </summary>
        public QrBuilder WithModuleScale(double scale) {
            Options.ModuleScale = scale;
            return this;
        }

        /// <summary>
        /// Sets module corner radius in pixels.
        /// </summary>
        public QrBuilder WithModuleCornerRadiusPx(int radiusPx) {
            Options.ModuleCornerRadiusPx = radiusPx;
            return this;
        }

        /// <summary>
        /// Sets the foreground gradient.
        /// </summary>
        public QrBuilder WithForegroundGradient(QrPngGradientOptions? gradient) {
            Options.ForegroundGradient = gradient;
            return this;
        }

        /// <summary>
        /// Sets eye (finder) styling.
        /// </summary>
        public QrBuilder WithEyes(QrPngEyeOptions? eyes) {
            Options.Eyes = eyes;
            return this;
        }

        /// <summary>
        /// Sets a fixed target size (in pixels). Module size is adjusted to fit.
        /// </summary>
        public QrBuilder WithTargetSize(int sizePx, bool includeQuietZone = true) {
            Options.TargetSizePx = sizePx;
            Options.TargetSizeIncludesQuietZone = includeQuietZone;
            return this;
        }

        /// <summary>
        /// Sets a fixed target size (in pixels). Module size is adjusted to fit.
        /// </summary>
        public QrBuilder WithFixedSize(int sizePx, bool includeQuietZone = true) => WithTargetSize(sizePx, includeQuietZone);

        /// <summary>
        /// Sets an embedded logo from PNG bytes.
        /// </summary>
        public QrBuilder WithLogoPng(byte[] png) {
            Options.LogoPng = png;
            return this;
        }

        /// <summary>
        /// Sets the logo scale relative to the QR area (excluding quiet zone).
        /// </summary>
        public QrBuilder WithLogoScale(double scale) {
            Options.LogoScale = scale;
            return this;
        }

        /// <summary>
        /// Sets the logo padding in pixels.
        /// </summary>
        public QrBuilder WithLogoPaddingPx(int paddingPx) {
            Options.LogoPaddingPx = paddingPx;
            return this;
        }

        /// <summary>
        /// Sets whether to draw a background plate behind the logo.
        /// </summary>
        public QrBuilder WithLogoBackground(bool enabled = true) {
            Options.LogoDrawBackground = enabled;
            return this;
        }

        /// <summary>
        /// Enables/disables auto-bumping the minimum version for logo background plates.
        /// </summary>
        public QrBuilder WithLogoBackgroundAutoBump(bool enabled = true) {
            Options.AutoBumpVersionForLogoBackground = enabled;
            return this;
        }

        /// <summary>
        /// Sets the minimum version used when a logo background plate is enabled.
        /// </summary>
        public QrBuilder WithLogoBackgroundMinVersion(int minVersion) {
            Options.LogoBackgroundMinVersion = minVersion;
            return this;
        }

        /// <summary>
        /// Sets the logo background color.
        /// </summary>
        public QrBuilder WithLogoBackgroundColor(Rgba32? color) {
            Options.LogoBackground = color;
            return this;
        }

        /// <summary>
        /// Sets the logo background corner radius in pixels.
        /// </summary>
        public QrBuilder WithLogoCornerRadiusPx(int radiusPx) {
            Options.LogoCornerRadiusPx = radiusPx;
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
        /// Sets ICO output sizes (in pixels).
        /// </summary>
        public QrBuilder WithIcoSizes(params int[] sizes) {
            Options.IcoSizes = sizes;
            return this;
        }

        /// <summary>
        /// Sets ICO aspect ratio preservation behavior.
        /// </summary>
        public QrBuilder WithIcoPreserveAspectRatio(bool enabled = true) {
            Options.IcoPreserveAspectRatio = enabled;
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
        /// Renders PPM bytes.
        /// </summary>
        public byte[] Ppm() => _payloadData is null ? QrEasy.RenderPpm(_payload, Options) : QrEasy.RenderPpm(_payloadData, Options);

        /// <summary>
        /// Renders PBM bytes.
        /// </summary>
        public byte[] Pbm() => _payloadData is null ? QrEasy.RenderPbm(_payload, Options) : QrEasy.RenderPbm(_payloadData, Options);

        /// <summary>
        /// Renders PGM bytes.
        /// </summary>
        public byte[] Pgm() => _payloadData is null ? QrEasy.RenderPgm(_payload, Options) : QrEasy.RenderPgm(_payloadData, Options);

        /// <summary>
        /// Renders PAM bytes.
        /// </summary>
        public byte[] Pam() => _payloadData is null ? QrEasy.RenderPam(_payload, Options) : QrEasy.RenderPam(_payloadData, Options);

        /// <summary>
        /// Renders XBM text.
        /// </summary>
        public string Xbm() => _payloadData is null ? QrEasy.RenderXbm(_payload, Options) : QrEasy.RenderXbm(_payloadData, Options);

        /// <summary>
        /// Renders XPM text.
        /// </summary>
        public string Xpm() => _payloadData is null ? QrEasy.RenderXpm(_payload, Options) : QrEasy.RenderXpm(_payloadData, Options);

        /// <summary>
        /// Renders TGA bytes.
        /// </summary>
        public byte[] Tga() => _payloadData is null ? QrEasy.RenderTga(_payload, Options) : QrEasy.RenderTga(_payloadData, Options);

        /// <summary>
        /// Renders ICO bytes.
        /// </summary>
        public byte[] Ico() => _payloadData is null ? QrEasy.RenderIco(_payload, Options) : QrEasy.RenderIco(_payloadData, Options);

        /// <summary>
        /// Renders SVGZ bytes.
        /// </summary>
        public byte[] Svgz() => _payloadData is null ? QrEasy.RenderSvgz(_payload, Options) : QrEasy.RenderSvgz(_payloadData, Options);

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode mode = RenderMode.Vector) => _payloadData is null ? QrEasy.RenderPdf(_payload, Options, mode) : QrEasy.RenderPdf(_payloadData, Options, mode);

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public string Eps(RenderMode mode = RenderMode.Vector) => _payloadData is null ? QrEasy.RenderEps(_payload, Options, mode) : QrEasy.RenderEps(_payloadData, Options, mode);

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
        /// Saves PPM to a file.
        /// </summary>
        public string SavePpm(string path) => _payloadData is null ? QR.SavePpm(_payload, path, Options) : QR.SavePpm(_payloadData, path, Options);

        /// <summary>
        /// Saves PPM to a stream.
        /// </summary>
        public void SavePpm(Stream stream) {
            if (_payloadData is null) {
                QR.SavePpm(_payload, stream, Options);
            } else {
                QR.SavePpm(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves PBM to a file.
        /// </summary>
        public string SavePbm(string path) => _payloadData is null ? QR.SavePbm(_payload, path, Options) : QR.SavePbm(_payloadData, path, Options);

        /// <summary>
        /// Saves PBM to a stream.
        /// </summary>
        public void SavePbm(Stream stream) {
            if (_payloadData is null) {
                QR.SavePbm(_payload, stream, Options);
            } else {
                QR.SavePbm(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves PGM to a file.
        /// </summary>
        public string SavePgm(string path) => _payloadData is null ? QR.SavePgm(_payload, path, Options) : QR.SavePgm(_payloadData, path, Options);

        /// <summary>
        /// Saves PGM to a stream.
        /// </summary>
        public void SavePgm(Stream stream) {
            if (_payloadData is null) {
                QR.SavePgm(_payload, stream, Options);
            } else {
                QR.SavePgm(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves PAM to a file.
        /// </summary>
        public string SavePam(string path) => _payloadData is null ? QR.SavePam(_payload, path, Options) : QR.SavePam(_payloadData, path, Options);

        /// <summary>
        /// Saves PAM to a stream.
        /// </summary>
        public void SavePam(Stream stream) {
            if (_payloadData is null) {
                QR.SavePam(_payload, stream, Options);
            } else {
                QR.SavePam(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves XBM to a file.
        /// </summary>
        public string SaveXbm(string path) => _payloadData is null ? QR.SaveXbm(_payload, path, Options) : QR.SaveXbm(_payloadData, path, Options);

        /// <summary>
        /// Saves XBM to a stream.
        /// </summary>
        public void SaveXbm(Stream stream) {
            if (_payloadData is null) {
                QR.SaveXbm(_payload, stream, Options);
            } else {
                QR.SaveXbm(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves XPM to a file.
        /// </summary>
        public string SaveXpm(string path) => _payloadData is null ? QR.SaveXpm(_payload, path, Options) : QR.SaveXpm(_payloadData, path, Options);

        /// <summary>
        /// Saves XPM to a stream.
        /// </summary>
        public void SaveXpm(Stream stream) {
            if (_payloadData is null) {
                QR.SaveXpm(_payload, stream, Options);
            } else {
                QR.SaveXpm(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves TGA to a file.
        /// </summary>
        public string SaveTga(string path) => _payloadData is null ? QR.SaveTga(_payload, path, Options) : QR.SaveTga(_payloadData, path, Options);

        /// <summary>
        /// Saves TGA to a stream.
        /// </summary>
        public void SaveTga(Stream stream) {
            if (_payloadData is null) {
                QR.SaveTga(_payload, stream, Options);
            } else {
                QR.SaveTga(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves ICO to a file.
        /// </summary>
        public string SaveIco(string path) => _payloadData is null ? QR.SaveIco(_payload, path, Options) : QR.SaveIco(_payloadData, path, Options);

        /// <summary>
        /// Saves ICO to a stream.
        /// </summary>
        public void SaveIco(Stream stream) {
            if (_payloadData is null) {
                QR.SaveIco(_payload, stream, Options);
            } else {
                QR.SaveIco(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves SVGZ to a file.
        /// </summary>
        public string SaveSvgz(string path) => _payloadData is null ? QR.SaveSvgz(_payload, path, Options) : QR.SaveSvgz(_payloadData, path, Options);

        /// <summary>
        /// Saves SVGZ to a stream.
        /// </summary>
        public void SaveSvgz(Stream stream) {
            if (_payloadData is null) {
                QR.SaveSvgz(_payload, stream, Options);
            } else {
                QR.SaveSvgz(_payloadData, stream, Options);
            }
        }

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode mode = RenderMode.Vector) => _payloadData is null ? QR.SavePdf(_payload, path, Options, mode) : QR.SavePdf(_payloadData, path, Options, mode);

        /// <summary>
        /// Saves PDF to a stream.
        /// </summary>
    /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SavePdf(Stream stream, RenderMode mode = RenderMode.Vector) {
            if (_payloadData is null) {
                QR.SavePdf(_payload, stream, Options, mode);
            } else {
                QR.SavePdf(_payloadData, stream, Options, mode);
            }
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode mode = RenderMode.Vector) => _payloadData is null ? QR.SaveEps(_payload, path, Options, mode) : QR.SaveEps(_payloadData, path, Options, mode);

        /// <summary>
        /// Saves EPS to a stream.
        /// </summary>
    /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SaveEps(Stream stream, RenderMode mode = RenderMode.Vector) {
            if (_payloadData is null) {
                QR.SaveEps(_payload, stream, Options, mode);
            } else {
                QR.SaveEps(_payloadData, stream, Options, mode);
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
        /// Saves based on file extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) => _payloadData is null
            ? QR.Save(_payload, path, Options, title)
            : QR.Save(_payloadData, path, Options, title);
    }
}
