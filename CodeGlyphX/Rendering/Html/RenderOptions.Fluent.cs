using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Html;

public sealed partial class QrHtmlRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public QrHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public QrHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the dark color (CSS value).
    /// </summary>
    public QrHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>
    /// Sets the light color (CSS value).
    /// </summary>
    public QrHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    /// <summary>
    /// Enables or disables email-safe HTML output.
    /// </summary>
    public QrHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    /// <summary>
    /// Sets the logo overlay options.
    /// </summary>
    public QrHtmlRenderOptions WithLogo(QrLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    /// <summary>
    /// Sets the module shape.
    /// </summary>
    public QrHtmlRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    /// <summary>
    /// Sets the module scale.
    /// </summary>
    public QrHtmlRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    /// <summary>
    /// Sets the module corner radius in pixels.
    /// </summary>
    public QrHtmlRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>
    /// Sets the foreground gradient.
    /// </summary>
    public QrHtmlRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    /// <summary>
    /// Sets custom eye (finder) options.
    /// </summary>
    public QrHtmlRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }
}

public sealed partial class MatrixHtmlRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public MatrixHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public MatrixHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the dark color (CSS value).
    /// </summary>
    public MatrixHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>
    /// Sets the light color (CSS value).
    /// </summary>
    public MatrixHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    /// <summary>
    /// Enables or disables email-safe HTML output.
    /// </summary>
    public MatrixHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }
}

public sealed partial class BarcodeHtmlRenderOptions {
    /// <summary>
    /// Sets the module size in pixels.
    /// </summary>
    public BarcodeHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public BarcodeHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the barcode height in modules.
    /// </summary>
    public BarcodeHtmlRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    /// <summary>
    /// Sets the bar color (CSS value).
    /// </summary>
    public BarcodeHtmlRenderOptions WithBarColor(string color) {
        BarColor = color;
        return this;
    }

    /// <summary>
    /// Sets the background color (CSS value).
    /// </summary>
    public BarcodeHtmlRenderOptions WithBackgroundColor(string color) {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// Enables or disables email-safe HTML output.
    /// </summary>
    public BarcodeHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    /// <summary>
    /// Sets the label text.
    /// </summary>
    public BarcodeHtmlRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    /// <summary>
    /// Sets the label font size in pixels.
    /// </summary>
    public BarcodeHtmlRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    /// <summary>
    /// Sets the label margin in pixels.
    /// </summary>
    public BarcodeHtmlRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    /// <summary>
    /// Sets the label color (CSS value).
    /// </summary>
    public BarcodeHtmlRenderOptions WithLabelColor(string color) {
        LabelColor = color;
        return this;
    }

    /// <summary>
    /// Sets the label font family.
    /// </summary>
    public BarcodeHtmlRenderOptions WithLabelFontFamily(string family) {
        LabelFontFamily = family;
        return this;
    }
}
