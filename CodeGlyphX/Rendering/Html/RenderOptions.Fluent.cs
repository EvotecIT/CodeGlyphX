using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Html;

public sealed partial class QrHtmlRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public QrHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public QrHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the foreground color.</summary>
    public QrHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public QrHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    /// <summary>Enables email-safe table rendering.</summary>
    public QrHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    /// <summary>Sets the logo options.</summary>
    public QrHtmlRenderOptions WithLogo(QrLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    /// <summary>Sets the module shape.</summary>
    public QrHtmlRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    /// <summary>Sets the module scale.</summary>
    public QrHtmlRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    /// <summary>Sets the module corner radius in pixels.</summary>
    public QrHtmlRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>Sets the foreground gradient options.</summary>
    public QrHtmlRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    /// <summary>Sets the eye styling options.</summary>
    public QrHtmlRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }
}

public sealed partial class MatrixHtmlRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public MatrixHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public MatrixHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the foreground color.</summary>
    public MatrixHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public MatrixHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    /// <summary>Enables email-safe table rendering.</summary>
    public MatrixHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }
}

public sealed partial class BarcodeHtmlRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public BarcodeHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public BarcodeHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the bar height in modules.</summary>
    public BarcodeHtmlRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    /// <summary>Sets the bar color.</summary>
    public BarcodeHtmlRenderOptions WithBarColor(string color) {
        BarColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public BarcodeHtmlRenderOptions WithBackgroundColor(string color) {
        BackgroundColor = color;
        return this;
    }

    /// <summary>Enables email-safe table rendering.</summary>
    public BarcodeHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    /// <summary>Sets the human-readable label text.</summary>
    public BarcodeHtmlRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    /// <summary>Sets the label font size in pixels.</summary>
    public BarcodeHtmlRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    /// <summary>Sets the label margin in pixels.</summary>
    public BarcodeHtmlRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    /// <summary>Sets the label color.</summary>
    public BarcodeHtmlRenderOptions WithLabelColor(string color) {
        LabelColor = color;
        return this;
    }

    /// <summary>Sets the label font family.</summary>
    public BarcodeHtmlRenderOptions WithLabelFontFamily(string family) {
        LabelFontFamily = family;
        return this;
    }
}
