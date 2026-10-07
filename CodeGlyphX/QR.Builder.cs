using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using System;
using System.IO;

namespace CodeGlyphX;

/// <summary>Fluent QR builder that owns independent encoding and appearance settings.</summary>
public sealed class QrBuilder {
    private readonly string _payload;

    /// <summary>Mutable encoding settings owned by this builder; incoming options are copied.</summary>
    public QrEncodingOptions Encoding { get; }

    /// <summary>Mutable appearance settings owned by this builder; incoming nested options are copied.</summary>
    public QrRenderOptions Rendering { get; }

    internal QrBuilder(string payload, QrRenderOptions? rendering, QrEncodingOptions? encoding) {
        _payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Encoding = QR.ResolveEncodingOptions(null, encoding);
        Rendering = rendering is null ? new QrRenderOptions() : QrRenderer.CloneOptions(rendering);
    }

    internal QrBuilder(QrPayloadData payload, QrRenderOptions? rendering, QrEncodingOptions? encoding) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        _payload = payload.Text;
        Encoding = QR.ResolveEncodingOptions(payload, encoding);
        Rendering = rendering is null ? new QrRenderOptions() : QrRenderer.CloneOptions(rendering);
    }

    /// <summary>Updates the encoding settings owned by this builder.</summary>
    public QrBuilder WithEncoding(Action<QrEncodingOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(Encoding);
        return this;
    }

    /// <summary>Updates the appearance settings owned by this builder.</summary>
    public QrBuilder WithRendering(Action<QrRenderOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(Rendering);
        return this;
    }

    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public QrBuilder WithModuleSize(int moduleSize) {
        Rendering.ModuleSize = moduleSize;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public QrBuilder WithQuietZone(int quietZone) {
        Rendering.QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets foreground and background colors.
    /// </summary>
    public QrBuilder WithColors(Rgba32 foreground, Rgba32 background) {
        Rendering.Foreground = foreground;
        Rendering.Background = background;
        return this;
    }

    /// <summary>
    /// Sets foreground color.
    /// </summary>
    public QrBuilder WithForeground(Rgba32 color) {
        Rendering.Foreground = color;
        return this;
    }

    /// <summary>
    /// Sets background color.
    /// </summary>
    public QrBuilder WithBackground(Rgba32 color) {
        Rendering.Background = color;
        return this;
    }

    /// <summary>
    /// Uses a transparent background (alpha = 0).
    /// </summary>
    public QrBuilder WithTransparentBackground() {
        Rendering.Background = Rgba32.Transparent;
        return this;
    }

    /// <summary>
    /// Sets the render style preset.
    /// </summary>
    public QrBuilder WithStyle(QrRenderStyle style) {
        Rendering.Style = style;
        return this;
    }

    /// <summary>
    /// Sets module shape override.
    /// </summary>
    public QrBuilder WithModuleShape(QrModuleShape shape) {
        Rendering.ModuleShape = shape;
        return this;
    }

    /// <summary>
    /// Sets module scale override (0.1..1.0).
    /// </summary>
    public QrBuilder WithModuleScale(double scale) {
        Rendering.ModuleScale = scale;
        return this;
    }

    /// <summary>
    /// Copies the module scale map into this builder.
    /// </summary>
    public QrBuilder WithModuleScaleMap(QrModuleScaleMapOptions? map) {
        Rendering.ModuleScaleMap = QrRenderer.CloneScaleMap(map);
        return this;
    }

    /// <summary>
    /// Copies the module shape map into this builder.
    /// </summary>
    public QrBuilder WithModuleShapeMap(QrModuleShapeMapOptions? map) {
        Rendering.ModuleShapeMap = QrRenderer.CloneShapeMap(map);
        return this;
    }

    /// <summary>
    /// Copies the per-module jitter options into this builder.
    /// </summary>
    public QrBuilder WithModuleJitter(QrModuleJitterOptions? jitter) {
        Rendering.ModuleJitter = QrRenderer.CloneJitter(jitter);
        return this;
    }

    /// <summary>
    /// Sets module corner radius in pixels.
    /// </summary>
    public QrBuilder WithModuleCornerRadiusPx(int radiusPx) {
        Rendering.ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>
    /// Copies the foreground gradient into this builder.
    /// </summary>
    public QrBuilder WithForegroundGradient(QrGradientOptions? gradient) {
        Rendering.ForegroundGradient = QrRenderer.CloneGradient(gradient);
        return this;
    }

    /// <summary>
    /// Copies the background gradient into this builder.
    /// </summary>
    public QrBuilder WithBackgroundGradient(QrGradientOptions? gradient) {
        Rendering.BackgroundGradient = QrRenderer.CloneGradient(gradient);
        return this;
    }

    /// <summary>
    /// Copies the foreground palette and its colors into this builder.
    /// </summary>
    public QrBuilder WithForegroundPalette(QrPaletteOptions? palette) {
        Rendering.ForegroundPalette = QrRenderer.ClonePalette(palette);
        return this;
    }

    /// <summary>
    /// Copies the canvas options, including nested styles and color arrays, into this builder.
    /// </summary>
    public QrBuilder WithCanvas(QrCanvasOptions? canvas) {
        Rendering.Canvas = QrRenderer.CloneCanvas(canvas);
        return this;
    }

    /// <summary>
    /// Copies the zone palette overrides and their colors into this builder.
    /// </summary>
    public QrBuilder WithForegroundPaletteZones(QrPaletteZoneOptions? zones) {
        Rendering.ForegroundPaletteZones = QrRenderer.ClonePaletteZones(zones);
        return this;
    }

    /// <summary>
    /// Copies the eye (finder) styling, including nested gradients and color arrays, into this builder.
    /// </summary>
    public QrBuilder WithEyes(QrEyeOptions? eyes) {
        Rendering.Eyes = QrRenderer.CloneEyes(eyes);
        return this;
    }

    /// <summary>
    /// Sets a fixed target size (in pixels). Module size is adjusted to fit.
    /// </summary>
    public QrBuilder WithTargetSize(int sizePx, bool includeQuietZone = true) {
        Rendering.TargetSizePx = sizePx;
        Rendering.TargetSizeIncludesQuietZone = includeQuietZone;
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
        Rendering.LogoPng = png is null ? throw new ArgumentNullException(nameof(png)) : (byte[])png.Clone();
        return this;
    }

    /// <summary>
    /// Sets the logo scale relative to the QR area (excluding quiet zone).
    /// </summary>
    public QrBuilder WithLogoScale(double scale) {
        Rendering.LogoScale = scale;
        return this;
    }

    /// <summary>
    /// Sets the logo padding in pixels.
    /// </summary>
    public QrBuilder WithLogoPaddingPx(int paddingPx) {
        Rendering.LogoPaddingPx = paddingPx;
        return this;
    }

    /// <summary>
    /// Sets whether to draw a background plate behind the logo.
    /// </summary>
    public QrBuilder WithLogoBackground(bool enabled = true) {
        Rendering.LogoDrawBackground = enabled;
        return this;
    }

    /// <summary>
    /// Sets the logo background color.
    /// </summary>
    public QrBuilder WithLogoBackgroundColor(Rgba32? color) {
        Rendering.LogoBackground = color;
        return this;
    }

    /// <summary>
    /// Sets the logo background corner radius in pixels.
    /// </summary>
    public QrBuilder WithLogoCornerRadiusPx(int radiusPx) {
        Rendering.LogoCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>
    /// Sets an embedded logo from a PNG file.
    /// </summary>
    public QrBuilder WithLogoFile(string path) {
        Rendering.LogoPng = RenderIO.ReadBinary(path);
        return this;
    }

    /// <summary>
    /// Sets error correction level.
    /// </summary>
    public QrBuilder WithErrorCorrection(QrErrorCorrectionLevel ecc) {
        Encoding.ErrorCorrectionLevel = ecc;
        return this;
    }

    /// <summary>
    /// Encodes the QR code.
    /// </summary>
    public QrCode Encode() => QR.Encode(_payload, Encoding);

    /// <summary>
    /// Renders the configured QR code to the requested output format.
    /// </summary>
    public RenderedOutput Render(OutputFormat format, OutputOptions? outputOptions = null) {
        return Encode().Render(format, Rendering, outputOptions);
    }

    /// <summary>
    /// Saves the configured QR code, selecting the output format from the file extension.
    /// </summary>
    public string Save(string path, OutputOptions? outputOptions = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, Render(format, outputOptions));
    }

    /// <summary>
    /// Writes the configured QR code to a stream in the requested output format.
    /// </summary>
    public void Save(Stream stream, OutputFormat format, OutputOptions? outputOptions = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, outputOptions));
    }
}