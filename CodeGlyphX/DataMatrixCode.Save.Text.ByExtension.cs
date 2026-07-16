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
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, mode, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Data Matrix to a file using explicit encoding options and selecting the output format from the extension.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingOptions encodingOptions, MatrixOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodingOptions, options, extras);
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


    /// <summary>
    /// Saves Data Matrix to a file for byte payloads using explicit encoding options.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingOptions encodingOptions, MatrixOptions? options = null, RenderExtras? extras = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, encodingOptions, options, extras);
        return OutputWriter.Write(path, output);
    }

}
