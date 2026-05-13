using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Simple helpers for matrix and stacked barcode symbologies.
/// </summary>
/// <remarks>
/// Use <see cref="Save(CodeGlyphX.BarcodeType,string,string,CodeGlyphX.MatrixOptions,CodeGlyphX.Rendering.RenderExtras)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// MatrixBarcode.Save(BarcodeType.DataMatrix, "ORDER-12345", "datamatrix.png");
/// MatrixBarcode.Save(BarcodeType.PDF417, "DOCUMENT-12345", "pdf417.svg");
/// </code>
/// </example>
public static class MatrixBarcode {
    /// <summary>
    /// Encodes a matrix or stacked barcode.
    /// </summary>
    public static BitMatrix Encode(BarcodeType type, string content) {
        return MatrixBarcodeEncoder.Encode(type, content);
    }

    /// <summary>
    /// Renders a matrix or stacked barcode to the requested output format.
    /// </summary>
    public static RenderedOutput Render(BarcodeType type, string content, OutputFormat format, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = Encode(type, content);
        return Render(modules, format, options, extras);
    }

    /// <summary>
    /// Renders a matrix or stacked barcode to the requested output format.
    /// </summary>
    public static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? options = null, RenderExtras? extras = null) {
        return MatrixOutputRenderer.Render(modules, format, options, extras);
    }

    /// <summary>
    /// Saves a matrix or stacked barcode to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(BarcodeType type, string content, string path, MatrixOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(type, content, format, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves a matrix or stacked barcode to a stream using the requested output format.
    /// </summary>
    public static void Save(BarcodeType type, string content, Stream stream, OutputFormat format, MatrixOptions? options = null, RenderExtras? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var output = Render(type, content, format, options, extras);
        OutputWriter.Write(stream, output);
    }

}
