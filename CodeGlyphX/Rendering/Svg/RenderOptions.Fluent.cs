using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

public sealed partial class QrSvgRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public QrSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public QrSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the foreground color.</summary>
    public QrSvgRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public QrSvgRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }

    /// <summary>Sets the logo options.</summary>
    public QrSvgRenderOptions WithLogo(QrLogoOptions? logo) {
        Logo = logo;
        return this;
    }

    /// <summary>Sets the module shape.</summary>
    public QrSvgRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    /// <summary>Sets the module scale.</summary>
    public QrSvgRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    /// <summary>Sets the module corner radius in pixels.</summary>
    public QrSvgRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    /// <summary>Sets the foreground gradient options.</summary>
    public QrSvgRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    /// <summary>Sets the eye styling options.</summary>
    public QrSvgRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }
}

public sealed partial class MatrixSvgRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public MatrixSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public MatrixSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the foreground color.</summary>
    public MatrixSvgRenderOptions WithDarkColor(string color) {
        DarkColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public MatrixSvgRenderOptions WithLightColor(string color) {
        LightColor = color;
        return this;
    }
}

public sealed partial class BarcodeSvgRenderOptions {
    /// <summary>Sets the module size in pixels.</summary>
    public BarcodeSvgRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public BarcodeSvgRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the bar height in modules.</summary>
    public BarcodeSvgRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    /// <summary>Sets the bar color.</summary>
    public BarcodeSvgRenderOptions WithBarColor(string color) {
        BarColor = color;
        return this;
    }

    /// <summary>Sets the background color.</summary>
    public BarcodeSvgRenderOptions WithBackgroundColor(string color) {
        BackgroundColor = color;
        return this;
    }

    /// <summary>Sets the human-readable label text.</summary>
    public BarcodeSvgRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    /// <summary>Sets the label font size in pixels.</summary>
    public BarcodeSvgRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    /// <summary>Sets the label margin in pixels.</summary>
    public BarcodeSvgRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    /// <summary>Sets the label color.</summary>
    public BarcodeSvgRenderOptions WithLabelColor(string color) {
        LabelColor = color;
        return this;
    }

    /// <summary>Sets the label font family.</summary>
    public BarcodeSvgRenderOptions WithLabelFontFamily(string family) {
        LabelFontFamily = family;
        return this;
    }
}
