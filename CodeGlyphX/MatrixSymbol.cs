using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>An encoded symbol with an orthogonal module grid and stable format identity.</summary>
/// <remarks>Modules are an owned, read-only snapshot. Clone them for editing or damage simulation.</remarks>
public class MatrixSymbol {
    /// <summary>Gets the physical symbol format.</summary>
    public SymbolFormat Format { get; }

    /// <summary>Gets the read-only modules without a quiet zone.</summary>
    public BitMatrix Modules { get; }

    /// <summary>Gets the width in modules.</summary>
    public int Width => Modules.Width;

    /// <summary>Gets the height in modules.</summary>
    public int Height => Modules.Height;

    internal MatrixSymbol(SymbolFormat format, BitMatrix modules) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        Format = format;
        Modules = modules.Clone().Freeze();
    }

    /// <summary>Renders the encoded modules with layout options and format-specific output settings.</summary>
    public RenderedOutput Render(OutputFormat format, MatrixOptions? options = null, RenderExtras? extras = null) =>
        MatrixOutputRenderer.Render(Modules, format, options, extras);

    /// <summary>Saves the symbol, selecting the output format from the file extension.</summary>
    public string Save(string path, MatrixOptions? options = null, RenderExtras? extras = null) =>
        OutputWriter.Write(path, Render(OutputFormatInfo.Resolve(path, OutputFormat.Png), options, extras));

    /// <summary>Writes the symbol to a stream in the specified output format.</summary>
    public void Save(Stream stream, OutputFormat format, MatrixOptions? options = null, RenderExtras? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, options, extras));
    }
}
