using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves PDF417 to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

}
