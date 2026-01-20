namespace CodeGlyphX.Rendering.Ascii;

public sealed partial class MatrixAsciiRenderOptions {
    /// <summary>Sets the quiet zone size (in modules).</summary>
    public MatrixAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the module width (in characters).</summary>
    public MatrixAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    /// <summary>Sets the module height (in characters).</summary>
    public MatrixAsciiRenderOptions WithModuleHeight(int height) {
        ModuleHeight = height;
        return this;
    }

    /// <summary>Sets the dark module string.</summary>
    public MatrixAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    /// <summary>Sets the light module string.</summary>
    public MatrixAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    /// <summary>Sets the newline sequence.</summary>
    public MatrixAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }
}

public sealed partial class BarcodeAsciiRenderOptions {
    /// <summary>Sets the module width (in characters).</summary>
    public BarcodeAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    /// <summary>Sets the quiet zone size (in modules).</summary>
    public BarcodeAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>Sets the barcode height (in rows).</summary>
    public BarcodeAsciiRenderOptions WithHeight(int height) {
        Height = height;
        return this;
    }

    /// <summary>Sets the dark bar string.</summary>
    public BarcodeAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    /// <summary>Sets the light bar string.</summary>
    public BarcodeAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    /// <summary>Sets the newline sequence.</summary>
    public BarcodeAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }
}
