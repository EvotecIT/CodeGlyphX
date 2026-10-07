using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Aztec code helpers.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// AztecCode.Save("Ticket: CONF-2024", "ticket.png");
/// </code>
/// </example>
public static partial class AztecCode {
    /// <summary>
    /// Encodes a text payload as Aztec.
    /// </summary>
    public static AztecSymbol Encode(string text, AztecEncodeOptions? options = null) {
        return AztecEncoder.EncodeSymbol(text, options);
    }

    /// <summary>
    /// Encodes binary payload as Aztec.
    /// </summary>
    public static AztecSymbol Encode(ReadOnlySpan<byte> data, AztecEncodeOptions? options = null) {
        return AztecEncoder.EncodeSymbol(data.ToArray(), options);
    }

    /// <summary>
    /// Saves Aztec to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves an Aztec binary payload to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return Save((ReadOnlySpan<byte>)data, path, encodeOptions, renderOptions, extras);
    }

    /// <summary>
    /// Saves an Aztec binary payload to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(ReadOnlySpan<byte> data, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(data, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

}
