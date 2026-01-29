using System;
using System.IO;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    [Obsolete("Use the overload with RenderExtras and set HtmlTitle instead.")]
    public static string Save(string text, string path, DataMatrixEncodingMode mode, MatrixOptions? options, string? title) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Data Matrix to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    [Obsolete("Use the overload with RenderExtras and set HtmlTitle instead.")]
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode, MatrixOptions? options, string? title) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

}
