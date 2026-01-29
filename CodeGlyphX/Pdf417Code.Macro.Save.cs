using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves Macro PDF417 to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveMacro(string text, Pdf417MacroOptions macro, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = RenderMacro(text, macro, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }
}
