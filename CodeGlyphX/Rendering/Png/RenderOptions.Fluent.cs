namespace CodeGlyphX.Rendering.Png;

public sealed partial class QrPngRenderOptions {
    public QrPngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public QrPngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public QrPngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    public QrPngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }

    public QrPngRenderOptions WithForegroundGradient(QrPngGradientOptions? gradient) {
        ForegroundGradient = gradient;
        return this;
    }

    public QrPngRenderOptions WithEyes(QrPngEyeOptions? eyes) {
        Eyes = eyes;
        return this;
    }

    public QrPngRenderOptions WithModuleShape(QrPngModuleShape shape) {
        ModuleShape = shape;
        return this;
    }

    public QrPngRenderOptions WithModuleScale(double scale) {
        ModuleScale = scale;
        return this;
    }

    public QrPngRenderOptions WithModuleCornerRadiusPx(int radiusPx) {
        ModuleCornerRadiusPx = radiusPx;
        return this;
    }

    public QrPngRenderOptions WithLogo(QrPngLogoOptions? logo) {
        Logo = logo;
        return this;
    }
}

public sealed partial class MatrixPngRenderOptions {
    public MatrixPngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public MatrixPngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public MatrixPngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    public MatrixPngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }
}

public sealed partial class BarcodePngRenderOptions {
    public BarcodePngRenderOptions WithModuleSize(int size) {
        ModuleSize = size;
        return this;
    }

    public BarcodePngRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public BarcodePngRenderOptions WithHeightModules(int heightModules) {
        HeightModules = heightModules;
        return this;
    }

    public BarcodePngRenderOptions WithForeground(Rgba32 color) {
        Foreground = color;
        return this;
    }

    public BarcodePngRenderOptions WithBackground(Rgba32 color) {
        Background = color;
        return this;
    }

    public BarcodePngRenderOptions WithLabelText(string? text) {
        LabelText = text;
        return this;
    }

    public BarcodePngRenderOptions WithLabelFontSize(int size) {
        LabelFontSize = size;
        return this;
    }

    public BarcodePngRenderOptions WithLabelMargin(int margin) {
        LabelMargin = margin;
        return this;
    }

    public BarcodePngRenderOptions WithLabelColor(Rgba32 color) {
        LabelColor = color;
        return this;
    }
}
