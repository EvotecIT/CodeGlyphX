namespace CodeGlyphX.Rendering.Ascii;

public sealed partial class MatrixAsciiRenderOptions {
    public MatrixAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public MatrixAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    public MatrixAsciiRenderOptions WithModuleHeight(int height) {
        ModuleHeight = height;
        return this;
    }

    public MatrixAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    public MatrixAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    public MatrixAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }
}

public sealed partial class BarcodeAsciiRenderOptions {
    public BarcodeAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    public BarcodeAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    public BarcodeAsciiRenderOptions WithHeight(int height) {
        Height = height;
        return this;
    }

    public BarcodeAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    public BarcodeAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    public BarcodeAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }
}
