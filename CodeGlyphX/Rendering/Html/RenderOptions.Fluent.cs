using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Html;

public sealed partial class QrHtmlRenderOptions {
    public QrHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public QrHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public QrHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    public QrHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    public QrHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    public QrHtmlRenderOptions WithLogo(QrLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    public QrHtmlRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    public QrHtmlRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    public QrHtmlRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    public QrHtmlRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    public QrHtmlRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }
}

public sealed partial class MatrixHtmlRenderOptions {
    public MatrixHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public MatrixHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public MatrixHtmlRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    public MatrixHtmlRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    public MatrixHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }
}

public sealed partial class BarcodeHtmlRenderOptions {
    public BarcodeHtmlRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public BarcodeHtmlRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public BarcodeHtmlRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    public BarcodeHtmlRenderOptions WithBarColor(string color) {
        BarColor = color;
        return this;
    }

    public BarcodeHtmlRenderOptions WithBackgroundColor(string color) {
        BackgroundColor = color;
        return this;
    }

    public BarcodeHtmlRenderOptions WithEmailSafeTable(bool enabled = true) {
        EmailSafeTable = enabled;
        return this;
    }

    public BarcodeHtmlRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    public BarcodeHtmlRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    public BarcodeHtmlRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    public BarcodeHtmlRenderOptions WithLabelColor(string color) {
        LabelColor = color;
        return this;
    }

    public BarcodeHtmlRenderOptions WithLabelFontFamily(string family) {
        LabelFontFamily = family;
        return this;
    }
}
