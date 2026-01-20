using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

public sealed partial class QrSvgRenderOptions {
    public QrSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public QrSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public QrSvgRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    public QrSvgRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    public QrSvgRenderOptions WithLogo(QrLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    public QrSvgRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    public QrSvgRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    public QrSvgRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    public QrSvgRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    public QrSvgRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }
}

public sealed partial class MatrixSvgRenderOptions {
    public MatrixSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public MatrixSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public MatrixSvgRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    public MatrixSvgRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }
}

public sealed partial class BarcodeSvgRenderOptions {
    public BarcodeSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public BarcodeSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public BarcodeSvgRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    public BarcodeSvgRenderOptions WithBarColor(string color) {
        BarColor = color;
        return this;
    }

    public BarcodeSvgRenderOptions WithBackgroundColor(string color) {
        BackgroundColor = color;
        return this;
    }

    public BarcodeSvgRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    public BarcodeSvgRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    public BarcodeSvgRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    public BarcodeSvgRenderOptions WithLabelColor(string color) {
        LabelColor = color;
        return this;
    }

    public BarcodeSvgRenderOptions WithLabelFontFamily(string family) {
        LabelFontFamily = family;
        return this;
    }
}
