using System;
using System.IO;
using System.Threading;
using System.Text;
using CodeGlyphX.Pdf417;
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
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Simple PDF417 helpers with fluent and static APIs.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// Pdf417Code.Save("Document ID: 98765", "pdf417.png");
/// </code>
/// </example>
public static partial class Pdf417Code {
    /// <summary>
    /// Starts a fluent PDF417 builder for text payloads.
    /// </summary>
    public static Pdf417Builder Create(string text, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return new Pdf417Builder(text, encodeOptions, renderOptions);
    }

    /// <summary>
    /// Starts a fluent PDF417 builder for byte payloads.
    /// </summary>
    public static Pdf417Builder Create(byte[] data, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        return new Pdf417Builder(data, encodeOptions, renderOptions);
    }

    /// <summary>
    /// Encodes a text payload as PDF417.
    /// </summary>
    public static BitMatrix Encode(string text, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes a Macro PDF417 payload.
    /// </summary>
    public static BitMatrix EncodeMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.EncodeMacro(text, macro, options);
    }

    /// <summary>
    /// Encodes a byte payload as PDF417.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, Pdf417EncodeOptions? options = null) {
        return Pdf417Encoder.EncodeBytes(data, options);
    }
}
