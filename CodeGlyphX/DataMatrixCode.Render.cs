using System;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Renders a Data Matrix payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = Encode(text, mode);
        return Render(modules, format, options, extras);
    }

    /// <summary>
    /// Renders a Data Matrix byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(byte[] data, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, mode);
        return Render(modules, format, options, extras);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Renders a Data Matrix byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(ReadOnlySpan<byte> data, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, mode);
        return Render(modules, format, options, extras);
    }
#endif

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? options, RenderExtras? extras) {
        return MatrixOutputRenderer.Render(modules, format, options, extras);
    }
}
