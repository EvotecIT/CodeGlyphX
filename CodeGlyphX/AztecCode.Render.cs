using System;
using CodeGlyphX.Aztec;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class AztecCode {
    /// <summary>
    /// Renders an Aztec payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = Encode(text, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? renderOptions, RenderExtras? extras) {
        return MatrixOutputRenderer.Render(modules, format, renderOptions, extras);
    }
}
