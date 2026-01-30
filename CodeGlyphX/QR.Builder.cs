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
        /// Sets module scale map.
        /// </summary>
        public QrBuilder WithModuleScaleMap(QrPngModuleScaleMapOptions? map) {
            Options.ModuleScaleMap = map;
            return this;
        }

        /// <summary>
        /// Sets module shape map.
        /// </summary>
        public QrBuilder WithModuleShapeMap(QrPngModuleShapeMapOptions? map) {
            Options.ModuleShapeMap = map;
            return this;
        }

        /// <summary>
        /// Sets per-module jitter options.
        /// </summary>
        public QrBuilder WithModuleJitter(QrPngModuleJitterOptions? jitter) {
            Options.ModuleJitter = jitter;
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
        /// Sets the background gradient.
        /// </summary>
        public QrBuilder WithBackgroundGradient(QrPngGradientOptions? gradient) {
            Options.BackgroundGradient = gradient;
            return this;
        }

        /// <summary>
        /// Sets the foreground palette.
        /// </summary>
        public QrBuilder WithForegroundPalette(QrPngPaletteOptions? palette) {
            Options.ForegroundPalette = palette;
            return this;
        }

        /// <summary>
        /// Sets the canvas options.
        /// </summary>
        public QrBuilder WithCanvas(QrPngCanvasOptions? canvas) {
            Options.Canvas = canvas;
            return this;
        }

        /// <summary>
        /// Sets palette overrides for specific zones.
        /// </summary>
        public QrBuilder WithForegroundPaletteZones(QrPngPaletteZoneOptions? zones) {
            Options.ForegroundPaletteZones = zones;
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

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return QrEasy.Render(Encode(), format, Options, extras);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Render(OutputFormat.Png).Data;

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Render(OutputFormat.Svg).GetText();

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Render(OutputFormat.Html).GetText();

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Render(OutputFormat.Jpeg).Data;

        /// <summary>
        /// Renders PPM bytes.
        /// </summary>
        public byte[] Ppm() => Render(OutputFormat.Ppm).Data;

        /// <summary>
        /// Renders PBM bytes.
        /// </summary>
        public byte[] Pbm() => Render(OutputFormat.Pbm).Data;

        /// <summary>
        /// Renders PGM bytes.
        /// </summary>
        public byte[] Pgm() => Render(OutputFormat.Pgm).Data;

        /// <summary>
        /// Renders PAM bytes.
        /// </summary>
        public byte[] Pam() => Render(OutputFormat.Pam).Data;

        /// <summary>
        /// Renders XBM text.
        /// </summary>
        public string Xbm() => Render(OutputFormat.Xbm).GetText();

        /// <summary>
        /// Renders XPM text.
        /// </summary>
        public string Xpm() => Render(OutputFormat.Xpm).GetText();

        /// <summary>
        /// Renders TGA bytes.
        /// </summary>
        public byte[] Tga() => Render(OutputFormat.Tga).Data;

        /// <summary>
        /// Renders ICO bytes.
        /// </summary>
        public byte[] Ico() => Render(OutputFormat.Ico).Data;

        /// <summary>
        /// Renders SVGZ bytes.
        /// </summary>
        public byte[] Svgz() => Render(OutputFormat.Svgz).Data;

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode mode = RenderMode.Vector) => Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }).Data;

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public string Eps(RenderMode mode = RenderMode.Vector) => Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }).GetText();

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => OutputWriter.Write(path, Render(OutputFormat.Png));

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Png));

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => OutputWriter.Write(path, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return OutputWriter.Write(path, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            OutputWriter.Write(stream, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => OutputWriter.Write(path, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) => OutputWriter.Write(path, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves WebP to a stream.
        /// </summary>
        public void SaveWebp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) => OutputWriter.Write(path, Render(OutputFormat.Bmp));

        /// <summary>
        /// Saves BMP to a stream.
        /// </summary>
        public void SaveBmp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Bmp));

        /// <summary>
        /// Saves PPM to a file.
        /// </summary>
        public string SavePpm(string path) => OutputWriter.Write(path, Render(OutputFormat.Ppm));

        /// <summary>
        /// Saves PPM to a stream.
        /// </summary>
        public void SavePpm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Ppm));

        /// <summary>
        /// Saves PBM to a file.
        /// </summary>
        public string SavePbm(string path) => OutputWriter.Write(path, Render(OutputFormat.Pbm));

        /// <summary>
        /// Saves PBM to a stream.
        /// </summary>
        public void SavePbm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pbm));

        /// <summary>
        /// Saves PGM to a file.
        /// </summary>
        public string SavePgm(string path) => OutputWriter.Write(path, Render(OutputFormat.Pgm));

        /// <summary>
        /// Saves PGM to a stream.
        /// </summary>
        public void SavePgm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pgm));

        /// <summary>
        /// Saves PAM to a file.
        /// </summary>
        public string SavePam(string path) => OutputWriter.Write(path, Render(OutputFormat.Pam));

        /// <summary>
        /// Saves PAM to a stream.
        /// </summary>
        public void SavePam(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pam));

        /// <summary>
        /// Saves XBM to a file.
        /// </summary>
        public string SaveXbm(string path) => OutputWriter.Write(path, Render(OutputFormat.Xbm));

        /// <summary>
        /// Saves XBM to a stream.
        /// </summary>
        public void SaveXbm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Xbm));

        /// <summary>
        /// Saves XPM to a file.
        /// </summary>
        public string SaveXpm(string path) => OutputWriter.Write(path, Render(OutputFormat.Xpm));

        /// <summary>
        /// Saves XPM to a stream.
        /// </summary>
        public void SaveXpm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Xpm));

        /// <summary>
        /// Saves TGA to a file.
        /// </summary>
        public string SaveTga(string path) => OutputWriter.Write(path, Render(OutputFormat.Tga));

        /// <summary>
        /// Saves TGA to a stream.
        /// </summary>
        public void SaveTga(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Tga));

        /// <summary>
        /// Saves ICO to a file.
        /// </summary>
        public string SaveIco(string path) => OutputWriter.Write(path, Render(OutputFormat.Ico));

        /// <summary>
        /// Saves ICO to a stream.
        /// </summary>
        public void SaveIco(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Ico));

        /// <summary>
        /// Saves SVGZ to a file.
        /// </summary>
        public string SaveSvgz(string path) => OutputWriter.Write(path, Render(OutputFormat.Svgz));

        /// <summary>
        /// Saves SVGZ to a stream.
        /// </summary>
        public void SaveSvgz(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svgz));

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode mode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves PDF to a stream.
        /// </summary>
    /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SavePdf(Stream stream, RenderMode mode = RenderMode.Vector) {
            OutputWriter.Write(stream, Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode mode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves EPS to a stream.
        /// </summary>
    /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SaveEps(Stream stream, RenderMode mode = RenderMode.Vector) {
            OutputWriter.Write(stream, Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? asciiOptions = null) {
            return Render(OutputFormat.Ascii, new RenderExtras { MatrixAscii = asciiOptions }).GetText();
        }

        /// <summary>
        /// Renders console-friendly ASCII text with auto-fit.
        /// </summary>
        public string AsciiConsole(AsciiConsoleOptions? consoleOptions = null) {
            return Render(OutputFormat.Ascii, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
        }

        /// <summary>
        /// Saves ASCII to a file.
        /// </summary>
        public string SaveAscii(string path, MatrixAsciiRenderOptions? asciiOptions = null) {
            return OutputWriter.Write(path, Render(OutputFormat.Ascii, new RenderExtras { MatrixAscii = asciiOptions }));
        }

        /// <summary>
        /// Saves console-friendly ASCII to a file.
        /// </summary>
        public string SaveAsciiConsole(string path, AsciiConsoleOptions? consoleOptions = null) {
            return OutputWriter.Write(path, Render(OutputFormat.Ascii, new RenderExtras { AsciiConsole = consoleOptions }));
        }

        /// <summary>
        /// Saves based on file extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return _payloadData is null
                ? QR.Save(_payload, path, Options, extras)
                : QR.Save(_payloadData, path, Options, extras);
        }
    }
}
