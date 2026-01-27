namespace CodeGlyphX.Rendering.Png;

public sealed partial class QrPngRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public QrPngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public QrPngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    public QrPngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Sets the background color.
    /// </summary>
    public QrPngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }

    /// <summary>
    /// Sets the background gradient.
    /// </summary>
    public QrPngRenderOptions WithBackgroundGradient(QrPngGradientOptions? gradient) {
        BackgroundGradient = gradient;
        return this;
    }

    /// <summary>
    /// Sets the background pattern overlay.
    /// </summary>
    public QrPngRenderOptions WithBackgroundPattern(QrPngBackgroundPatternOptions? pattern) {
        BackgroundPattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the background supersample factor (1 = disabled).
    /// </summary>
    public QrPngRenderOptions WithBackgroundSupersample(int factor) {
        BackgroundSupersample = factor;
        return this;
    }

    /// <summary>
    /// Sets the foreground gradient.
    /// </summary>
    public QrPngRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    /// <summary>
    /// Sets the foreground palette.
    /// </summary>
    public QrPngRenderOptions WithForegroundPalette(QrPngPaletteOptions? palette) {
        ForegroundPalette = palette;
        return this;
    }

    /// <summary>
    /// Sets palette overrides for specific zones.
    /// </summary>
    public QrPngRenderOptions WithForegroundPaletteZones(QrPngPaletteZoneOptions? zones) {
        ForegroundPaletteZones = zones;
        return this;
    }

    /// <summary>
    /// Sets custom eye (finder) options.
    /// </summary>
    public QrPngRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }

    /// <summary>
    /// Sets the module shape.
    /// </summary>
    public QrPngRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    /// <summary>
    /// Sets the module scale.
    /// </summary>
    public QrPngRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    /// <summary>
    /// Sets per-module scale mapping options.
    /// </summary>
    public QrPngRenderOptions WithModuleScaleMap(QrPngModuleScaleMapOptions? map) {
        ModuleScaleMap = map;
        return this;
    }

    /// <summary>
    /// Sets the module corner radius in pixels.
    /// </summary>
    public QrPngRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>
    /// Sets the PNG compression level (0 = stored/uncompressed, 1-9 = compressed).
    /// </summary>
    public QrPngRenderOptions WithPngCompressionLevel(int level) {
        PngCompressionLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the logo overlay options.
    /// </summary>
    public QrPngRenderOptions WithLogo(QrPngLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    /// <summary>
    /// Sets the canvas options.
    /// </summary>
    public QrPngRenderOptions WithCanvas(QrPngCanvasOptions? canvas) {
        Canvas = canvas;
        return this;
    }

    /// <summary>
    /// Sets the debug overlay options.
    /// </summary>
    public QrPngRenderOptions WithDebug(QrPngDebugOptions? debug) {
        Debug = debug;
        return this;
    }
}

public sealed partial class MatrixPngRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public MatrixPngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public MatrixPngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    public MatrixPngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Sets the background color.
    /// </summary>
    public MatrixPngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }

    /// <summary>
    /// Sets the PNG compression level (0 = stored/uncompressed, 1-9 = compressed).
    /// </summary>
    public MatrixPngRenderOptions WithPngCompressionLevel(int level) {
        PngCompressionLevel = level;
        return this;
    }
}

public sealed partial class BarcodePngRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public BarcodePngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public BarcodePngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the barcode height in modules.
    /// </summary>
    public BarcodePngRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    /// <summary>
    /// Sets the bar (foreground) color.
    /// </summary>
    public BarcodePngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Sets the background color.
    /// </summary>
    public BarcodePngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }

    /// <summary>
    /// Sets the label text.
    /// </summary>
    public BarcodePngRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    /// <summary>
    /// Sets the label font size in pixels.
    /// </summary>
    public BarcodePngRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    /// <summary>
    /// Sets the label margin in pixels.
    /// </summary>
    public BarcodePngRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    /// <summary>
    /// Sets the label color.
    /// </summary>
    public BarcodePngRenderOptions WithLabelColor(Rgba32 color) {
        LabelColor = color;
        return this;
    }

    /// <summary>
    /// Sets the PNG compression level (0 = stored/uncompressed, 1-9 = compressed).
    /// </summary>
    public BarcodePngRenderOptions WithPngCompressionLevel(int level) {
        PngCompressionLevel = level;
        return this;
    }
}
