using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;
using System;
using System.IO;
using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Fluent barcode builder returned by <see cref="Barcode.Create(SymbolFormat, string, BarcodeOptions?)"/>.
/// </summary>
public sealed class BarcodeBuilder {
    /// <summary>Gets the format-specific output settings configured for this builder.</summary>
    public OutputOptions OutputOptions { get; } = new OutputOptions();

    /// <summary>Configures format-specific output settings without changing symbol layout.</summary>
    public BarcodeBuilder WithOutputOptions(Action<OutputOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(OutputOptions);
        return this;
    }

    private readonly SymbolFormat _type;
    private readonly string _content;

    /// <summary>
    /// Rendering options used by this builder.
    /// </summary>
    public BarcodeOptions Options { get; }

    internal BarcodeBuilder(SymbolFormat type, string content, BarcodeOptions? options) {
        _type = type;
        _content = content ?? throw new ArgumentNullException(nameof(content));
        Options = options?.Clone() ?? new BarcodeOptions();
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
    /// Sets foreground color.
    /// </summary>
    public BarcodeBuilder WithForeground(Rgba32 color) {
        Options.Foreground = color;
        return this;
    }

    /// <summary>
    /// Sets background color.
    /// </summary>
    public BarcodeBuilder WithBackground(Rgba32 color) {
        Options.Background = color;
        return this;
    }

    /// <summary>
    /// Uses a transparent background (alpha = 0).
    /// </summary>
    public BarcodeBuilder WithTransparentBackground() {
        Options.Background = Rgba32.Transparent;
        return this;
    }

    /// <summary>
    /// Sets JPEG quality.
    /// </summary>
    public BarcodeBuilder WithJpegQuality(int quality) {
        OutputOptions.JpegQuality = quality;
        return this;
    }

    /// <summary>
    /// Sets JPEG encoding options.
    /// </summary>
    public BarcodeBuilder WithJpegOptions(JpegEncodeOptions options) {
        OutputOptions.JpegOptions = options ?? throw new ArgumentNullException(nameof(options));
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
    /// Sets ICO output sizes (in pixels).
    /// </summary>
    public BarcodeBuilder WithIcoSizes(params int[] sizes) {
        OutputOptions.IcoSizes = sizes is null ? throw new ArgumentNullException(nameof(sizes)) : (int[])sizes.Clone();
        return this;
    }

    /// <summary>
    /// Sets ICO aspect ratio preservation behavior.
    /// </summary>
    public BarcodeBuilder WithIcoPreserveAspectRatio(bool enabled = true) {
        OutputOptions.IcoPreserveAspectRatio = enabled;
        return this;
    }

    /// <summary>
    /// Encodes the barcode.
    /// </summary>
    public Barcode1D Encode() => Barcode.Encode(_type, _content);

    /// <summary>
    /// Renders the configured barcode to the requested output format.
    /// </summary>
    public RenderedOutput Render(OutputFormat format, OutputOptions? extras = null) {
        return Barcode.Render(_type, _content, format, Options, extras ?? OutputOptions);
    }

    /// <summary>
    /// Saves the configured barcode, selecting the output format from the file extension.
    /// </summary>
    public string Save(string path, OutputOptions? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, Render(format, extras));
    }

    /// <summary>
    /// Writes the configured barcode to a stream in the requested output format.
    /// </summary>
    public void Save(Stream stream, OutputFormat format, OutputOptions? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, extras));
    }
}