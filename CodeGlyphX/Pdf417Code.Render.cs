using System;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Renders a PDF417 payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = Encode(text, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    /// <summary>
    /// Renders a Macro PDF417 payload to the requested output format.
    /// </summary>
    public static RenderedOutput RenderMacro(string text, Pdf417MacroOptions macro, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    /// <summary>
    /// Renders a PDF417 byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(byte[] data, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Renders a PDF417 byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(ReadOnlySpan<byte> data, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }
#endif

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? renderOptions, RenderExtras? extras) {
        return MatrixOutputRenderer.Render(modules, format, renderOptions, extras);
    }
}
