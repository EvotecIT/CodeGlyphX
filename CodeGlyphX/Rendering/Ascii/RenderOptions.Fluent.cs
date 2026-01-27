namespace CodeGlyphX.Rendering.Ascii;

public sealed partial class MatrixAsciiRenderOptions {
    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public MatrixAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the module width in characters.
    /// </summary>
    public MatrixAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the module height in rows.
    /// </summary>
    public MatrixAsciiRenderOptions WithModuleHeight(int height) {
        ModuleHeight = height;
        return this;
    }

    /// <summary>
    /// Sets an additional scale multiplier applied to both module width and height.
    /// </summary>
    public MatrixAsciiRenderOptions WithScale(int scale) {
        Scale = scale;
        return this;
    }

    /// <summary>
    /// Sets the character(s) used for dark modules.
    /// </summary>
    public MatrixAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    /// <summary>
    /// Sets the character(s) used for light modules.
    /// </summary>
    public MatrixAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    /// <summary>
    /// Sets the line separator.
    /// </summary>
    public MatrixAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }

    /// <summary>
    /// Enables or disables Unicode block glyphs when defaults are used.
    /// </summary>
    public MatrixAsciiRenderOptions WithUnicodeBlocks(bool enabled = true) {
        UseUnicodeBlocks = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables inverted output (dark/light swapped).
    /// </summary>
    public MatrixAsciiRenderOptions WithInvert(bool enabled = true) {
        Invert = enabled;
        return this;
    }
}

public sealed partial class BarcodeAsciiRenderOptions {
    /// <summary>
    /// Sets the module width in characters.
    /// </summary>
    public BarcodeAsciiRenderOptions WithModuleWidth(int width) {
        ModuleWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the quiet zone size in modules.
    /// </summary>
    public BarcodeAsciiRenderOptions WithQuietZone(int quietZone) {
        QuietZone = quietZone;
        return this;
    }

    /// <summary>
    /// Sets the height in text rows.
    /// </summary>
    public BarcodeAsciiRenderOptions WithHeight(int height) {
        Height = height;
        return this;
    }

    /// <summary>
    /// Sets the character(s) used for bars.
    /// </summary>
    public BarcodeAsciiRenderOptions WithDark(string dark) {
        Dark = dark;
        return this;
    }

    /// <summary>
    /// Sets the character(s) used for spaces.
    /// </summary>
    public BarcodeAsciiRenderOptions WithLight(string light) {
        Light = light;
        return this;
    }

    /// <summary>
    /// Sets the line separator.
    /// </summary>
    public BarcodeAsciiRenderOptions WithNewLine(string newLine) {
        NewLine = newLine;
        return this;
    }
}
