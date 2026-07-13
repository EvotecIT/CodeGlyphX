using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;
using System;
using System.IO;
using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Fluent QR builder returned by <see cref="QR.Create(string, QrEasyOptions?)"/>.
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

    /// <summary>
    /// Renders the configured QR code to the requested output format.
    /// </summary>
    public RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
        return QrEasy.Render(Encode(), format, Options, extras);
    }

    /// <summary>
    /// Saves the configured QR code, selecting the output format from the file extension.
    /// </summary>
    public string Save(string path, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, Render(format, extras));
    }

    /// <summary>
    /// Writes the configured QR code to a stream in the requested output format.
    /// </summary>
    public void Save(Stream stream, OutputFormat format, RenderExtras? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, extras));
    }
}